namespace Shootopia
{
    /// <summary>
    /// Represents the ready state, team assignment, and identifier of a player in a session WaitingRoom.
    /// This state is synchronized via UGS player custom properties.
    /// </summary>
    [System.Serializable]
    public struct PlayerReadyState
    {
        public string playerId;
        public bool isReady;
        public string displayName;
        public int teamIndex; // 0 = Alpha (Sapphire / Blue), 1 = Omega (Ruby / Red)

        public PlayerReadyState(string playerId, bool isReady, string displayName, int teamIndex = 0)
        {
            this.playerId = playerId;
            this.isReady = isReady;
            this.displayName = displayName;
            this.teamIndex = teamIndex;
        }
    }
}
