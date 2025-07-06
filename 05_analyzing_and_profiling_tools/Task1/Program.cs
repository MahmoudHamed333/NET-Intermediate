using System.Diagnostics;
using System.Security.Cryptography;

public class PasswordHasherAnalysis
{
    // the original method - unchanged for baseline measurement
    //public string GeneratePasswordHashUsingSalt(string passwordText, byte[] salt)
    //{
    //    var iterate = 10000;
    //    var pbkdf2 = new Rfc2898DeriveBytes(passwordText, salt, iterate);
    //    byte[] hash = pbkdf2.GetBytes(20);
    //    byte[] hashBytes = new byte[36];
    //    Array.Copy(salt, 0, hashBytes, 0, 16);
    //    Array.Copy(hash, 0, hashBytes, 16, 20);
    //    var passwordHash = Convert.ToBase64String(hashBytes);
    //    return passwordHash;
    //}

    // The OriginalMethod after optimization
    public string GeneratePasswordHashUsingSalt(string passwordText, byte[] salt)
    {
        const int saltLength = 16;
        const int hashLength = 20;
        const int totalLength = saltLength + hashLength;

        var iterate = 10000;
        using var pbkdf2 = new Rfc2898DeriveBytes(passwordText, salt, iterate);
        Span<byte> hashBytes = stackalloc byte[totalLength];
        salt.CopyTo(hashBytes.Slice(0, saltLength));
        pbkdf2.GetBytes(hashBytes.Slice(saltLength, hashLength).Length);
        var passwordHash = Convert.ToBase64String(hashBytes);
        return passwordHash;
    }

    // Baseline performance measurement
    public void MeasureBaselinePerformance()
    {
        // Test data
        string password = "TestPassword123!";
        byte[] salt = new byte[16];
        new Random().NextBytes(salt);

        // Warm up JIT
        Console.WriteLine("Warming up JIT...");
        for (int i = 0; i < 10; i++)
        {
            GeneratePasswordHashUsingSalt(password, salt);
        }

        // Measure execution time
        Console.WriteLine("\n=== BASELINE PERFORMANCE MEASUREMENT ===");

        var stopwatch = Stopwatch.StartNew();
        string result = "";

        // Single execution measurement
        stopwatch.Restart();
        result = GeneratePasswordHashUsingSalt(password, salt);
        stopwatch.Stop();
        Console.WriteLine($"Single execution: {stopwatch.ElapsedTicks} ticks ({stopwatch.ElapsedMilliseconds} ms)");

        // Multiple executions for average
        int iterations = 1000;
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            result = GeneratePasswordHashUsingSalt(password, salt);
        }
        stopwatch.Stop();

        double avgTicks = (double)stopwatch.ElapsedTicks / iterations;
        double avgMs = (double)stopwatch.ElapsedMilliseconds / iterations;

        Console.WriteLine($"Average over {iterations} iterations:");
        Console.WriteLine($"  - {avgTicks:F2} ticks per call");
        Console.WriteLine($"  - {avgMs:F4} ms per call");
        Console.WriteLine($"  - Total time: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"  - Sample result: {result}");
    }

    // Memory allocation measurement
    public void MeasureMemoryAllocation()
    {
        string password = "TestPassword123!";
        byte[] salt = new byte[16];
        new Random().NextBytes(salt);

        Console.WriteLine("\n=== MEMORY ALLOCATION MEASUREMENT ===");

        // Force garbage collection to get clean baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memoryBefore = GC.GetTotalMemory(false);

        // Execute method multiple times
        int iterations = 100;
        for (int i = 0; i < iterations; i++)
        {
            GeneratePasswordHashUsingSalt(password, salt);
        }

        long memoryAfter = GC.GetTotalMemory(false);
        long memoryAllocated = memoryAfter - memoryBefore;

        Console.WriteLine($"Memory allocated for {iterations} iterations: {memoryAllocated:N0} bytes");
        Console.WriteLine($"Average memory per call: {(double)memoryAllocated / iterations:F2} bytes");

        // Check GC pressure
        Console.WriteLine($"Gen 0 collections: {GC.CollectionCount(0)}");
        Console.WriteLine($"Gen 1 collections: {GC.CollectionCount(1)}");
        Console.WriteLine($"Gen 2 collections: {GC.CollectionCount(2)}");
    }

    // Comprehensive performance profile
    public void RunCompleteBaselineAnalysis()
    {
        Console.WriteLine("PASSWORD HASHING PERFORMANCE BASELINE ANALYSIS");
        Console.WriteLine(new string('=', 50));

        MeasureBaselinePerformance();
        MeasureMemoryAllocation();
    }
}

// Usage example
public class Program
{
    public static void Main()
    {
        var analyzer = new PasswordHasherAnalysis();
        analyzer.RunCompleteBaselineAnalysis();

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}