using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Resilience;

public static class CircuitBreakerPolicy
{
    public const double DefaultFailureRatio = 0.5;
    public const int DefaultMinimumThroughput = 3;

    public static readonly TimeSpan DefaultSamplingDuration = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan DefaultBreakDuration = TimeSpan.FromSeconds(30);

    public static HttpCircuitBreakerStrategyOptions CreateHttpCircuitBreakerStrategy(
        string clientName,
        ILogger logger,
        double failureRatio = DefaultFailureRatio,
        TimeSpan? samplingDuration = null,
        TimeSpan? breakDuration = null,
        int minimumThroughput = DefaultMinimumThroughput)
    {
        return new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = failureRatio,
            SamplingDuration = samplingDuration ?? DefaultSamplingDuration,
            BreakDuration = breakDuration ?? DefaultBreakDuration,
            MinimumThroughput = minimumThroughput,
            // Closed => Open: devre acildi, istekler artik servise gitmiyor (BrokenCircuitException)
            OnOpened = args =>
            {
                logger.LogError(args.Outcome.Exception,
                    "Circuit OPENED for {ClientName}. Requests are blocked for {BreakDuration}",
                    clientName, args.BreakDuration);

                return ValueTask.CompletedTask;
            },
            // Open => HalfOpen: sure doldu, tek bir deneme istegi servise birakiliyor
            OnHalfOpened = _ =>
            {
                logger.LogWarning("Circuit HALF-OPEN for {ClientName}. Trial request will be sent", clientName);

                return ValueTask.CompletedTask;
            },
            // HalfOpen => Closed: deneme istegi basarili, normale donuldu
            OnClosed = _ =>
            {
                logger.LogInformation("Circuit CLOSED for {ClientName}. Back to normal", clientName);

                return ValueTask.CompletedTask;
            }
        };
    }
}