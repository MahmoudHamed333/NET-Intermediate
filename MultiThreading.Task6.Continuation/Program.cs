/*
*  Create a Task and attach continuations to it according to the following criteria:
   a.    Continuation task should be executed regardless of the result of the parent task.
   b.    Continuation task should be executed when the parent task finished without success.
   c.    Continuation task should be executed when the parent task would be finished with fail and parent task thread should be reused for continuation
   d.    Continuation task should be executed outside of the thread pool when the parent task would be cancelled
   Demonstrate the work of the each case with console utility.
*/
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiThreading.Task6.Continuation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Create a Task and attach continuations to it according to the following criteria:");
            Console.WriteLine("a.    Continuation task should be executed regardless of the result of the parent task.");
            Console.WriteLine("b.    Continuation task should be executed when the parent task finished without success.");
            Console.WriteLine("c.    Continuation task should be executed when the parent task would be finished with fail and parent task thread should be reused for continuation.");
            Console.WriteLine("d.    Continuation task should be executed outside of the thread pool when the parent task would be cancelled.");
            Console.WriteLine("Demonstrate the work of the each case with console utility.");
            Console.WriteLine();

            await ContinueWhenAll();
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();

            await ContinueWhenFaulted();
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();

            await ContinueWhenFaultedWithSameThread();
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();


            Console.ReadLine();
        }

        static async Task ContinueWhenAll()
        {
            Console.WriteLine("A: Continue regardless of parent task result");
            Console.WriteLine("----------------------------------------------");

            // Test with successful parent task
            Console.WriteLine("\nTest with sucessful parent task");
            var successfulTask = Task.Run(() =>
            {
                Console.WriteLine($"Parent task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1000);
                Console.WriteLine("Parent task completed successfully");
                return 42;
            });

            var continuationTask = successfulTask.ContinueWith(t =>
            {
                Console.WriteLine($"Continuation task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine($"Parent task status: {t.Status}");

                if (t.IsCompletedSuccessfully)
                {
                    Console.WriteLine($"Parent task result: {t.Result}");
                }
            },TaskContinuationOptions.None);
            await continuationTask;

            // Test with failed parent task
            Console.WriteLine("\nTest with failed parent task");
            var failedTask = Task.Run(() =>
            {
                Console.WriteLine($"Parent task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1000);
                Console.WriteLine("Parent task throwing exception");
                throw new InvalidOperationException("Task failed intentionally");
            });

            var continuationAfterFailure = failedTask.ContinueWith(t =>
            {
                Console.WriteLine($"Continuation task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine($"Parent task status: {t.Status}");
                if (t.IsFaulted)
                {
                    Console.WriteLine($"Parent task exception: {t.Exception.InnerException.Message}");
                }
            }, TaskContinuationOptions.None);

            await continuationAfterFailure;
        }

        static async Task ContinueWhenFaulted()
        {
            Console.WriteLine("B: Continue only when parent task fails");
            Console.WriteLine("----------------------------------------------");
            var successTask = Task.Run(() =>
            {
                Console.WriteLine($"Parent task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1000);
                Console.WriteLine("Parent task completed successfully");
                return 42;
            });

            var noRunContinuation = successTask.ContinueWith(t =>
            {
                Console.WriteLine($"Continuation task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine($"Parent task status: {t.Status}");
                Console.WriteLine($"Parent task exception", t.Exception?.InnerException?.Message);
            }, TaskContinuationOptions.OnlyOnFaulted);

            // Test with failed parent task (continuation should run)
            Console.WriteLine("\nTest with failed parent task (continuation should run)");
            
            var failedTask = Task.Run(() =>
            {
                Console.WriteLine($"Parent task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1000);
                Console.WriteLine("Parent task throwing exception");
                throw new InvalidOperationException("Task failed intentionally");
            });

            var runContinuation = failedTask.ContinueWith(t =>
            {
                Console.WriteLine($"Continuation task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine($"Parent task status: {t.Status}");
                Console.WriteLine($"Parent task exception: {t.Exception.InnerException.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);

            await Task.WhenAll(Task.WhenAny(noRunContinuation).ContinueWith(_=> {}), Task.WhenAny(runContinuation));
        }

        static async Task ContinueWhenFaultedWithSameThread()
        {
            Console.WriteLine("C: Continue on same thread when parent task fails");
            Console.WriteLine("----------------------------------------------");
            var failedTask = Task.Run(() =>
            {
                Console.WriteLine($"Parent task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1000);
                Console.WriteLine("Parent task throwing exception");
                throw new InvalidOperationException("Task failed intentionally");
            });
            var continuation = failedTask.ContinueWith(t =>
            {
                Console.WriteLine($"Continuation task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine($"Parent task status: {t.Status}");
                Console.WriteLine($"Parent task exception: {t.Exception.InnerException.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
           
            await Task.WhenAny(continuation);
            Console.WriteLine("\nNote: ExecuteSynchronously attempts to use the same thread when possible");
        }
    }
}
