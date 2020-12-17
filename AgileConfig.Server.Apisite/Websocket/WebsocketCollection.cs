﻿using Agile.Config.Protocol;
using AgileConfig.Server.IService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgileConfig.Server.Apisite.Websocket
{
    public class WebsocketCollection : IWebsocketCollection
    {
        private WebsocketCollection()
        {
        }

        static WebsocketCollection()
        {
            Instance = new WebsocketCollection();
        }

        private List<WebsocketClient> Clients = new List<WebsocketClient>();
        private object _lockObj = new object();

        public void SendToAll(string message)
        {
            lock (_lockObj)
            {
                if (Clients.Count == 0)
                {
                    return;
                }
                var data = Encoding.UTF8.GetBytes(message);
                foreach (var webSocket in Clients)
                {
                    if (webSocket.Client.State == WebSocketState.Open)
                    {
                        webSocket.Client.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
                     CancellationToken.None);
                    }
                }
            }
        }

        public void SendToAppClients(string appId, string message)
        {
            lock (_lockObj)
            {
                if (Clients.Count == 0)
                {
                    return;
                }
                var appClients = Clients.Where(c => c.AppId == appId);
                if (appClients.Count() == 0)
                {
                    return;
                }
                var data = Encoding.UTF8.GetBytes(message);
                foreach (var webSocket in appClients)
                {
                    if (webSocket.AppId == appId && webSocket.Client.State == WebSocketState.Open)
                    {
                        webSocket.Client.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
                     CancellationToken.None);
                    }
                }
            }
        }

        public void SendActionToAppClients(string appId, WebsocketAction action)
        {
            lock (_lockObj)
            {
                if (Clients.Count == 0)
                {
                    return;
                }
                var appClients = Clients.Where(c => c.AppId == appId);
                if (appClients.Count() == 0)
                {
                    return;
                }
                var json = JsonConvert.SerializeObject(action);
                var data = Encoding.UTF8.GetBytes(json);
                foreach (var webSocket in appClients)
                {
                    if (webSocket.AppId == appId && webSocket.Client.State == WebSocketState.Open)
                    {
                        webSocket.Client.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
                     CancellationToken.None);
                    }
                }
            }
        }


        public async Task SendToOne(WebsocketClient client, string message)
        {
            if (client.Client.State == WebSocketState.Open)
            {
                var data = Encoding.UTF8.GetBytes(message);
                await client.Client.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
               CancellationToken.None);
            }
        }

        public async Task SendActionToOne(WebsocketClient client, WebsocketAction action)
        {
            if (client.Client.State == WebSocketState.Open)
            {
                var json = JsonConvert.SerializeObject(action);
                var data = Encoding.UTF8.GetBytes(json);
                await client.Client.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
               CancellationToken.None);
            }
        }


        public void AddClient(WebsocketClient client)
        {
            lock (_lockObj)
            {
                client.LastHeartbeatTime = DateTime.Now;
                Clients.Add(client);
            }
        }

        public async Task RemoveClient(WebsocketClient client, WebSocketCloseStatus? closeStatus, string closeDesc = null)
        {
            lock (_lockObj)
            {
                Clients.Remove(client);
            }
            if (client.Client.State == WebSocketState.Open)
            {
                await client.Client.CloseAsync(closeStatus.HasValue ? closeStatus.Value : WebSocketCloseStatus.Empty, closeDesc, CancellationToken.None);
                client.Client.Dispose();
            }
        }

        public void RemoveAppClients(string appId, WebSocketCloseStatus? closeStatus, string closeDesc)
        {
            lock (_lockObj)
            {
                var removeClients = Clients.Where(c => c.AppId == appId).ToList();
                if (removeClients.Count == 0)
                {
                    return;
                }
                foreach (var webSocket in removeClients)
                {
                    Clients.Remove(webSocket);
                }
                Task.Run(async () =>
                {
                    foreach (var webSocket in removeClients)
                    {
                        try
                        {
                            if (webSocket.Client.State == WebSocketState.Open)
                            {
                                await webSocket.Client.CloseAsync(closeStatus.HasValue ? closeStatus.Value : WebSocketCloseStatus.Empty, closeDesc, CancellationToken.None);
                                webSocket.Client.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Try to close websocket client {0} err {1}.", webSocket.Id, ex.Message);
                        }
                    }
                });
            }
        }

        public WebsocketClient Get(string clientId)
        {
            lock (_lockObj)
            {
                return Clients.FirstOrDefault(c => c.Id == clientId);
            }
        }

        public ClientInfos Report()
        {
            lock (_lockObj)
            {
                return new ClientInfos
                {
                    ClientCount = Clients.Count,
                    Infos = Clients
                                    .Select(c => new ClientInfo { Id = c.Id, AppId = c.AppId, LastHeartbeatTime = c.LastHeartbeatTime })
                                    .OrderBy(c => c.AppId)
                                    .ThenByDescending(c => c.LastHeartbeatTime)
                                    .ToList()
                };
            }
        }

        public void SendActionToAll(WebsocketAction action)
        {
            lock (_lockObj)
            {
                if (Clients.Count == 0)
                {
                    return;
                }

                var json = JsonConvert.SerializeObject(action);
                var data = Encoding.UTF8.GetBytes(json);
                foreach (var webSocket in Clients)
                {
                    if (webSocket.Client.State == WebSocketState.Open)
                    {
                        webSocket.Client.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
                     CancellationToken.None);
                    }
                }
            }
        }

        public static IWebsocketCollection Instance { get; private set; }
    }

}