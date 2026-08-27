using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.RateLimiting;
using Polly.Timeout;

namespace SharedLibrary.Resilience;

// Son care policy'si: diger tum policy'ler tukendiginde istegi hataya birakmak yerine
// onceden tanimlanmis bir yanit doner (orn. bos liste, cache'ten veri, varsayilan deger).
public static class FallbackPolicy
{
    public static FallbackStrategyOptions<HttpResponseMessage> CreateHttpFallbackStrategy(
        string clientName,
        ILogger logger,
        Func<HttpResponseMessage> fallbackResponseFactory)
    {
        return new FallbackStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = args => ValueTask.FromResult(ShouldFallback(args.Outcome)),
            FallbackAction = _ => Outcome.FromResultAsValueTask(fallbackResponseFactory()),
            OnFallback = args =>
            {
                logger.LogWarning(args.Outcome.Exception,
                    "{ClientName} call failed. Returning fallback response", clientName);

                // Basarisiz yanit kullanilmayacak, kaynaklari birakalim.
                args.Outcome.Result?.Dispose();

                return ValueTask.CompletedTask;
            }
        };
    }

    private static bool ShouldFallback(Outcome<HttpResponseMessage> outcome)
    {
        // Servise hic ulasilamadi / diger policy'ler istegi reddetti
        if (outcome.Exception is HttpRequestException
            or TimeoutRejectedException // timeout policy
            or BrokenCircuitException // circuit breaker devreyi acti
            or RateLimiterRejectedException) // eszamanlilik limiti doldu
            return true;

        // Servis cevap verdi ama basarisiz (5xx, 404 vb.)
        return outcome.Result is { IsSuccessStatusCode: false };
    }
}
