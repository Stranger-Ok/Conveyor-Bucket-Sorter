using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedLib;

namespace ContentGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Generator starts.");
            var totalTimeWatcher = Stopwatch.StartNew();
            MainLoop();
            totalTimeWatcher.Stop();
            Console.Clear();
            Console.WriteLine("Generator ended.");
            Console.WriteLine($"File {Settings.Instance.OutputFilePath} with size {Settings.Instance.MaxFileSizeInMegabytes} created. Total time: {totalTimeWatcher.Elapsed.TotalSeconds} seconds. ");
            Console.WriteLine("Press any key.");
            Console.ReadKey();
        }

        private static void MainLoop()
        {
            using (var generator = new LinesGenerator())
            using (var writer = new LinesWriter(generator.Packages))
            {
                generator.IncreaseSpeed();
                writer.Start();

                DateTime generatorLastSpeedChange = DateTime.MinValue;
                while (writer.FileSizeInMegabytes < Settings.Instance.MaxFileSizeInMegabytes)
                {
                    Console.Clear();

                    Console.WriteLine($"Current writing speed is {writer.CurrentSpeedInPackagesPerSecond} packages\\sec .");
                    Console.WriteLine($"Current generating speed is {generator.CurrentSpeedPackagesPerSecond} packages .");
                    Console.WriteLine($"Total packages in cache is {generator.Packages.Count} .");
                    Console.WriteLine($"Output file size is {writer.FileSizeInMegabytes} mb from {Settings.Instance.MaxFileSizeInMegabytes} mb.");

                    if (writer.CurrentSpeedInPackagesPerSecond == 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    // Increase\Decrease speed is slow operation so wait CooldownTime, after last operation 
                    if ((DateTime.Now - generatorLastSpeedChange).TotalMilliseconds > LinesGenerator.CooldownTimeInMilliseconds)
                    {
                        // if with curent write speed packages ends in less then 2 seconds, then increase speed
                        if ((generator.Packages.Count + generator.CurrentSpeedPackagesPerSecond * 2) / writer.CurrentSpeedInPackagesPerSecond < 2)
                        {
                            generator.IncreaseSpeed();
                            generatorLastSpeedChange = DateTime.Now;
                        }
                        // if with curent write speed packages ends in more then 4 seconds, then generating is too fast, decrease speed
                        else if ((generator.Packages.Count + generator.CurrentSpeedPackagesPerSecond * 4) / writer.CurrentSpeedInPackagesPerSecond >= 4)
                        {
                            generator.DecreaseSpeed();
                            generatorLastSpeedChange = DateTime.Now;
                        }
                        // if with curent write speed packages ends in between 2 and 4 seconds, its Ok.
                    }

                    Thread.Sleep(100);
                }
               
                generator.Stop();
                writer.Stop();
            }
        }

    }
}
