using ClassImpl;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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

            foreach (var method in impl.Methods.Where(o => (typeof(T).IsInterface || o.IsVirtual) && !o.IsSpecialName && o.IsPublic && typeof(Task).IsAssignableFrom(o.ReturnType)))
            {
                var invokeMethod = ResolveInvokeMethod(method);
                int invokeMethodParamCount = invokeMethod.GetParameters().Length;

                impl.Member<object>(method).Callback(args =>
                {
                    var invokeArgs = new object[invokeMethodParamCount];
                    invokeArgs[0] = connection;
                    invokeArgs[1] = method.Name;

                    for (int i = 0; i < args.Count; i++)
                    {
                        invokeArgs[i + 2] = args.Values.ElementAt(i);
                    }

                    return invokeMethod.Invoke(null, invokeArgs);
                });
            }

            return impl.Finish();
        }

        private static MethodInfo ResolveInvokeMethod(MethodInfo method)
        {
            var genericArgs = method.ReturnType.GetGenericArguments(); // Task<T>
            var returnType = genericArgs.Length == 1 ? genericArgs[0] : null; // T
            
            var invokeMethodArgs = new List<Type>()
            {
                typeof(HubConnection),
                typeof(string)
            };
            invokeMethodArgs.AddRange(Enumerable.Repeat(typeof(object), method.GetParameters().Length));
            invokeMethodArgs.Add(typeof(CancellationToken));

            var invokeMethod = typeof(HubConnectionExtensions).GetMethods().SingleOrDefault(o =>
            {
                bool isValid = o.Name == nameof(HubConnectionExtensions.InvokeAsync)
                            && o.GetParameters().Length == method.GetParameters().Length + 3;

                if (returnType != null)
                    isValid &= o.ContainsGenericParameters;
                else
                    isValid &= !o.ContainsGenericParameters;

                return isValid;
            });

            if (invokeMethod == null)
                throw new MissingMemberException($"Method {method.Name} has too many arguments");

            if (returnType != null)
                invokeMethod = invokeMethod.MakeGenericMethod(returnType);

            return invokeMethod;
        }
    }
}
