using System;

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
}

