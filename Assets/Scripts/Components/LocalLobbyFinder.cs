#define DEBUG_LOCAL_LOBBY_FINDER

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utils;

public class LocalLobbyFinder : MonoBehaviour
{
    public LobbyFinderClient Client { get; private set; }
    public LobbyFinderServer Server { get; private set; }

    private const int Port = 9020;
    private const string ServerAvailableMessage = "Come on in, the water's fine!";
    private const string FindServerMessage = "I can has zerver?";

    private void Awake()
    {
        Client = new LobbyFinderClient();
        Server = new LobbyFinderServer();
    }

    public class LobbyFinderClient
    {
        private static readonly TimeSpan ServerTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan BroadcastInterval = TimeSpan.FromSeconds(1);

        public async IAsyncEnumerable<HashSet<IPAddress>> FindServers(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var ipComparer = new IPAddressEqualityComparer();
            var previousServers = new HashSet<IPAddress>(ipComparer);
            var servers = new HashSet<IPAddress>(ipComparer);

            using var client = BuildUdpClient();

#if DEBUG_LOCAL_LOBBY_FINDER
            Debug.Log("Started discovering local servers");
#endif

            StartBroadcastingServerPings(client, cancellationToken);
            StartListeningForServerReplies(client, servers, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(ServerTimeout, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (!servers.SetEquals(previousServers))
                {
#if DEBUG_LOCAL_LOBBY_FINDER
                    Debug.Log($"Updated server list: {string.Join(", ", servers)}");
#endif
                    yield return servers;
                }

                previousServers.Clear();
                previousServers.UnionWith(servers);
                servers.Clear();
            }

#if DEBUG_LOCAL_LOBBY_FINDER
            Debug.Log("Stopped discovering local servers");
#endif
        }

        private static async void StartBroadcastingServerPings(
            UdpClient client,
            CancellationToken cancellationToken = default
        )
        {
#if DEBUG_LOCAL_LOBBY_FINDER
            Debug.Log("Started broadcasting server pings");
#endif
            var requestBytes = Encoding.UTF8.GetBytes(FindServerMessage);
            while (!cancellationToken.IsCancellationRequested)
            {
                await client.SendAsync(requestBytes, requestBytes.Length, new IPEndPoint(IPAddress.Broadcast, Port));

#if DEBUG_LOCAL_LOBBY_FINDER
                Debug.Log("Sent server ping");
#endif
                try
                {
                    await Task.Delay(BroadcastInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

#if DEBUG_LOCAL_LOBBY_FINDER
            Debug.Log("Stopped broadcasting server pings");
#endif
        }

        private static async void StartListeningForServerReplies(
            UdpClient client,
            ISet<IPAddress> servers,
            CancellationToken cancellationToken = default
        )
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var receiveResultOrCanceled = await client
                    .ReceiveAsync()
                    .WithCancellation(cancellationToken);
                if (receiveResultOrCanceled is not Completed<UdpReceiveResult>(var receiveResult)) break;
                var receivedMessage = Encoding.UTF8.GetString(receiveResult.Buffer);

#if DEBUG_LOCAL_LOBBY_FINDER
                    Debug.Log(
                        $"While listening for server replies, pinged by {receiveResult.RemoteEndPoint}: {receivedMessage}"
                    );
#endif

                if (receivedMessage != ServerAvailableMessage) continue;

#if DEBUG_LOCAL_LOBBY_FINDER
                    Debug.Log(
                        $"Received server reply from {receiveResult.RemoteEndPoint}"
                    );
#endif

                servers.Add(receiveResult.RemoteEndPoint.Address);
            }

#if DEBUG_LOCAL_LOBBY_FINDER
            Debug.Log("Stopped listening for server replies");
#endif
        }

        private static UdpClient BuildUdpClient()
        {
            var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.EnableBroadcast = true;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
            return client;
        }
    }

    public class LobbyFinderServer
    {
        public async void StartRespondingToClientPings(CancellationToken cancellationToken = default)
        {
            using var client = BuildUdpClient();

#if DEBUG_LOCAL_LOBBY_FINDER
            Debug.Log("Started responding to client pings");
#endif

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var receiveResultOrCanceled = await client.ReceiveAsync().WithCancellation(cancellationToken);
                    if (receiveResultOrCanceled is not Completed<UdpReceiveResult>(var receiveResult)) break;
                    var receivedMessage = Encoding.UTF8.GetString(receiveResult.Buffer);

#if DEBUG_LOCAL_LOBBY_FINDER
                    Debug.Log(
                        $"While listening for client pings, pinged by {receiveResult.RemoteEndPoint}: {receivedMessage}"
                    );
#endif

                    if (receivedMessage != FindServerMessage) continue;

#if DEBUG_LOCAL_LOBBY_FINDER
                    Debug.Log(
                        $"Responding to client ping by {receiveResult.RemoteEndPoint}"
                    );
#endif

                    var replyBytes = Encoding.UTF8.GetBytes(ServerAvailableMessage);
                    await client.SendAsync(replyBytes, replyBytes.Length, receiveResult.RemoteEndPoint);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

#if DEBUG_LOCAL_LOBBY_FINDER
            Debug.Log("Stopped responding to client pings");
#endif
        }

        private static UdpClient BuildUdpClient()
        {
            var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
            return client;
        }
    }
}
