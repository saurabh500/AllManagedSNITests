using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
namespace AllManagedSNITests
{
    public class TestOrder
    {
        private static ConcurrentQueue<int> Qadd = new ConcurrentQueue<int>();
        private static ConcurrentQueue<int> Qexecute = new ConcurrentQueue<int>();
        private static ConcurrentQueue<int> Qrelease = new ConcurrentQueue<int>();
        // A padding interval to make the output more orderly.
        private static int padding;
        internal class ConcurrentQueueSemaphore
        {
            private readonly SemaphoreSlim _semaphore;
            private readonly ConcurrentQueue<TaskCompletionSource<bool>> _queue =
                new ConcurrentQueue<TaskCompletionSource<bool>>();
            public ConcurrentQueueSemaphore(int initialCount)
            {
                _semaphore = new SemaphoreSlim(initialCount);
            }
            public ConcurrentQueueSemaphore(int initialCount, int maxCount)
            {
                _semaphore = new SemaphoreSlim(initialCount, maxCount);
            }
            public void Wait(int n)
            {
                WaitAsync(n).Wait();
            }
            public Task WaitAsync(int n)
            {
                var tcs = new TaskCompletionSource<bool>();
                _queue.Enqueue(tcs);
                lock (Qadd)
                    Qadd.Enqueue(n);
                Console.WriteLine(n + " is added");
                _semaphore.WaitAsync().ContinueWith(t =>
                {
                    lock (Qexecute)
                        Qexecute.Enqueue(n);
                    Console.WriteLine(n + " is executed");
                    if (_queue.TryDequeue(out TaskCompletionSource<bool> popped))
                        popped.SetResult(true);
                });
                return tcs.Task;
            }
            public int Release(int n)
            {
                Thread.Sleep(500);
                lock (Qrelease)
                    Qrelease.Enqueue(n);
                Console.WriteLine(n + " releases");
                _semaphore.Release();
                return _semaphore.CurrentCount;
            }
            public int CurrentCount
            {
                get
                {
                    return _semaphore.CurrentCount;
                }
            }
        }
        public void Main()
        {
            SemaphoreSlim s = new SemaphoreSlim(1);
            ConcurrentQueueSemaphore semaphore = new ConcurrentQueueSemaphore(1);
            Console.WriteLine("{0} tasks can enter the semaphore.",
                              semaphore.CurrentCount);
            Task[] tasks = new Task[5];
            Console.WriteLine();
            // Create and start five numbered tasks.
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    int semaphoreCount;
                    // Wait for half a second, to allow all the tasks to start and block.
                    Thread.Sleep(500);
                    semaphore.Wait((int)Task.CurrentId);
                    try
                    {
                        Interlocked.Add(ref padding, 100);
                    }
                    finally
                    {
                        semaphoreCount = semaphore.Release((int)Task.CurrentId);
                    }
                });
            }
            // Wait for half a second, to allow all the tasks to start and block.
            Thread.Sleep(500);
            // Main thread waits for the tasks to complete.
            Task.WaitAll(tasks);
            Console.Write("Add:\t\t");
            ShowQ(Qadd);
            Console.Write("Execute:\t");
            ShowQ(Qexecute);
            Console.Write("Release:\t");
            ShowQ(Qrelease);
            Console.WriteLine();
            Console.WriteLine("Main thread exits.");
        }
        private static void ShowQ(ConcurrentQueue<int> q)
        {
            while (q.TryDequeue(out int item))
            {
                Console.Write($"{item}, ");
            }
            Console.WriteLine();
        }
    }
}
