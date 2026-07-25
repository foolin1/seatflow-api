using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SeatFlow.Api.Health;

public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType =
            "application/json; charset=utf-8";

        var response =
            new
            {
                status =
                    report.Status.ToString(),

                totalDurationMilliseconds =
                    report.TotalDuration
                        .TotalMilliseconds,

                checks =
                    report.Entries
                        .OrderBy(
                            entry => entry.Key)
                        .ToDictionary(
                            entry => entry.Key,
                            entry =>
                                new
                                {
                                    status =
                                        entry.Value.Status
                                            .ToString(),

                                    description =
                                        entry.Value.Description,

                                    durationMilliseconds =
                                        entry.Value.Duration
                                            .TotalMilliseconds,

                                    error =
                                        entry.Value.Exception?
                                            .Message
                                })
            };

        return context.Response
            .WriteAsJsonAsync(response);
    }
}