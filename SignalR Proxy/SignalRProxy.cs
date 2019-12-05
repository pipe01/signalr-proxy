using ClassImpl;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRProxy
{
    public static class SignalRProxy
    {
        /// <summary>
        /// Connects to a SignalR hub and, if successful, returns a proxy that wraps some endpoints.
        /// </summary>
        /// <typeparam name="T">The type of the proxy. This type may implement <see cref="IProxy"/></typeparam>
        /// <param name="url">The URL of the SignalR hub</param>
        public static async Task<T> Connect<T>(string url, Action<IHubConnectionBuilder> opts = null)
        {
            var builder = new HubConnectionBuilder().WithUrl(url);
            opts?.Invoke(builder);

            var connection = builder.Build();
            await connection.StartAsync();

            if (connection.State != HubConnectionState.Connected)
                throw new Exception("Couldn't connect to host");

            return connection.CreateProxy<T>();
        }

        public static T CreateProxy<T>(this HubConnection connection)
        {
            var impl = new Implementer<T>();
            bool isIProxy = typeof(IProxy).IsAssignableFrom(typeof(T));

            if (isIProxy)
            {
                impl.Member(o => ((IProxy)o).Connection).Returns(connection);
            }

            foreach (var item in impl.Methods.Where(o => (typeof(T).IsInterface || o.IsVirtual) && !o.IsSpecialName && o.IsPublic && typeof(Task).IsAssignableFrom(o.ReturnType)))
            {
                var returnType = item.ReturnType.GetGenericArguments()[0];

                impl.Member<object>(item).Callback(args
                    => connection.InvokeCoreAsync(item.Name, returnType, args.Values.ToArray()));
            }

            return impl.Finish();
        }
    }
}
