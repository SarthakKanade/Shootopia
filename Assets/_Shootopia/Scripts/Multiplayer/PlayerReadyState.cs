namespace Shootopia
{
    /// <summary>
    /// Represents the ready state and identifier of a player in a sessionWaitingRoom.
    /// This state is synchronized via UGS player custom properties.
    /// </summary>
    [System.Serializable]
    public struct PlayerReadyState
    {
        public string playerId;
        public bool isReady;
        public string displayName;

        public PlayerReadyState(string playerId, bool isReady, string displayName)
        {
            this.playerId = playerId;
            this.isReady = isReady;
            this.displayName = displayName;
        }
    }
}
