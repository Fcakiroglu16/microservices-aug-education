using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

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


    public static IHttpClientBuilder AddHedgingPolicy(
        this IHttpClientBuilder builder,
        int maxHedgedAttempts = HedgingPolicy.DefaultMaxHedgedAttempts,
        TimeSpan? delay = null)
    {
        builder.AddResilienceHandler($"{builder.Name}-hedging", (pipeline, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger($"Hedging.{builder.Name}");

            pipeline.AddHedging(HedgingPolicy.CreateHttpHedgingStrategy(
                builder.Name, logger, maxHedgedAttempts, delay));
        });

        return builder;
    }


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


    public static IHttpClientBuilder AddTimeoutPolicy(
        this IHttpClientBuilder builder,
        TimeSpan? timeout = null)
    {
        builder.AddResilienceHandler($"{builder.Name}-timeout", (pipeline, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger($"Timeout.{builder.Name}");

            pipeline.AddTimeout(TimeoutPolicy.CreateHttpTimeoutStrategy(builder.Name, logger, timeout));
        });

        return builder;
    }

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