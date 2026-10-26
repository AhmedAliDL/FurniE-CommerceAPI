using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace App.Infrastructure.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
