using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContentSorter
{
    class Program
    {
        private static TimeSpan decompositionTime;
        private static TimeSpan sortingTime;
        private static TimeSpan compositionTime;
        private static bool isSuccesfull = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Sorter starts.");
            var totalTimeWatcher = Stopwatch.StartNew();
            MainLoop();
            totalTimeWatcher.Stop();
            Console.Clear();
            Console.WriteLine(string.Format("Sorter ended {0}.", isSuccesfull? "successfully": "with errors, some data could be lost"));
            Console.WriteLine($"File {Settings.Instance.OutputFileName} with sorted data created. Total time: {totalTimeWatcher.Elapsed.TotalSeconds} seconds. ");
            Console.WriteLine($"Decomposition time is  {decompositionTime} ");
            Console.WriteLine($"Sorting time is  {sortingTime} ");
            Console.WriteLine($"Composition time is  {compositionTime} ");
            Console.WriteLine("Press any key.");
            Console.ReadKey();
        }

        
        private static void MainLoop()
        {
            var maxThreadsCount = Environment.ProcessorCount * 2;
            var maxPercentageOfUsedMemory = 0.2;
            var maxBucketSize = (int) (maxPercentageOfUsedMemory * (new ComputerInfo().AvailablePhysicalMemory) / (maxThreadsCount));

            using (var bucketSorter = new BucketSorter(maxBucketSize, maxThreadsCount))
            {
                var watcher = Stopwatch.StartNew();
                bucketSorter.RunDecomposition();
                watcher.Stop();
                decompositionTime = watcher.Elapsed;
                watcher.Restart();
                ContentSorter.SortBucketContent(bucketSorter.CompletedBuckets, maxThreadsCount);
                watcher.Stop();
                sortingTime = watcher.Elapsed;
                watcher.Restart();
                bucketSorter.RunComposition();
                watcher.Stop();
                compositionTime = watcher.Elapsed;
                isSuccesfull = bucketSorter.TotalReadedLines == bucketSorter.TotalSortedLines;
            }
        }
    }
}
