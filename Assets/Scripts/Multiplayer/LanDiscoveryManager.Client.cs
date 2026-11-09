using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Shootopia
{
    public static partial class LanDiscoveryManager
    {
        public static Task<LanSessionInfo> LocateHostByCodeAsync(string roomCode, float timeoutSeconds = 5.0f)
        {
            return FindRoomByCodeAsync(roomCode, Mathf.RoundToInt(timeoutSeconds * 1000f));
        }

        /// <summary>
        /// Probes local Wi-Fi subnet to find the host machine matching the given room code.
        /// Resolves Host IP and Port in 200-500ms.
        /// </summary>
        public static async Task<LanSessionInfo> FindRoomByCodeAsync(string roomCode, int timeoutMs = 2500)
        {
            string cleanCode = roomCode.Trim().ToUpper();
            if (cleanCode.StartsWith("LAN_")) cleanCode = cleanCode.Substring(4);

            using var cts = new CancellationTokenSource(timeoutMs);
            using var client = new UdpClient();
            client.EnableBroadcast = true;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            try
            {
                client.Client.Bind(new IPEndPoint(IPAddress.Any, 0)); // Any available client port
            }
            catch { }

            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
            string probeMsg = $"{ProbePrefix}|{cleanCode}";
            byte[] probeBytes = Encoding.UTF8.GetBytes(probeMsg);

            // Send initial probe
            try
            {
                await client.SendAsync(probeBytes, probeBytes.Length, broadcastEndpoint);
            }
            catch { }

            var startTime = DateTime.UtcNow;

            while (!cts.Token.IsCancellationRequested && (DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    var receiveTask = client.ReceiveAsync();
                    var timeoutTask = Task.Delay(400, cts.Token);

                    var completed = await Task.WhenAny(receiveTask, timeoutTask);
                    if (completed == receiveTask)
                    {
                        var result = receiveTask.Result;
                        string text = Encoding.UTF8.GetString(result.Buffer);

                        if (text.StartsWith(PacketPrefix))
                        {
                            var info = ParsePacket(text, result.RemoteEndPoint.Address.ToString());
                            if (info != null && info.RoomCode.Equals(cleanCode, StringComparison.OrdinalIgnoreCase))
                            {
                                Debug.Log($"[LanDiscoveryManager] Discovered LAN Room [{cleanCode}] at {info.HostIP}:{info.HostPort}");
                                return info;
                            }
                        }
                    }
                    else
                    {
                        // Repeat probe
                        try
                        {
                            await client.SendAsync(probeBytes, probeBytes.Length, broadcastEndpoint);
                        }
                        catch { }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception) { }
            }

            return null;
        }

        private static LanSessionInfo ParsePacket(string packet, string remoteSenderIp)
        {
            try
            {
                var parts = packet.Split('|');
                if (parts.Length < 9) return null;

                string hostIp = parts[2];
                // If sender reported loopback or 0.0.0.0, use the socket's actual remote sender IP
                if (string.IsNullOrEmpty(hostIp) || hostIp == "127.0.0.1" || hostIp == "0.0.0.0")
                {
                    hostIp = remoteSenderIp;
                }

                return new LanSessionInfo
                {
                    RoomCode = parts[1].Trim().ToUpper(),
                    HostIP = hostIp,
                    HostPort = ushort.TryParse(parts[3], out ushort p) ? p : (ushort)7777,
                    DisplayName = parts[4],
                    MapName = parts[5],
                    GameMode = parts[6],
                    MaxPlayers = int.TryParse(parts[7], out int max) ? max : 8,
                    CurrentPlayers = int.TryParse(parts[8], out int cur) ? cur : 1,
                    LastSeen = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
