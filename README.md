# signalr-proxy
![Nuget](https://img.shields.io/nuget/v/SignalR.Proxy)

Connect to a SignalR Core hub and call its methods in a typed and direct manner

## Usage

```cs
public interface IMyHub
{
    Task MyMethod(string arg1, int arg2);
}

IMyHub myHub = await SignalRProxy.Connect<IMyHub>("https://example.com/my-hub");
// OR
HubConnection connection = new HubConnectionBuilder().WithUrl("https://example.com/my-hub").Build();
IMyHub myHub = connection.CreateProxy<IMyHub>();

await myHub.MyMethod("hello", 42);
```
