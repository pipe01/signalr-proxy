using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRProxy
{
    public interface IProxy
    {
        HubConnection Connection { get; }
    }
}
