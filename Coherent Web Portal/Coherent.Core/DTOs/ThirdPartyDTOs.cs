namespace Coherent.Core.DTOs;

public class ThirdPartyValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Guid? ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string DataAccessLevel { get; set; } = string.Empty;
    public string? AllowedIPs { get; set; }
}

public class ThirdPartyRequestLogDto
{
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
