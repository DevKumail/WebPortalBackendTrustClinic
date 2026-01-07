using Microsoft.AspNetCore.SignalR;

namespace Coherent.Web.Portal.Hubs;

/// <summary>
/// SignalR hub for CRM chat. Supports both JWT auth (web clients) and API key auth (mobile backend service).
/// Authentication is handled in Program.cs JWT events.
/// </summary>
public class CrmChatHub : Hub
{
    private readonly ILogger<CrmChatHub> _logger;

    public CrmChatHub(ILogger<CrmChatHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        var isServiceConnection = Context.Items.ContainsKey("IsServiceConnection") && (bool)Context.Items["IsServiceConnection"]!;
        _logger.LogInformation(
            "CrmChatHub: Client connected. ConnectionId={ConnectionId}, IsService={IsService}",
            Context.ConnectionId, isServiceConnection);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "CrmChatHub: Client disconnected. ConnectionId={ConnectionId}, Error={Error}",
            Context.ConnectionId, exception?.Message);
        return base.OnDisconnectedAsync(exception);
    }

    public Task JoinThread(string crmThreadId)
    {
        if (string.IsNullOrWhiteSpace(crmThreadId))
            throw new HubException("crmThreadId is required");

        _logger.LogDebug("Connection {ConnectionId} joining thread {ThreadId}", Context.ConnectionId, crmThreadId);
        return Groups.AddToGroupAsync(Context.ConnectionId, crmThreadId);
    }

    public Task LeaveThread(string crmThreadId)
    {
        if (string.IsNullOrWhiteSpace(crmThreadId))
            throw new HubException("crmThreadId is required");

        _logger.LogDebug("Connection {ConnectionId} leaving thread {ThreadId}", Context.ConnectionId, crmThreadId);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, crmThreadId);
    }
}
