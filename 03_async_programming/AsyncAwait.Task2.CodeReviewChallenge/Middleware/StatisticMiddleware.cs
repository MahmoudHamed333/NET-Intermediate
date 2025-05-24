using System;
using System.Threading.Tasks;
using AsyncAwait.Task2.CodeReviewChallenge.Headers;
using CloudServices.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AsyncAwait.Task2.CodeReviewChallenge.Middleware;

public class StatisticMiddleware
{
    private readonly RequestDelegate _next;

    private readonly IStatisticService _statisticService;

    public StatisticMiddleware(RequestDelegate next, IStatisticService statisticService)
    {
        _next = next;
        _statisticService = statisticService ?? throw new ArgumentNullException(nameof(statisticService));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path;

        // Register the visit and get the updated count asynchronously
        await _statisticService.RegisterVisitAsync(path);
        var count = await _statisticService.GetVisitsCountAsync(path);

        // Add the header with the updated count
        context.Response.Headers.Add(
            CustomHttpHeaders.TotalPageVisits,
            count.ToString());
        await Task.Delay(3000); 
        await _next(context);
    }
}
