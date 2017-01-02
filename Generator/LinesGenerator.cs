using SharedLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContentGenerator
{
    public class LinesGenerator : IDisposable
    {
        private ConcurrentDictionary<int, long> tasksSpeed = null;
        private Dictionary<Task, CancellationTokenSource> generatingTasks = null;

        public const int CooldownTimeInMilliseconds = 1000;
        public ConcurrentQueue<StringBuilder> Packages { get; set; }
        public long CurrentSpeedPackagesPerSecond { get { return tasksSpeed.Values.Sum(); } }

        public LinesGenerator()
        {
            generatingTasks = new Dictionary<Task, CancellationTokenSource>();
            Packages = new ConcurrentQueue<StringBuilder>();
            tasksSpeed = new ConcurrentDictionary<int, long>();
        }

        public void IncreaseSpeed()
        {
            var cancelationTokenSource = new CancellationTokenSource();
            generatingTasks.Add(Task.Run(() => GeneratePackage(cancelationTokenSource.Token), cancelationTokenSource.Token), cancelationTokenSource);
        }

        public void DecreaseSpeed()
        {
            if (generatingTasks.Count == 0)
                return;
            var task = generatingTasks.Last().Key;
            generatingTasks[task].Cancel();
            int id = task.Id;
            generatingTasks.Remove(task);

            long lastSpeed;
            tasksSpeed.TryRemove(id, out lastSpeed);
            task.WaitWithErrorHandling();
        }

        public void Stop()
        {
            while (generatingTasks.Count > 0)
                DecreaseSpeed();
        }

        private void GeneratePackage(CancellationToken cancellationToken)
        {
            var readWatcher = new Stopwatch();
            tasksSpeed.TryAdd(Task.CurrentId.Value, 0);
            var rnd = new Random();

            while (!cancellationToken.IsCancellationRequested)
            {
                var builder = new StringBuilder();
                
                readWatcher.Restart();
                for (int i = 0; i < Settings.Instance.AveragelinesCountInOneMbOfData; i++)
                    builder.AppendLine(LineStructure.Create(rnd,
                        Settings.Instance.MaxNumberInLine,
                        Settings.Instance.MaxWordsCountInLine,
                        Settings.Instance.WordsList));
                
                Packages.Enqueue(builder);

                readWatcher.Stop();
                tasksSpeed.TryUpdate(Task.CurrentId.Value, (int)(1 / readWatcher.Elapsed.TotalSeconds), 0);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
