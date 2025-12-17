using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Coherent.Web.Portal.Hubs;

[Authorize]
public class CrmChatHub : Hub
{
    public Task JoinThread(string crmThreadId)
    {
        if (string.IsNullOrWhiteSpace(crmThreadId))
            throw new HubException("crmThreadId is required");

        return Groups.AddToGroupAsync(Context.ConnectionId, crmThreadId);
    }

    public Task LeaveThread(string crmThreadId)
    {
        if (string.IsNullOrWhiteSpace(crmThreadId))
            throw new HubException("crmThreadId is required");

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, crmThreadId);
    }
}
