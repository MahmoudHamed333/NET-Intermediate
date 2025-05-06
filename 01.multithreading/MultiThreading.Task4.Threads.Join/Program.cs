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
            Console.WriteLine("4. Write a program which recursively creates 10 threads.");
            Console.WriteLine("Each thread should decrement a state, print it, and pass it to the next thread.");
            Console.WriteLine("Options:");
            Console.WriteLine("- a) Use Thread class with Join.");
            Console.WriteLine("- b) Use ThreadPool class with Semaphore.");
            Console.WriteLine();

            Console.WriteLine("Option A: Using Thread class with Join");
            StartThreadRecursively(10);

            Console.WriteLine();

            Console.WriteLine("Option B: Using ThreadPool class with Semaphore");
            StartThreadPoolRecursively(10);

            Console.ReadLine();
        }

        static void StartThreadRecursively(int counter)
        {
            if (counter <= 0) return;

            Thread thread = new Thread(state =>
            {
                int count = (int)state;
                Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}, Counter: {count}");
                if (count > 1)
                {
                    StartThreadRecursively(count - 1);
                }
            });

            thread.Start(counter);
            thread.Join();
        }

        static void StartThreadPoolRecursively(int counter)
        {
            Semaphore semaphore = new Semaphore(0, 1);

            void ThreadPoolProc(int count)
            {
                if (count <= 0)
                {
                    semaphore.Release();
                    return;
                }

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}, Counter: {count}");
                    ThreadPoolProc(count - 1);
                });
            }

            ThreadPoolProc(counter);
            semaphore.WaitOne(); 
        }
    }
}
