using Coherent.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Coherent.Infrastructure.Services;

public class ChatWebhookBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChatWebhookBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public ChatWebhookBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<ChatWebhookBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat webhook background service loop error");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        }
    }

    private async Task ProcessOnceAsync(CancellationToken stoppingToken)
    {
        var webhookUrl = _configuration["Integrations:MobileBackend:ChatWebhookUrl"];
        var secret = _configuration["Integrations:MobileBackend:ChatWebhookSecret"];

        if (string.IsNullOrWhiteSpace(webhookUrl) || string.IsNullOrWhiteSpace(secret))
            return;

        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IChatWebhookOutboxRepository>();

        var dueItems = await outbox.DequeueDueAsync(10);
        if (dueItems.Count == 0)
            return;

        var client = _httpClientFactory.CreateClient("MobileBackend");

        foreach (var item in dueItems)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                var payloadBytes = Encoding.UTF8.GetBytes(item.PayloadJson);
                var signature = ComputeHmacSha256Hex(payloadBytes, secret);

                using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
                request.Headers.TryAddWithoutValidation("X-Signature", signature);
                request.Content = new StringContent(item.PayloadJson, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request, stoppingToken);
                if (response.IsSuccessStatusCode)
                {
                    await outbox.MarkSucceededAsync(item.Id);
                    continue;
                }

                var error = $"Webhook failed: {(int)response.StatusCode} {response.ReasonPhrase}";
                await ScheduleRetryAsync(outbox, item, error);
            }
            catch (Exception ex)
            {
                await ScheduleRetryAsync(outbox, item, ex.Message);
            }
        }
    }

    private static string ComputeHmacSha256Hex(byte[] body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(body);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static TimeSpan ComputeBackoff(int attemptCount)
    {
        // attemptCount is current attempts already made; next attempt is attemptCount + 1
        var next = attemptCount + 1;
        var seconds = Math.Min(Math.Pow(2, next), 3600); // cap at 1 hour
        return TimeSpan.FromSeconds(seconds);
    }

    private static async Task ScheduleRetryAsync(IChatWebhookOutboxRepository outbox, (Guid Id, string CrmMessageId, string PayloadJson, int AttemptCount) item, string error)
    {
        var nextAttemptCount = item.AttemptCount + 1;
        var delay = ComputeBackoff(item.AttemptCount);
        var nextAttemptAt = DateTime.UtcNow.Add(delay);
        await outbox.MarkFailedAsync(item.Id, error, nextAttemptAt, nextAttemptCount);
    }
}
