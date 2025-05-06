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
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the result type for the main task:");
            Console.WriteLine("1 - Success");
            Console.WriteLine("2 - Failure");
            Console.WriteLine("3 - Cancellation");
            Console.Write("Your choice: ");
            var input = Console.ReadLine();

            var cts = new CancellationTokenSource();
            var mainTask = MainTask(input, cts.Token);

            var continuationA = mainTask.ContinueWith(ContinuationA, TaskContinuationOptions.None);
            var continuationB = mainTask.ContinueWith(ContinuationB, TaskContinuationOptions.OnlyOnFaulted);
            var continuationC = mainTask.ContinueWith(ContinuationC, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            var continuationD = mainTask.ContinueWith(ContinuationD, TaskContinuationOptions.OnlyOnCanceled);

            Task.WhenAny(continuationA, continuationB, continuationC, continuationD);
            Console.ReadKey();
        }

        static Task<int> MainTask(string input, CancellationToken token)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"Main task started on thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1000);

                switch (input)
                {
                    case "1":
                        Console.WriteLine("Main task completed successfully.");
                        return 42;
                    case "2":
                        Console.WriteLine("Main task throwing exception.");
                        throw new InvalidOperationException("Task failed intentionally.");
                    case "3":
                        Console.WriteLine("Main task canceled.");
                        token.ThrowIfCancellationRequested();
                        return 0;
                    default:
                        throw new ArgumentException("Invalid input.");
                }
            }, token);
        }

        static void ContinuationA(Task<int> task)
        {
            Console.WriteLine($"Continuation A started on thread {Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine($"Parent task status: {task.Status}");
            if (task.IsCompletedSuccessfully)
            {
                Console.WriteLine($"Parent task result: {task.Result}");
            }
            else if (task.IsFaulted)
            {
                Console.WriteLine($"Parent task exception: {task.Exception.InnerException.Message}");
            }
        }

        static void ContinuationB(Task<int> task)
        {
            Console.WriteLine($"Continuation B started on thread {Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine($"Parent task status: {task.Status}");
            Console.WriteLine($"Parent task exception: {task.Exception.InnerException.Message}");
        }

        static void ContinuationC(Task<int> task)
        {
            Console.WriteLine($"Continuation C started on thread {Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine($"Parent task status: {task.Status}");
            Console.WriteLine($"Parent task exception: {task.Exception.InnerException.Message}");
        }

        static void ContinuationD(Task<int> task)
        {
            Console.WriteLine($"Continuation D started on thread {Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine($"Parent task status: {task.Status}");
            Console.WriteLine("Parent task was canceled.");
        }
    }
}
