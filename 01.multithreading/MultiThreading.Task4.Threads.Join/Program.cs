/*
 * 4.	Write a program which recursively creates 10 threads.
 * Each thread should be with the same body and receive a state with integer number, decrement it,
 * print and pass as a state into the newly created thread.
 * Use Thread class for this task and Join for waiting threads.
 * 
 * Implement all of the following options:
 * - a) Use Thread class for this task and Join for waiting threads.
 * - b) ThreadPool class for this task and Semaphore for waiting threads.
 */

using System;
using System.Threading;

namespace MultiThreading.Task4.Threads.Join
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("4.	Write a program which recursively creates 10 threads.");
            Console.WriteLine("Each thread should be with the same body and receive a state with integer number, decrement it, print and pass as a state into the newly created thread.");
            Console.WriteLine("Implement all of the following options:");
            Console.WriteLine();
            Console.WriteLine("- a) Use Thread class for this task and Join for waiting threads.");
            Console.WriteLine("- b) ThreadPool class for this task and Semaphore for waiting threads.");

            Console.WriteLine();

            Console.WriteLine("Option A: Using Thread class with JOIN");
            CreateThreadRecursively(10);

            Console.WriteLine();

            Console.WriteLine("Option B: Using ThreadPool class with Semaphore");
            CreateThreadPoolRecursively(10);

            Console.ReadLine();
        }

        // Option A: Using Thread class with JOIN
        static void CreateThreadRecursively(int counter)
        {
            if (counter <= 0)
                return;

            Thread thread = new Thread((state) =>
            {
                int count = (int)state;
                count--;
                Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}, Counter: {count}");

                if (count > 0)
                {
                    Thread childThread = new Thread(new ParameterizedThreadStart(ThreadProc));
                    childThread.Start(count);
                    childThread.Join(); // Wait for the child thread to finish
                }
            });

            thread.Start(counter);
            thread.Join(); // Wait for the main thread to finish
        }

        static void ThreadProc(object state)
        {
            int count = (int)state;
            count--;
            Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}, Counter: {count}");
            if (count > 0)
            {
                Thread childThread = new Thread(new ParameterizedThreadStart(ThreadProc));
                childThread.Start(count);
                childThread.Join(); // Wait for the child thread to finish
            }
        }

        // Option B: Using ThreadPool class with Semaphore
        static void CreateThreadPoolRecursively(int counter)
        {
            Semaphore semaphore = new Semaphore(0, 10);

            ThreadPoolThreadProc(counter, semaphore);
        }

        static void ThreadPoolThreadProc(int counter, Semaphore semaphore)
        {
            if (counter <= 0)
            {
                semaphore.Release();
                return;
            }

            ThreadPool.QueueUserWorkItem((state) =>
            {
                int count = counter - 1;
                Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}, Counter: {count}");

                if (count > 0)
                {
                    ThreadPoolThreadProc(count, semaphore);
                }
                else
                {
                    semaphore.Release();
                }
            });
        }
    }
}
