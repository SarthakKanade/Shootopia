using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Inspector-editable configuration for custom room creation parameters.
    /// Attached directly to TabContent_Create in the scene so designers and developers
    /// can easily tune room rules, limits, and defaults directly on TabContent_Create without hardcoded values.
    /// </summary>
    public class CustomRoomCreateConfig : MonoBehaviour
    {
        [Header("Default Rules by Mode")]
        [Tooltip("Default match duration in minutes for Free-For-All mode.")]
        public int defaultFfaDuration = 5;

        [Tooltip("Default score/kill limit for Free-For-All mode.")]
        public int defaultFfaScore = 10;

        [Tooltip("Default match duration in minutes for Team Deathmatch mode.")]
        public int defaultTdmDuration = 7;

        [Tooltip("Default score/kill limit for Team Deathmatch mode.")]
        public int defaultTdmScore = 15;

        [Header("Match Duration Constraints")]
        [Tooltip("Minimum match duration allowed in minutes.")]
        public int minDuration = 1;

        [Tooltip("Maximum match duration allowed in minutes.")]
        public int maxDuration = 30;

        [Tooltip("Number of minutes added/subtracted per +/- click.")]
        public int durationStep = 1;

        [Header("Score Limit Constraints")]
        [Tooltip("Minimum score/kill limit allowed.")]
        public int minScore = 5;

        [Tooltip("Maximum score/kill limit allowed.")]
        public int maxScore = 100;

        [Tooltip("Number of points/kills added/subtracted per +/- click.")]
        public int scoreStep = 5;

        [Header("Player Count Constraints")]
        [Tooltip("Default max players in custom room.")]
        public int defaultPlayers = 8;

        [Tooltip("Minimum player limit for custom rooms.")]
        public int minPlayers = 2;

        [Tooltip("Maximum player limit for custom rooms.")]
        public int maxPlayers = 12;

        [Tooltip("Number of players added/subtracted per +/- click.")]
        public int playerStep = 1;

        [Header("Default Mode & Map")]
        [Tooltip("Default selected mode (FFA or TDM).")]
        public string defaultMode = "FFA";

        [Tooltip("Default selected map name (Map1 or GasStationMap).")]
        public string defaultMap = "Map1";
    }
}
