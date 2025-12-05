namespace Coherent.Domain.Entities;

/// <summary>
/// ADHICS Compliance: Logs all third-party system requests
/// </summary>
public class ThirdPartyRequestLog
{
    public Guid Id { get; set; }
    public Guid ThirdPartyClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RequestPayload { get; set; } = string.Empty;
    public string? ResponsePayload { get; set; }
    public int StatusCode { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime RequestTimestamp { get; set; }
    public DateTime? ResponseTimestamp { get; set; }
    public long DurationMs { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string SecurityValidationResult { get; set; } = string.Empty;
}
