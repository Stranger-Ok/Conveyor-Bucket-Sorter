using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib
{
    public static class TaskHelper
    {
        public static void WaitAllWithErrorHandling(params Task[] tasks)
        {
            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException e)
            {
                bool importantExceptionOcurs = false;
                foreach (var v in e.InnerExceptions)
                {
                    if (!(v is TaskCanceledException))
                    {
                        Console.WriteLine($"Exception: {v.ToString()}");
                        importantExceptionOcurs = true;
                    }
                }

                if (importantExceptionOcurs)
                    throw;
            }
        }

        public static void WaitWithErrorHandling(this Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException e)
            {
                bool importantExceptionOcurs = false;
                foreach (var v in e.InnerExceptions)
                {
                    if (!(v is TaskCanceledException))
                    {
                        Console.WriteLine($"Exception: {v.ToString()}");
                        importantExceptionOcurs = true;
                    }
                }

                if (importantExceptionOcurs)
                    throw;
            }
        }
    }
}
