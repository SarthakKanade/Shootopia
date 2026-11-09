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
    /// Lightweight peer-to-peer Wi-Fi discovery service for Shootopia.
    /// Broadcasts local game rooms only across the local Wi-Fi subnet (never sent to the internet).
    /// Enables joining LAN games strictly by entering the 6-character room code.
    /// </summary>
    public static partial class LanDiscoveryManager
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
    }
}

