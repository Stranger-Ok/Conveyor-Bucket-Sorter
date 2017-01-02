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

namespace ContentGenerator
{
    public class LinesWriter : IDisposable
    {
        private ConcurrentQueue<StringBuilder> packagesSource = new ConcurrentQueue<StringBuilder>();
        private Task writeTask = null;
        private CancellationTokenSource writeTaskCancelation = null;
        public int CurrentSpeedInPackagesPerSecond { get; private set; } = 0;
        public long FileSizeInMegabytes { get; private set; } = 0;

        public LinesWriter(ConcurrentQueue<StringBuilder> packagesSource)
        {
            this.packagesSource = packagesSource;
            this.writeTaskCancelation = new CancellationTokenSource();
        }

        public void Start()
        {
            if (writeTask != null)
                throw new NotSupportedException("Writing is already started. You should call Stop() to stop writing and then call Start() again.");

            writeTask = Task.Run(() => WriteLoop(writeTaskCancelation.Token), writeTaskCancelation.Token);
        }

        public void Stop()
        {
            if (writeTask == null)
                throw new NotSupportedException("Writing is not started. You should call Start() to start writing and then call Stop() again.");

            writeTaskCancelation.Cancel();
            writeTask.WaitWithErrorHandling();
            writeTask = null;
        }

       private void WriteLoop(CancellationToken token)
        {
            using (var fileWriter = new StreamWriter(Settings.Instance.OutputFilePath, false))
            {
                var writeWatcher = new Stopwatch();
                while (!token.IsCancellationRequested)
                {
                    // if nothing to write wait some time
                    if (packagesSource.Count == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    writeWatcher.Restart();
                    StringBuilder buffer;

                    // try to write maximum packages in a second, otherwise took all
                    int packCountToWrite = CurrentSpeedInPackagesPerSecond == 0 || packagesSource.Count < CurrentSpeedInPackagesPerSecond ? packagesSource.Count : CurrentSpeedInPackagesPerSecond;
                    for (int i = 0; i < packCountToWrite; i++)
                    {
                        packagesSource.TryDequeue(out buffer);
                        fileWriter.Write(buffer.ToString());
                    }
                    
                    fileWriter.Flush();
                    writeWatcher.Stop();
                    
                    CurrentSpeedInPackagesPerSecond = (int)(packCountToWrite / writeWatcher.Elapsed.TotalSeconds);
                    FileSizeInMegabytes = fileWriter.BaseStream.Length / 1024 / 1024;
                }
            }

        }

        public void Dispose()
        {
            if (writeTask != null)
                Stop();
        }
    }
}
