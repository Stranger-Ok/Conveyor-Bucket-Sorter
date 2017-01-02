using SharedLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContentSorter
{
    public class ContentSorter
    {
        public static void SortBucketContent(IEnumerable<FileBucket> completedBuckets, int degreeOfParallelelizm = 1)
        {
            List<Task> sortTasks = new List<Task>();
            foreach (var bucket in completedBuckets)
            {
                if (sortTasks.Count < degreeOfParallelelizm)
                    sortTasks.Add(Task.Run(() => SortBucket(bucket)));
                else
                {
                    var completedIndex = Task.WaitAny(sortTasks.ToArray());
                    sortTasks.RemoveAt(completedIndex);
                }
            }
            TaskHelper.WaitAllWithErrorHandling(sortTasks.ToArray());
        }

        private static void SortBucket(FileBucket bucket)
        {
            List<LineStructure> lines = new List<LineStructure>();
            var totalTimeWatcher = Stopwatch.StartNew();
            using (StreamReader reader = new StreamReader(bucket.FilePath))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }
            totalTimeWatcher.Stop();
            Console.WriteLine($"Bucket '{bucket.PrefixString}' with lines {lines.Count} readed in: {totalTimeWatcher.Elapsed.TotalSeconds} seconds. ");
            totalTimeWatcher.Restart();
            lines.Sort();
            totalTimeWatcher.Stop();
            Console.WriteLine($"Bucket '{bucket.PrefixString}' with lines {lines.Count} Sorted in: {totalTimeWatcher.Elapsed.TotalSeconds} seconds. ");
            totalTimeWatcher.Restart();
            File.Delete(bucket.FilePath);
            using (StreamWriter writer = new StreamWriter(bucket.FilePath))
            {
                for (int i = 0; i < lines.Count; i++)
                    writer.WriteLine(lines[i]);
                writer.Flush();
            }
            totalTimeWatcher.Stop();
            Console.WriteLine($"Bucket '{bucket.PrefixString}' with lines {lines.Count} writed in: {totalTimeWatcher.Elapsed.TotalSeconds} seconds. ");
        }
    }
}
