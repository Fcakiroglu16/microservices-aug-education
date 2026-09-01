using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace SharedLibrary.Resilience;

// Hedging (paralel deneme) policy'si.
// Retry'dan farki: retry basarisiz olan denemeyi BEKLEYIP tekrar dener, hedging ise
// yavas kalan denemeyi IPTAL ETMEDEN yaninda yeni bir deneme baslatir.
// Ilk basarili donen yanit kullanilir, geri kalan denemeler iptal edilir.
// Yani "kuyruk gecikmesini" (p99 latency) kirpmak icindir; bedeli fazladan istek yuktur.
// Varsayilan: 2 sn icinde cevap gelmezse yeni deneme, orijinal + en fazla 2 paralel deneme.
// DIKKAT: Sadece idempotent istekler (GET, HEAD, PUT, DELETE) icin guvenlidir.
// POST gibi istekler ayni islemi birden fazla kez tetikleyebilir.
public static class HedgingPolicy
{
    // Orijinal istege EK olarak acilacak paralel deneme sayisi (Polly siniri: 1-10).
    public const int DefaultMaxHedgedAttempts = 2;

    public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(2);

    // ResilienceHandler, pipeline'i calistirmadan once o anki HttpRequestMessage'i
    // bu anahtarla resilience context'ine koyar; callback de istegi buradan okur.
    // Ayni HttpRequestMessage ornegi paralel olarak iki kez gonderilemedigi icin
    // her hedge denemesine kendi kopyasini bu anahtarla yaziyoruz.
    private static readonly ResiliencePropertyKey<HttpRequestMessage> RequestMessageKey =
        new("Resilience.Http.RequestMessage");

    public static HttpHedgingStrategyOptions CreateHttpHedgingStrategy(
        string clientName,
        ILogger logger,
        int maxHedgedAttempts = DefaultMaxHedgedAttempts,
        TimeSpan? delay = null)
    {
        return new HttpHedgingStrategyOptions
        {
            MaxHedgedAttempts = maxHedgedAttempts,
            Delay = delay ?? DefaultDelay,
            // Varsayilan ShouldHandle: HttpRequestException, 5xx, 408, 429,
            // BrokenCircuitException ve TimeoutRejectedException.
            // NOT: Hedging sadece hatada degil, Delay suresi dolunca da yeni deneme acar.

            // Varsayilan ActionGenerator yalnizca AddStandardHedgingHandler'in kurdugu
            // "request snapshot" ile calisir. Kendi pipeline'imizda o katman olmadigi icin
            // istegi burada kendimiz kopyaliyoruz.
            ActionGenerator = args =>
            {
                if (!args.PrimaryContext.Properties.TryGetValue(RequestMessageKey, out var originalRequest))
                    // Kopyalanacak istek yok => bu denemeyi acma (null => hedging durur).
                    return null;

                return async () =>
                {
                    args.ActionContext.Properties.Set(RequestMessageKey, await CloneAsync(originalRequest));

                    return await args.Callback(args.ActionContext);
                };
            },
            OnHedging = args =>
            {
                logger.LogWarning(
                    "{ClientName} did not respond in time. Starting hedged attempt {AttemptNumber}/{MaxHedgedAttempts}",
                    clientName, args.AttemptNumber + 1, maxHedgedAttempts);

                return ValueTask.CompletedTask;
            }
        };
    }

    // HttpRequestMessage tek kullanimliktir; her paralel deneme icin yeni bir ornek gerekir.
    private static async ValueTask<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in (IDictionary<string, object?>)request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        if (request.Content is not null)
        {
            // Govde birden fazla kez gonderilecegi icin bellege alinir.
            // (ReadAsByteArrayAsync icerigi buffer'ladigi icin sonraki kopyalar da okuyabilir.)
            var body = await request.Content.ReadAsByteArrayAsync();
            var content = new ByteArrayContent(body);

            foreach (var header in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);

            clone.Content = content;
        }

        return clone;
    }
}
