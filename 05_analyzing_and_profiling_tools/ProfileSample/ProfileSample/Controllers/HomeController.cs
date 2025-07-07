using ProfileSample.DAL;
using ProfileSample.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace ProfileSample.Controllers;

public class HomeController : Controller
{
    // original Index method - unchanged for baseline measurement
    //public ActionResult Index()
    //{
    //    var context = new ProfileSampleEntities();
    //    var sources = context.ImgSources.Take(20).Select(x => x.Id);
    //    var model = new List<ImageModel>();

    //    foreach (var id in sources)
    //    {
    //        var item = context.ImgSources.Find(id);
    //        var obj = new ImageModel()
    //        {
    //            Name = item.Name,
    //            Data = item.Data
    //        };
    //        model.Add(obj);
    //    }

    //    return View(model);
    //}

    // OPTIMIZED VERSION 1: Fix N+1 Query Problem
    public ActionResult Index()
    {
        using (var context = new ProfileSampleEntities())
        {
            // Single query instead of 21 queries
            var model = context.ImgSources
                .Take(20)
                .Select(x => new ImageModel
                {
                    Name = x.Name,
                    Data = x.Data
                })
                .ToList();

            return View("Index", model);
        }
    }

    // Baseline performance measurement method
    public ActionResult IndexWithPerfMeasurement()
    {
        var totalStopwatch = Stopwatch.StartNew();
        var context = new ProfileSampleEntities();

        // Measure the LINQ query execution
        var queryStopwatch = Stopwatch.StartNew();
        var sources = context.ImgSources.Take(20).Select(x => x.Id);
        queryStopwatch.Stop();

        // Measure the enumeration and Find operations
        var enumerationStopwatch = Stopwatch.StartNew();
        var model = new List<ImageModel>();
        int findCallCount = 0;
        var findTotalTime = new Stopwatch();

        foreach (var id in sources)
        {
            findTotalTime.Start();
            var item = context.ImgSources.Find(id);
            findTotalTime.Stop();
            findCallCount++;

            var obj = new ImageModel()
            {
                Name = item.Name,
                Data = item.Data
            };
            model.Add(obj);
        }
        enumerationStopwatch.Stop();
        totalStopwatch.Stop();

        // Log performance metrics
        LogPerformanceMetrics(new PerformanceMetrics
        {
            TotalExecutionTime = totalStopwatch.ElapsedMilliseconds,
            QueryExecutionTime = queryStopwatch.ElapsedMilliseconds,
            EnumerationTime = enumerationStopwatch.ElapsedMilliseconds,
            FindCallCount = findCallCount,
            FindTotalTime = findTotalTime.ElapsedMilliseconds,
            AverageFindTime = findCallCount > 0 ? (double)findTotalTime.ElapsedMilliseconds / findCallCount : 0,
            RecordsProcessed = model.Count,
            TotalDataSize = model.Sum(x => x.Data?.Length ?? 0)
        });

        return View("Index", model);
    }

    // Performance metrics logging
    private void LogPerformanceMetrics(PerformanceMetrics metrics)
    {
        // Log to file, database, or debug output
        var logMessage = $@"
                        === INDEX METHOD PERFORMANCE BASELINE ===
                        Total Execution Time: {metrics.TotalExecutionTime} ms
                        Query Execution Time: {metrics.QueryExecutionTime} ms
                        Enumeration Time: {metrics.EnumerationTime} ms
                        Find Operations: {metrics.FindCallCount} calls
                        Find Total Time: {metrics.FindTotalTime} ms
                        Average Find Time: {metrics.AverageFindTime:F2} ms per call
                        Records Processed: {metrics.RecordsProcessed}
                        Total Data Size: {metrics.TotalDataSize:N0} bytes
                        Memory Usage: {GC.GetTotalMemory(false):N0} bytes
                        ============================================";

        // Debug output (visible in Visual Studio output window)
        System.Diagnostics.Debug.WriteLine(logMessage);

        // Also log to trace for production analysis
        System.Diagnostics.Trace.WriteLine(logMessage);
    }

    // Method to run multiple iterations for statistical analysis
    public ActionResult RunPerformanceBaseline()
    {
        var results = new List<PerformanceMetrics>();
        int iterations = 10;

        // Warm up
        for (int i = 0; i < 3; i++)
        {
            Index();
        }

        // Run multiple iterations
        for (int i = 0; i < iterations; i++)
        {
            var metrics = MeasureSingleIteration();
            results.Add(metrics);
        }

        // Calculate statistics
        var avgTotal = results.Average(x => x.TotalExecutionTime);
        var avgFind = results.Average(x => x.AverageFindTime);
        var minTotal = results.Min(x => x.TotalExecutionTime);
        var maxTotal = results.Max(x => x.TotalExecutionTime);

        var summary = $@"
                            === BASELINE PERFORMANCE SUMMARY ({iterations} iterations) ===
                            Average Total Time: {avgTotal:F2} ms
                            Average Find Time: {avgFind:F2} ms
                            Min Total Time: {minTotal} ms
                            Max Total Time: {maxTotal} ms
                            Standard Deviation: {CalculateStandardDeviation(results.Select(x => x.TotalExecutionTime)):F2} ms
                            ============================================";

        System.Diagnostics.Debug.WriteLine(summary);
        ViewBag.PerformanceResults = summary;

        return View("Index", new List<ImageModel>());
    }

    private PerformanceMetrics MeasureSingleIteration()
    {
        var totalStopwatch = Stopwatch.StartNew();
        var context = new ProfileSampleEntities();

        var queryStopwatch = Stopwatch.StartNew();
        var sources = context.ImgSources.Take(20).Select(x => x.Id);
        queryStopwatch.Stop();

        var enumerationStopwatch = Stopwatch.StartNew();
        var model = new List<ImageModel>();
        int findCallCount = 0;
        var findTotalTime = new Stopwatch();

        foreach (var id in sources)
        {
            findTotalTime.Start();
            var item = context.ImgSources.Find(id);
            findTotalTime.Stop();
            findCallCount++;

            var obj = new ImageModel()
            {
                Name = item.Name,
                Data = item.Data
            };
            model.Add(obj);
        }
        enumerationStopwatch.Stop();
        totalStopwatch.Stop();

        return new PerformanceMetrics
        {
            TotalExecutionTime = totalStopwatch.ElapsedMilliseconds,
            QueryExecutionTime = queryStopwatch.ElapsedMilliseconds,
            EnumerationTime = enumerationStopwatch.ElapsedMilliseconds,
            FindCallCount = findCallCount,
            FindTotalTime = findTotalTime.ElapsedMilliseconds,
            AverageFindTime = findCallCount > 0 ? (double)findTotalTime.ElapsedMilliseconds / findCallCount : 0,
            RecordsProcessed = model.Count,
            TotalDataSize = model.Sum(x => x.Data?.Length ?? 0)
        };
    }

    private double CalculateStandardDeviation(IEnumerable<long> values)
    {
        var avg = values.Average();
        var sumSquares = values.Sum(x => Math.Pow(x - avg, 2));
        return Math.Sqrt(sumSquares / values.Count());
    }

    public ActionResult Convert()
    {
        var files = Directory.GetFiles(Server.MapPath("~/Content/Img"), "*.jpg");
        using (var context = new ProfileSampleEntities())
        {
            foreach (var file in files)
            {
                using (var stream = new FileStream(file, FileMode.Open))
                {
                    byte[] buff = new byte[stream.Length];
                    stream.Read(buff, 0, (int)stream.Length);
                    var entity = new ImgSource()
                    {
                        Name = Path.GetFileName(file),
                        Data = buff,
                    };
                    context.ImgSources.Add(entity);
                    context.SaveChanges();
                }
            }
        }
        return RedirectToAction("Index");
    }

    public ActionResult Contact()
    {
        ViewBag.Message = "Your contact page.";
        return View();
    }
}

public class PerformanceMetrics
{
    public long TotalExecutionTime { get; set; }
    public long QueryExecutionTime { get; set; }
    public long EnumerationTime { get; set; }
    public int FindCallCount { get; set; }
    public long FindTotalTime { get; set; }
    public double AverageFindTime { get; set; }
    public int RecordsProcessed { get; set; }
    public long TotalDataSize { get; set; }
}