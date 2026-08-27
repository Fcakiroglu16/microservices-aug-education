using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace SharedLibrary.Resilience;

// Tum HttpClient'larin ortak kullandigi retry policy.
// Varsayilan: 3 kez retry, bekleme sureleri 2sn - 4sn - 8sn (exponential backoff).
public static class RetryPolicy
{
    public const int DefaultMaxRetryAttempts = 3;

    public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(2);

    public static HttpRetryStrategyOptions CreateHttpRetryStrategy(
        string clientName,
        ILogger logger,
        int maxRetryAttempts = DefaultMaxRetryAttempts,
        TimeSpan? delay = null,
        bool useJitter = false)
    {
        return new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = maxRetryAttempts,
            BackoffType = DelayBackoffType.Exponential,
            Delay = delay ?? DefaultDelay,
            UseJitter = useJitter,
            // Varsayilan ShouldHandle: HttpRequestException, 5xx ve 408.
            // 4xx (400, 404 vb.) tekrar denense de sonuc degismeyecegi icin retry edilmez.
            OnRetry = args =>
            {
                logger.LogWarning(args.Outcome.Exception,
                    "{ClientName} call failed. Retry {AttemptNumber}/{MaxRetryAttempts} in {Delay}",
                    clientName, args.AttemptNumber + 1, maxRetryAttempts, args.RetryDelay);

                return ValueTask.CompletedTask;
            }
        };
    }
}
