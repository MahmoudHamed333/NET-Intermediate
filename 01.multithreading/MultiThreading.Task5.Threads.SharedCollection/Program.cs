/*
 * 5. Write a program which creates two threads and a shared collection:
 * the first one should add 10 elements into the collection and the second should print all elements
 * in the collection after each adding.
 * Use Thread, ThreadPool or Task classes for thread creation and any kind of synchronization constructions.
 */
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MultiThreading.Task5.Threads.SharedCollection
{
    class Program
    {
        private static List<int> sharedCollection = new List<int>();

        private static object _lock = new object();

        private static ManualResetEventSlim itemAddEvent = new ManualResetEventSlim(false);

        static void Main(string[] args)
        {
            Console.WriteLine("5. Write a program which creates two threads and a shared collection:");
            Console.WriteLine("the first one should add 10 elements into the collection and the second should print all elements in the collection after each adding.");
            Console.WriteLine("Use Thread, ThreadPool or Task classes for thread creation and any kind of synchronization constructions.");
            Console.WriteLine();

            Task consumerTask = Task.Run(() => ConsumerThread());
            Task producerTask = Task.Run(() => ProducerThread());

            Task.WaitAll(producerTask);

            Console.WriteLine("Producer task completed.");
            Console.ReadLine();
        }

        static void ProducerThread()
        {
            Console.WriteLine("Producer thread started.");
            for (int i = 0; i <= 10; i++)
            {
                lock (_lock)
                {
                    sharedCollection.Add(i);
                    Console.WriteLine($"Producer added: {i}");
                }

                itemAddEvent.Set(); 
            }

            Console.WriteLine("Producer finished adding items.");
        }

        static void ConsumerThread()
        {
            Console.WriteLine("Consumer thread started.");
            while (true)
            {
                itemAddEvent.Wait();
                itemAddEvent.Reset();

                lock (_lock)
                {
                    Console.WriteLine("\nConsumer: Current collection contents");
                    foreach (var item in sharedCollection)
                    {
                        Console.Write($"{item} ");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
