using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Utility for calculating and displaying real-time round-trip latency (RTT/Ping).
    /// Safe for Host, Client, and Singleplayer modes across HUD, Pause Menu, and Scoreboard.
    /// </summary>
    public static class NetworkPingUtility
    {
        /// <summary>
        /// Retrieves the current round-trip latency in milliseconds.
        /// </summary>
        /// <param name="clientId">Client ID to query (defaults to 0 / server client ID on client).</param>
        public static int GetCurrentPing(ulong clientId = 0)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                return 0;
            }

            // Host player querying self
            if (NetworkManager.Singleton.IsHost && (clientId == NetworkManager.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId))
            {
                return 5;
            }

            try
            {
                var transport = NetworkManager.Singleton.NetworkConfig?.NetworkTransport;
                if (transport != null)
                {
                    if (NetworkManager.Singleton.IsServer)
                    {
                        if (clientId == NetworkManager.ServerClientId) return 5;
                        ulong rtt = transport.GetCurrentRtt(clientId);
                        return Mathf.Max(5, (int)rtt);
                    }
                    else
                    {
                        // On Client: measure latency to host/server
                        ulong rtt = transport.GetCurrentRtt(NetworkManager.ServerClientId);
                        return Mathf.Max(10, (int)rtt);
                    }
                }
            }
            catch
            {
                // Transport might throw if client not fully connected or during scene transitions
            }

            return 0;
        }

        /// <summary>
        /// Formats ping into a human-readable display string.
        /// </summary>
        public static string FormatPingString(int pingMs, bool isHost = false)
        {
            if (isHost) return "HOST";
            if (pingMs <= 0) return "-- ms";
            return $"{pingMs} ms";
        }

        /// <summary>
        /// Returns high-contrast cybernetic color coding based on latency thresholds:
        /// Neon Cyan/Green (<70ms), Amber Yellow (70-140ms), Bright Red (>140ms).
        /// </summary>
        public static Color GetPingColor(int pingMs)
        {
            if (pingMs <= 0) return new Color(0.7f, 0.75f, 0.85f, 0.9f);
            if (pingMs < 70) return new Color(0.15f, 0.95f, 0.75f, 1f); // Neon green/cyan
            if (pingMs < 140) return new Color(1.0f, 0.82f, 0.2f, 1f); // Neon yellow
            return new Color(1.0f, 0.35f, 0.35f, 1f); // Alert red
        }
    }
}
