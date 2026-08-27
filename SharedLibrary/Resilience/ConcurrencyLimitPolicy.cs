using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Resilience;

// Client tarafi eszamanlilik limiti (bulkhead).
// Varsayilan: ayni anda en fazla 10 istek; siradaki istekler kuyruga alinmaz, direkt reddedilir.
public static class ConcurrencyLimitPolicy
{
    public const int DefaultPermitLimit = 10;

    // 0 => kuyruk yok, limit dolduysa istek aninda reddedilir (RateLimiterRejectedException).
    public const int DefaultQueueLimit = 0;

    public static HttpRateLimiterStrategyOptions CreateHttpConcurrencyLimiterStrategy(
        string clientName,
        ILogger logger,
        int permitLimit = DefaultPermitLimit,
        int queueLimit = DefaultQueueLimit)
    {
        return new HttpRateLimiterStrategyOptions
        {
            DefaultRateLimiterOptions = new ConcurrencyLimiterOptions
            {
                PermitLimit = permitLimit,
                QueueLimit = queueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            },
            OnRejected = _ =>
            {
                logger.LogWarning(
                    "Concurrency limit reached for {ClientName}. Request rejected (permit limit: {PermitLimit}, queue limit: {QueueLimit})",
                    clientName, permitLimit, queueLimit);

                return ValueTask.CompletedTask;
            }
        };
    }
}
