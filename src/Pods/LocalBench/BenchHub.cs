using Microsoft.AspNetCore.SignalR;

namespace LocalBench;

public class BenchHub : Hub
{
    public void Echo(long ticks, string payload)
    {
        Clients.Client(Context.ConnectionId).SendAsync("Measure", ticks, payload);
    }
}