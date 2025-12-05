using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Coherent.Infrastructure.Middleware;

/// <summary>
/// Middleware for third-party authentication and request logging (ADHICS compliance)
/// </summary>
public class ThirdPartyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ThirdPartyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        IThirdPartyService thirdPartyService)
    {
        // Only apply to third-party API endpoints
        if (!context.Request.Path.StartsWithSegments("/api/third-party"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var clientId = context.Request.Headers["X-Client-ID"].FirstOrDefault();
        var securityKey = context.Request.Headers["X-Security-Key"].FirstOrDefault();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "";

        // Enable request body reading multiple times
        context.Request.EnableBuffering();
        var requestBody = await ReadRequestBodyAsync(context.Request);

        ThirdPartyValidationResult? validationResult = null;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(securityKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Missing client credentials" });
            
            await LogThirdPartyRequestAsync(
                thirdPartyService,
                Guid.Empty,
                "Unknown",
                context.Request.Path,
                context.Request.Method,
                requestBody,
                null,
                401,
                ipAddress,
                DateTime.UtcNow,
                DateTime.UtcNow,
                stopwatch.ElapsedMilliseconds,
                false,
                "Missing client credentials",
                "FAILED"
            );
            
            return;
        }

        validationResult = await thirdPartyService.ValidateSecurityKeyAsync(clientId, securityKey, ipAddress);

        if (!validationResult.IsValid)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = validationResult.Message });
            
            await LogThirdPartyRequestAsync(
                thirdPartyService,
                Guid.Empty,
                clientId,
                context.Request.Path,
                context.Request.Method,
                requestBody,
                null,
                403,
                ipAddress,
                DateTime.UtcNow,
                DateTime.UtcNow,
                stopwatch.ElapsedMilliseconds,
                false,
                validationResult.Message,
                "FAILED"
            );
            
            return;
        }

        // Check rate limit
        if (!await thirdPartyService.CheckRateLimitAsync(validationResult.ClientId!.Value))
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
            
            await LogThirdPartyRequestAsync(
                thirdPartyService,
                validationResult.ClientId.Value,
                validationResult.ClientName,
                context.Request.Path,
                context.Request.Method,
                requestBody,
                null,
                429,
                ipAddress,
                DateTime.UtcNow,
                DateTime.UtcNow,
                stopwatch.ElapsedMilliseconds,
                false,
                "Rate limit exceeded",
                "RATE_LIMITED"
            );
            
            return;
        }

        // Check endpoint permission
        if (!await thirdPartyService.IsEndpointAllowedAsync(validationResult.ClientId.Value, context.Request.Path))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Endpoint not allowed" });
            
            await LogThirdPartyRequestAsync(
                thirdPartyService,
                validationResult.ClientId.Value,
                validationResult.ClientName,
                context.Request.Path,
                context.Request.Method,
                requestBody,
                null,
                403,
                ipAddress,
                DateTime.UtcNow,
                DateTime.UtcNow,
                stopwatch.ElapsedMilliseconds,
                false,
                "Endpoint not allowed",
                "UNAUTHORIZED_ENDPOINT"
            );
            
            return;
        }

        // Store validation result in HttpContext for controllers to use
        context.Items["ThirdPartyClient"] = validationResult;

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var requestTimestamp = DateTime.UtcNow;
        await _next(context);
        var responseTimestamp = DateTime.UtcNow;
        stopwatch.Stop();

        var responseBodyText = await ReadResponseBodyAsync(responseBody);
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);

        // Log the request
        await LogThirdPartyRequestAsync(
            thirdPartyService,
            validationResult.ClientId.Value,
            validationResult.ClientName,
            context.Request.Path,
            context.Request.Method,
            requestBody,
            responseBodyText,
            context.Response.StatusCode,
            ipAddress,
            requestTimestamp,
            responseTimestamp,
            stopwatch.ElapsedMilliseconds,
            context.Response.StatusCode < 400,
            null,
            "SUCCESS"
        );
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    private async Task<string> ReadResponseBodyAsync(MemoryStream responseBody)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private async Task LogThirdPartyRequestAsync(
        IThirdPartyService thirdPartyService,
        Guid clientId,
        string clientName,
        string endpoint,
        string method,
        string requestPayload,
        string? responsePayload,
        int statusCode,
        string ipAddress,
        DateTime requestTimestamp,
        DateTime responseTimestamp,
        long durationMs,
        bool isSuccess,
        string? errorMessage,
        string validationResult)
    {
        var log = new ThirdPartyRequestLogDto
        {
            ThirdPartyClientId = clientId,
            ClientName = clientName,
            Endpoint = endpoint,
            HttpMethod = method,
            RequestPayload = requestPayload,
            ResponsePayload = responsePayload,
            StatusCode = statusCode,
            IpAddress = ipAddress,
            RequestTimestamp = requestTimestamp,
            ResponseTimestamp = responseTimestamp,
            DurationMs = durationMs,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            SecurityValidationResult = validationResult
        };

        await thirdPartyService.LogRequestAsync(log);
    }
}
