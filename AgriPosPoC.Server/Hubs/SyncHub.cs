// /Hubs/SyncHub.cs

using Microsoft.AspNetCore.SignalR;

namespace AgriPosPoC.Server.Hubs
{
    // This hub is used to notify clients when server data changes
    public class SyncHub : Hub
    {
        public async Task BroadcastMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}