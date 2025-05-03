/*
 * 2.	Write a program, which creates a chain of four Tasks.
 * First Task – creates an array of 10 random integer.
 * Second Task – multiplies this array with another random integer.
 * Third Task – sorts this array by ascending.
 * Fourth Task – calculates the average value. All this tasks should print the values to console.
 */
using System;
using System.Threading.Tasks;

namespace MultiThreading.Task2.Chaining
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(".Net Mentoring Program. MultiThreading V1 ");
            Console.WriteLine("2.	Write a program, which creates a chain of four Tasks.");
            Console.WriteLine("First Task – creates an array of 10 random integer.");
            Console.WriteLine("Second Task – multiplies this array with another random integer.");
            Console.WriteLine("Third Task – sorts this array by ascending.");
            Console.WriteLine("Fourth Task – calculates the average value. All this tasks should print the values to console");
            Console.WriteLine();

            // Task 1 - creates an array of 10 random integer
            var task1 = Task.Run(() =>
            {
                var random = new Random();
                var array = new int[10];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = random.Next(1, 100);
                }

                Console.WriteLine("Generated random Array");
                Console.WriteLine(string.Join(", ", array));

                return array;
            });

            // Task 2 - multiplies this array with another random integer
            var task2 = task1.ContinueWith(previousTask =>
            {
                var array = previousTask.Result;
                var random = new Random();
                var multiplier = random.Next(1, 10);

                Console.WriteLine($"Multiplier array by: {multiplier}");

                for (int i = 0; i < array.Length; i++)
                {
                    array[i] *= multiplier;
                }

                Console.WriteLine($"[{string.Join(", ", array)}]");

                return array;
            });

            // Task 3 - sorts this array by ascending
            var task3 = task2.ContinueWith(previousTask =>
            {
                var array = previousTask.Result;
                Array.Sort(array);
                Console.WriteLine($"Sorted array: [{string.Join(", ", array)}]");
                return array;
            });

            // Task 4 - calculates the average value 
            var task4 = task3.ContinueWith(previousTask =>
            {
                var array = previousTask.Result;
                double average = 0;
                foreach (var number in array)
                {
                    average += number;
                }
                average /= array.Length;
                Console.WriteLine($"Average value: {average}");
            });

            await task4;

            Console.WriteLine("Task chain completed.");
            Console.ReadLine();
        }
    }
}
