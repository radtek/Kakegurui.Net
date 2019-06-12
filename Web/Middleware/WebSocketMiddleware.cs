using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Web.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                string url = context.Request.Path.Value.ToLower();
                if (WebSocketClients.Clients.ContainsKey(url))
                {
                    LogPool.Logger.LogInformation("ws_accept {0}", url);
                    WebSocket client = await context.WebSockets.AcceptWebSocketAsync();
                    WebSocketClients.Clients[url][client] = null;
                    var buffer = new byte[0];
                    try
                    {
                        WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        while (!result.CloseStatus.HasValue)
                        {
                            result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        }
                        LogPool.Logger.LogInformation("ws_close {0}", url);
                        await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    }
                    catch (WebSocketException)
                    {
                        LogPool.Logger.LogInformation("ws_shutdown {0}", url);
                    }
                    WebSocketClients.Clients[url].TryRemove(client, out object obj);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                }
            }
            else
            {
                await _next(context);
            }
     
        }
    }

    public static class WebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseTrafficWebSocket(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketMiddleware>();
        }
    }

    public static class WebSocketClients
    {
        public static ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, object>> Clients { get; } = new ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, object>>();

        public static async void Broadcast(string url, string json)
        {
            if (Clients.ContainsKey(url) && Clients[url].Count > 0)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                foreach (var pair in Clients[url])
                {
                    try
                    {
                        await pair.Key.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (WebSocketException ex)
                    {
                        LogPool.Logger.LogError(ex, "ws_send");
                    }
                    catch (OperationCanceledException)
                    {

                    }
                }
            }
        }

    }
}
