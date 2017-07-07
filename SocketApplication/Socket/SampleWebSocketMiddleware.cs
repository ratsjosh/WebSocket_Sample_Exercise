using Microsoft.AspNetCore.Http;
using SocketApplication.Data;
using SocketApplication.Extensions;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SocketApplication.Utils.Conversions.BusArrival;

namespace SocketApplication.Socket
{
    // Resource: https://www.softfluent.com/blog/dev/2016/12/11/Using-Web-Sockets-with-ASP-NET-Core

    public class SampleWebSocketMiddleware
    {
        // RequestDelegate is used to build the request pipeline. RequestDelegate handles each HTTP request.
        private readonly RequestDelegate _next;

        public SampleWebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invoke method get the socket that is available and use it to send and recieve data.
        /// 
        /// The Invoke method is called by configure(Startup.cs -> configure).
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                // Not a web socket request
                await _next.Invoke(context); // call the next guy in the middleware pipeline. 
                return;
            }

            // A web socket request
            var ct = context.RequestAborted;
            using (var socket = await context.WebSockets.AcceptWebSocketAsync())
            {
                await SendStringAsync(socket, "Welcome to the Socket Application!", ct);

                while (socket.State == WebSocketState.Open)
                {
                    // Socket state is still open
                    string response = await ReceiveStringAsync(socket, ct);
                    if (String.IsNullOrEmpty(response))
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Expected a reply", ct);
                        return;
                    }
                    string[] extracts = response.ExtractFeatures();
                    BusInfo busInfo = new BusInfo();
                    Information bi = busInfo.GetBusRequest(extracts);
                    if (bi == null)
                        await SendStringAsync(socket, $"receive message => \"{response}\"", ct);
                    else
                    {
                        ServiceInformation result = await busInfo.GetBusInformationAsync(bi.StopNumber, bi.BusNumber);
                        Operation op;
                        if (Enum.TryParse(result.Services[0].Status.Replace(" ", String.Empty), out op))
                        {
                            string message = "";
                            switch (op)
                            {
                                case Operation.InOperation:
                                    message = $"Bus {result.Services[0].ServiceNo} Next bus: {result.Services[0].NextBus.EstimatedArrival}    -----    Subsequent bus: {result.Services[0].SubsequentBus.EstimatedArrival}    -----    Thereafter: {result.Services[0].SubsequentBus3.EstimatedArrival}";
                                    break;
                                case Operation.NotInOperation:
                                    message = $"Bus {result.Services[0].ServiceNo} is not operating at {DateTime.Now.ToString("h:mm:ss tt") }";
                                    break;
                            }
                            await SendStringAsync(socket, message, ct);
                        }
                    }
                    //await Task.Delay(1000, ct);
                }
            }

        }

        /// <summary>
        /// Send the message to the client
        /// 
        /// message is sent through text(string) type
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static Task SendStringAsync(WebSocket socket, string data, CancellationToken ct = default(CancellationToken))
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            var segment = new ArraySegment<byte>(buffer);
            return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
        }

        /// <summary>
        /// Receive message from client
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
        {
            // Message can be sent by chunk.
            // We must read all chunks before decoding the content
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    ct.ThrowIfCancellationRequested();

                    // Await for message from client
                    result = await socket.ReceiveAsync(buffer, ct);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);
                if (result.MessageType != WebSocketMessageType.Text)
                    throw new Exception("Unexpected message");

                // Encoding UTF8: https://tools.ietf.org/html/rfc6455#section-5.6
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}
