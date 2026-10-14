using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Application.Core.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var started = Stopwatch.GetTimestamp();

        var response = await next();

        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (response is Result { IsFailure: true } failed)
            logger.LogWarning("{Request} failed with {ErrorCode} in {Elapsed:0} ms", name, failed.Error.Code, elapsed);
        else
            logger.LogInformation("{Request} handled in {Elapsed:0} ms", name, elapsed);

        return response;
    }
}
