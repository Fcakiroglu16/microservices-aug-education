using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Resilience;

// Tek bir HTTP denemesi icin timeout policy.
// Varsayilan: 10 saniye. Suresi dolan deneme iptal edilir (TimeoutRejectedException)
// ve retry policy tarafindan yeniden denenir.
public static class TimeoutPolicy
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static HttpTimeoutStrategyOptions CreateHttpTimeoutStrategy(
        string clientName,
        ILogger logger,
        TimeSpan? timeout = null)
    {
        return new HttpTimeoutStrategyOptions
        {
            Timeout = timeout ?? DefaultTimeout,
            OnTimeout = args =>
            {
                logger.LogWarning("{ClientName} attempt timed out after {Timeout}", clientName, args.Timeout);

                return ValueTask.CompletedTask;
            }
        };
    }
}