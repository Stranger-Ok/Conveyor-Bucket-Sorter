using Microsoft.VisualBasic.Devices;
using SharedLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContentSorter
{
    public class BucketSorter : IDisposable
    {
        private long maxSizeOfBucket;
        private int maxThreadsCount;
        private string tempFolderPath = "temp";
        private static long runningThreadsCount = 0;

        private ConcurrentDictionary<MemoryBucket, ConcurrentQueue<string>> memoryBucketsWithStringQueues = new ConcurrentDictionary<MemoryBucket, ConcurrentQueue<string>>();
        private ConcurrentDictionary<FileBucket, bool> fileBucketsWithStates = new ConcurrentDictionary<FileBucket, bool>();
        private ConcurrentDictionary<string, long> processedStringsCountByPrefix = new ConcurrentDictionary<string, long>();

        public long TotalReadedLines { get; private set; }
        public long TotalSortedLines { get; private set; }
        public IEnumerable<FileBucket> CompletedBuckets { get { return fileBucketsWithStates.Where(kv => kv.Value).Select(kv1 => kv1.Key); } }

        public BucketSorter(long maxSizeOfBucket, int maxThreadsCount)
        {
            this.maxThreadsCount = maxThreadsCount;
            this.maxSizeOfBucket = maxSizeOfBucket;
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, tempFolderPath);
            if (!Directory.Exists(tempFolderPath))
                Directory.CreateDirectory(tempFolderPath);
        }

        public void RunDecomposition()
        {
            fileBucketsWithStates.TryAdd(new FileBucket() { FilePath = Settings.Instance.InputFileName, PrefixString = "" }, false);

            Task.Run(() => VisualizeDecompisitionProgress());

            while (fileBucketsWithStates.Any(x => !x.Value))
            {
                // Check completed buckets by file size
                foreach (var bucketPath in fileBucketsWithStates.Where(x => !x.Value))
                {
                    var fileInfo = new FileInfo(bucketPath.Key.FilePath);
                    if (fileInfo.Length < maxSizeOfBucket)
                        fileBucketsWithStates.TryUpdate(bucketPath.Key, true, false);
                }

                var processBuckets = new List<KeyValuePair<FileBucket, bool>>(fileBucketsWithStates.Where(x => !x.Value));
                if (processBuckets.Count == 0)
                    break;
                DecompositeListOfBucketsConcurently(processBuckets);
            }
        }

        public void RunComposition()
        {
            var sortedBuckets = fileBucketsWithStates.Keys.ToList();
            sortedBuckets.Sort();
            Console.Clear();
            using (var writer = new StreamWriter(Settings.Instance.OutputFileName, false))
            {
                foreach (var bucket in sortedBuckets)
                {
                    Console.WriteLine($"Adding bucket with prefix '{bucket.PrefixString}' to output file.");
                    using (var reader = new StreamReader(bucket.FilePath))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            line = line.Insert(line.IndexOf(LineStructure.Seperator, 0, StringComparison.Ordinal) + LineStructure.Seperator.Length, bucket.PrefixString);
                            writer.WriteLine(line);
                            TotalSortedLines++;
                        }

                    }
                    writer.Flush();
                }
            }

            Directory.Delete(tempFolderPath, true);
        }

        private void VisualizeDecompisitionProgress()
        {
            while (fileBucketsWithStates.Any(x => !x.Value))
            {
                var prefixes = memoryBucketsWithStringQueues.Select(kv1 => kv1.Key.PrefixString).Distinct();
                Console.Clear();
                Console.WriteLine($"Max bucket size is {maxSizeOfBucket / 1024 / 1024} mb");
                Console.WriteLine($"Whole nubber of threads is {runningThreadsCount}");
                foreach (var prefix in prefixes)
                    Console.WriteLine($"Sorting prefix is '{prefix}' processed lines count {processedStringsCountByPrefix[prefix]}");
                Thread.Sleep(500);
            }
        }

        private void DecompositeListOfBucketsConcurently(List<KeyValuePair<FileBucket, bool>> processBuckets)
        {
            var tasksDictionaryByWord = new Dictionary<string, Task>();
            foreach (var bucket in processBuckets)
            {
                var taskDecomposite = Task.Run(() => DecompositeBucket(bucket.Key));
                tasksDictionaryByWord.Add(bucket.Key.PrefixString, taskDecomposite);
                // wait until task starts to know actual runningThreadsCount
                while (taskDecomposite.Status != TaskStatus.Running)
                    Thread.Sleep(100);
                if (Interlocked.Read(ref runningThreadsCount) > maxThreadsCount)
                {
                    TaskHelper.WaitAllWithErrorHandling(tasksDictionaryByWord.Values.ToArray());
                    tasksDictionaryByWord.Clear();
                }
            }
            if (tasksDictionaryByWord.Count > 0)
            {
                TaskHelper.WaitAllWithErrorHandling(tasksDictionaryByWord.Values.ToArray());
                tasksDictionaryByWord.Clear();
            }

        }

        private void DecompositeBucket(FileBucket bucket)
        {
            var cancellationSource = new CancellationTokenSource();
            processedStringsCountByPrefix.TryAdd(bucket.PrefixString, 0);

            Task readTask = null;
            Task writeTask = null;
            try
            {
                readTask = Task.Run(() => ReadLineByLine(bucket.FilePath, bucket.PrefixString, string.IsNullOrWhiteSpace(bucket.PrefixString)));
                Interlocked.Increment(ref runningThreadsCount);
                writeTask = Task.Run(() => WriteLinesByPrefix(bucket.PrefixString, cancellationSource.Token), cancellationSource.Token);
                Interlocked.Increment(ref runningThreadsCount);

                while (!readTask.IsCompleted || memoryBucketsWithStringQueues.Any(kv => kv.Key.PrefixString.Equals(bucket.PrefixString) && kv.Value.Count > 0))
                {
                    Thread.Sleep(100);
                }
                cancellationSource.Cancel();
                TaskHelper.WaitAllWithErrorHandling(writeTask, readTask);
                bool value;
                fileBucketsWithStates.TryRemove(bucket, out value);
            }
            finally
            {
                if (!cancellationSource.IsCancellationRequested)
                    cancellationSource.Cancel();
                Interlocked.Decrement(ref runningThreadsCount);
                Interlocked.Decrement(ref runningThreadsCount);
                if (!bucket.FilePath.Equals(Settings.Instance.InputFileName))
                    File.Delete(bucket.FilePath);
            }
        }

        private void ReadLineByLine(string pathToFile, string prefix, bool countTotalLines = false)
        {
            long readedLines = 0;
            using (var reader = new StreamReader(pathToFile))
            {
                var memoryBucketsByWord = new Dictionary<string, MemoryBucket>();
                while (!reader.EndOfStream)
                {
                    // if reader is much faster then writer, wait him
                    if (readedLines % 1000 == 0 && readedLines > processedStringsCountByPrefix[prefix] * 2)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    var line = reader.ReadLine();
                    readedLines++;

                    var word = string.Empty;
                    var seperatorIndex = line.IndexOf(LineStructure.Seperator, 0, StringComparison.Ordinal);
                    if (Settings.Instance.UseRealWordOptimization)
                    {
                        var wordLength = line.IndexOf(' ', seperatorIndex + LineStructure.Seperator.Length) - seperatorIndex - LineStructure.Seperator.Length;

                        if (wordLength > 0) // if space exists take word with space
                            word = line.Substring(seperatorIndex + LineStructure.Seperator.Length, wordLength + 1);
                        else if (line.Length > seperatorIndex + LineStructure.Seperator.Length) //if space not exists take last word
                            word = line.Substring(seperatorIndex + LineStructure.Seperator.Length, line.Length - seperatorIndex - LineStructure.Seperator.Length);
                        else // otherwise end of line
                            word = " ";
                    }
                    else
                        word = line.Length - seperatorIndex - LineStructure.Seperator.Length == 0 ? ' '.ToString() : line[seperatorIndex + LineStructure.Seperator.Length].ToString();

                    if (!memoryBucketsByWord.ContainsKey(word))
                    {
                        var memoBucket = new MemoryBucket(prefix, word);
                        memoryBucketsByWord.Add(word, memoBucket);
                        memoryBucketsWithStringQueues.TryAdd(memoBucket, new ConcurrentQueue<string>(new[] { line }));
                    }
                    else
                        memoryBucketsWithStringQueues[memoryBucketsByWord[word]].Enqueue(line);
                }
            }
            if (countTotalLines)
                TotalReadedLines = readedLines;
        }

        private void WriteLinesByPrefix(string prefix, CancellationToken cancellationToken)
        {
            // wait untill something readed
            while (!memoryBucketsWithStringQueues.Any(kv => kv.Key.PrefixString.Equals(prefix)))
                Thread.Sleep(100);

            var queueAndWriterDictionary = new Dictionary<ConcurrentQueue<string>, StreamWriter>();
            var queueAndWordDictionary = new Dictionary<ConcurrentQueue<string>, string>();

            try
            {
                bool isLastLap = false; // if request cancelation and we have not all queue's writed, run last lap
                while (!cancellationToken.IsCancellationRequested && !isLastLap)
                {
                    isLastLap = cancellationToken.IsCancellationRequested;
                    foreach (var q in memoryBucketsWithStringQueues.Where(kv => kv.Key.PrefixString.Equals(prefix)).Select(kv1 => kv1.Value))
                        if (!queueAndWriterDictionary.ContainsKey(q))
                            queueAndWriterDictionary.Add(q, null);

                    foreach (var queue in queueAndWriterDictionary.Keys.ToArray())
                    {
                        var word = string.Empty;
                        if (queueAndWordDictionary.ContainsKey(queue))
                            word = queueAndWordDictionary[queue];
                        else
                        {
                            word = memoryBucketsWithStringQueues.Where(kv => kv.Value == queue).First().Key.Word;
                            queueAndWordDictionary.Add(queue, word);
                        }

                        if (queueAndWriterDictionary[queue] == null)
                        {
                            string filePath = Path.Combine(tempFolderPath, string.Concat(Guid.NewGuid().ToString(), ".txt"));
                            queueAndWriterDictionary[queue] = new StreamWriter(filePath, false);

                            fileBucketsWithStates.TryAdd(new FileBucket()
                            {
                                FilePath = filePath,
                                PrefixString = string.Concat(prefix, word)
                            }, string.IsNullOrWhiteSpace(word));
                        }

                        var tmpString = string.Empty;
                        var linesCount = 0;
                        while (queue.TryDequeue(out tmpString))
                        {
                            linesCount++;

                            int firstCharIndex = tmpString.IndexOf(LineStructure.Seperator, 0, StringComparison.Ordinal) + LineStructure.Seperator.Length;
                            if (firstCharIndex == tmpString.Length)
                                queueAndWriterDictionary[queue].WriteLine(tmpString);
                            else
                                queueAndWriterDictionary[queue].WriteLine(tmpString.Remove(firstCharIndex, word.Length));
                        }
                        queueAndWriterDictionary[queue].Flush();
                        processedStringsCountByPrefix[prefix] += linesCount;
                    }
                }
            }
            finally
            {
                foreach (var kv in queueAndWriterDictionary)
                    if (kv.Value != null)
                    {
                        kv.Value.Close();
                        kv.Value.Dispose();
                    }
                var memoryBucketsToDelete = new List<MemoryBucket>();
                foreach (var kv in memoryBucketsWithStringQueues.Where(kv1 => kv1.Key.PrefixString.Equals(prefix)))
                    memoryBucketsToDelete.Add(kv.Key);
                ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
                foreach (var bucket in memoryBucketsToDelete)
                    memoryBucketsWithStringQueues.TryRemove(bucket, out queue);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(tempFolderPath))
                Directory.Delete(tempFolderPath, true);
        }
    }
}
