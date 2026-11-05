using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Information advertised by a local LAN host over Wi-Fi UDP broadcast.
    /// </summary>
    public class LanSessionInfo
    {
        public string RoomCode { get; set; }
        public string HostIP { get; set; }
        public ushort HostPort { get; set; }
        public string DisplayName { get; set; }
        public string MapName { get; set; }
        public string GameMode { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
        public DateTime LastSeen { get; set; }

        public override string ToString()
        {
            return $"LAN Room [{RoomCode}] at {HostIP}:{HostPort} ('{DisplayName}', {GameMode}, {MapName})";
        }
    }

    /// <summary>
    /// Lightweight peer-to-peer Wi-Fi discovery service for Shootopia.
    /// Broadcasts local game rooms only across the local Wi-Fi subnet (never sent to the internet).
    /// Enables joining LAN games strictly by entering the 6-character room code.
    /// </summary>
    public static class LanDiscoveryManager
    {
        public const int DiscoveryPort = 47777;
        private const string PacketPrefix = "SHOOTOPIA_LAN:1";
        private const string ProbePrefix = "SHOOTOPIA_LAN_PROBE";

        private static CancellationTokenSource _broadcastCts;
        private static LanSessionInfo _currentHostSession;
        private static UdpClient _hostUdpClient;

        public static bool IsBroadcasting => _broadcastCts != null && !_broadcastCts.IsCancellationRequested;
        public static string CurrentRoomCode => _currentHostSession != null ? _currentHostSession.RoomCode : string.Empty;

        /// <summary>
        /// Starts broadcasting a LAN room advertisement over local Wi-Fi.
        /// </summary>
        public static void StartBroadcasting(string roomCode, ushort hostPort, string displayName, string mapName, string gameMode, int maxPlayers, int currentPlayers = 1)
        {
            StopBroadcasting();

            string cleanCode = roomCode.Trim().ToUpper();
            if (cleanCode.StartsWith("LAN_")) cleanCode = cleanCode.Substring(4);

            string localIp = NetworkPingUtility.GetLocalIPAddress();

            _currentHostSession = new LanSessionInfo
            {
                RoomCode = cleanCode,
                HostIP = localIp,
                HostPort = hostPort,
                DisplayName = string.IsNullOrEmpty(displayName) ? $"{cleanCode}'s Arena" : displayName,
                MapName = string.IsNullOrEmpty(mapName) ? "Map1" : mapName,
                GameMode = string.IsNullOrEmpty(gameMode) ? "FFA" : gameMode,
                MaxPlayers = maxPlayers,
                CurrentPlayers = currentPlayers,
                LastSeen = DateTime.UtcNow
            };

            _broadcastCts = new CancellationTokenSource();
            var token = _broadcastCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    _hostUdpClient = new UdpClient();
                    _hostUdpClient.EnableBroadcast = true;
                    _hostUdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _hostUdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

                    Debug.Log($"[LanDiscoveryManager] Started local Wi-Fi broadcast for room [{cleanCode}] on NGO port {hostPort} (Discovery UDP: {DiscoveryPort}).");

                    // Start listening for probes
                    _ = ListenForProbesAsync(_hostUdpClient, token);

                    var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);

                    while (!token.IsCancellationRequested)
                    {
                        if (_currentHostSession != null)
                        {
                            string payload = $"{PacketPrefix}|{_currentHostSession.RoomCode}|{_currentHostSession.HostIP}|{_currentHostSession.HostPort}|{_currentHostSession.DisplayName}|{_currentHostSession.MapName}|{_currentHostSession.GameMode}|{_currentHostSession.MaxPlayers}|{_currentHostSession.CurrentPlayers}";
                            byte[] bytes = Encoding.UTF8.GetBytes(payload);
                            try
                            {
                                await _hostUdpClient.SendAsync(bytes, bytes.Length, broadcastEndpoint);
                            }
                            catch { }
                        }
                        await Task.Delay(1000, token);
                    }
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Debug.LogWarning($"[LanDiscoveryManager] Broadcast task exception: {ex.Message}");
                    }
                }
                finally
                {
                    try { _hostUdpClient?.Close(); } catch { }
                    _hostUdpClient = null;
                }
            }, token);
        }

        private static async Task ListenForProbesAsync(UdpClient listener, CancellationToken token)
        {
            while (!token.IsCancellationRequested && listener != null)
            {
                try
                {
                    var result = await listener.ReceiveAsync();
                    string msg = Encoding.UTF8.GetString(result.Buffer);

                    if (msg.StartsWith(ProbePrefix) && _currentHostSession != null)
                    {
                        var parts = msg.Split('|');
                        string targetCode = parts.Length > 1 ? parts[1].Trim().ToUpper() : "";

                        if (string.IsNullOrEmpty(targetCode) || targetCode == _currentHostSession.RoomCode)
                        {
                            string replyPayload = $"{PacketPrefix}|{_currentHostSession.RoomCode}|{_currentHostSession.HostIP}|{_currentHostSession.HostPort}|{_currentHostSession.DisplayName}|{_currentHostSession.MapName}|{_currentHostSession.GameMode}|{_currentHostSession.MaxPlayers}|{_currentHostSession.CurrentPlayers}";
                            byte[] replyBytes = Encoding.UTF8.GetBytes(replyPayload);
                            await listener.SendAsync(replyBytes, replyBytes.Length, result.RemoteEndPoint);
                        }
                    }
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception) { }
            }
        }

        public static void UpdatePlayerCount(int currentPlayers)
        {
            if (_currentHostSession != null)
            {
                _currentHostSession.CurrentPlayers = currentPlayers;
            }
        }

        public static void StopBroadcasting()
        {
            try
            {
                _broadcastCts?.Cancel();
                _broadcastCts?.Dispose();
                _broadcastCts = null;

                _hostUdpClient?.Close();
                _hostUdpClient = null;
            }
            catch { }
            _currentHostSession = null;
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
