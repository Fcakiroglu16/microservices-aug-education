using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.RateLimiting;
using Polly.Timeout;
using Polly.Retry;

namespace SharedLibrary.Resilience;

public static class HttpClientBuilderExtensions
{
    // Herhangi bir typed/named HttpClient'a ortak retry policy'sini ekler:
    // services.AddHttpClient<TwoApiClient>(...).AddRetryPolicy();
    public static IHttpClientBuilder AddRetryPolicy(
        this IHttpClientBuilder builder,
        int maxRetryAttempts = RetryPolicy.DefaultMaxRetryAttempts,
        TimeSpan? delay = null,
        bool useJitter = false)
    {
        builder.AddResilienceHandler($"{builder.Name}-retry", (pipeline, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger($"HttpRetry.{builder.Name}");

            pipeline.AddRetry(RetryPolicy.CreateHttpRetryStrategy(
                builder.Name, logger, maxRetryAttempts, delay, useJitter));
        });

        return builder;
    }

    // Herhangi bir typed/named HttpClient'a ortak circuit breaker policy'sini ekler:
    // services.AddHttpClient<TwoApiClient>(...).AddRetryPolicy().AddCircuitBreakerPolicy();
    // NOT: Once eklenen handler disda kalir => retry (dis) -> circuit breaker (ic) -> HTTP istegi.
    public static IHttpClientBuilder AddCircuitBreakerPolicy(
        this IHttpClientBuilder builder,
        double failureRatio = CircuitBreakerPolicy.DefaultFailureRatio,
        TimeSpan? samplingDuration = null,
        TimeSpan? breakDuration = null,
        int minimumThroughput = CircuitBreakerPolicy.DefaultMinimumThroughput)
    {
        builder.AddResilienceHandler($"{builder.Name}-circuit-breaker", (pipeline, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger($"CircuitBreaker.{builder.Name}");

            pipeline.AddCircuitBreaker(CircuitBreakerPolicy.CreateHttpCircuitBreakerStrategy(
                builder.Name, logger, failureRatio, samplingDuration, breakDuration, minimumThroughput));
        });

        return builder;
    }

    // Herhangi bir typed/named HttpClient'a eszamanlilik limiti ekler:
    // services.AddHttpClient<TwoApiClient>(...).AddConcurrencyLimitPolicy();
    // NOT: En disdaki handler olmasi icin diger policy'lerden ONCE eklenmelidir.
    // Bir istek, tum retry denemeleri boyunca tek bir permit tutar.
    public static IHttpClientBuilder AddConcurrencyLimitPolicy(
        this IHttpClientBuilder builder,
        int permitLimit = ConcurrencyLimitPolicy.DefaultPermitLimit,
        int queueLimit = ConcurrencyLimitPolicy.DefaultQueueLimit)
    {
        builder.AddResilienceHandler($"{builder.Name}-concurrency-limit", (pipeline, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger($"ConcurrencyLimit.{builder.Name}");

            pipeline.AddRateLimiter(ConcurrencyLimitPolicy.CreateHttpConcurrencyLimiterStrategy(
                builder.Name, logger, permitLimit, queueLimit));
        });

        return builder;
    }

    // Herhangi bir typed/named HttpClient'a timeout policy'sini ekler:
    // services.AddHttpClient<TwoApiClient>(...).AddRetryPolicy().AddTimeoutPolicy();
    // NOT: En ICTEKI handler olmasi icin diger policy'lerden SONRA eklenmelidir.
    // Boylece timeout her deneme icin ayri ayri isler ve dolan sure retry'i tetikler.
    public static IHttpClientBuilder AddTimeoutPolicy(
        this IHttpClientBuilder builder,
        TimeSpan? timeout = null)
    {
        // HttpClient'in kendi timeout'u (varsayilan 100 sn) devre disi birakilir.
        // Aksi halde iki ayri timeout katmani olur ve pipeline bitmeden HttpClient
        // TaskCanceledException firlatarak retry'lari yarida kesebilir.
        // Toplam sure zaten sinirlidir: (retry sayisi + 1) x timeout + retry beklemeleri.
        builder.ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);

        builder.AddResilienceHandler($"{builder.Name}-timeout", (pipeline, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger($"Timeout.{builder.Name}");

            pipeline.AddTimeout(TimeoutPolicy.CreateHttpTimeoutStrategy(builder.Name, logger, timeout));
        });

        return builder;
    }

    // Herhangi bir typed/named HttpClient'a fallback policy'sini ekler:
    // services.AddHttpClient<TwoApiClient>(...).AddFallbackPolicy(() => new HttpResponseMessage(HttpStatusCode.OK) { ... });
    // NOT: En DISTAKI handler olmasi icin diger policy'lerden ONCE eklenmelidir.
    // Boylece retry tukenmesi, acik devre, timeout ve limit reddi dahil her hata fallback'e duser.
    public static IHttpClientBuilder AddFallbackPolicy(
        this IHttpClientBuilder builder,
        Func<HttpResponseMessage> fallbackResponseFactory)
    {
        builder.AddResilienceHandler($"{builder.Name}-fallback", (pipeline, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger($"Fallback.{builder.Name}");

            pipeline.AddFallback(FallbackPolicy.CreateHttpFallbackStrategy(
                builder.Name, logger, fallbackResponseFactory));
        });

        return builder;
    }
}
