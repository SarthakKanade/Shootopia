using System;
using System.Collections.Generic;

namespace Shootopia
{
    [Serializable]
    public class MapData
    {
        public string SceneName;
        public string DisplayName;
        public string Description;
        public float VoidThreshold; // Y position below which player is considered fallen out of bounds

        public MapData(string sceneName, string displayName, string description, float voidThreshold = -10f)
        {
            SceneName = sceneName;
            DisplayName = displayName;
            Description = description;
            VoidThreshold = voidThreshold;
        }
    }

    public static class MapRegistry
    {
        public const string DEFAULT_MAP = "Map1";

        public static readonly MapData[] AvailableMaps = new MapData[]
        {
            new MapData("Map1", "SECTOR ALPHA", "Floating sci-fi arena with jump pads and lava hazards", 1.0f),
            new MapData("GasStationMap", "GAS STATION", "Cyberpunk gas station surrounded by a containment forcefield", -10.0f)
        };

        private static readonly Dictionary<string, MapData> _mapLookup = new Dictionary<string, MapData>(StringComparer.OrdinalIgnoreCase)
        {
            { "Map1", AvailableMaps[0] },
            { "GasStationMap", AvailableMaps[1] },
            { "GameScene", AvailableMaps[0] },
            { "Shootopia_Sandbox", AvailableMaps[0] }
        };

        public static bool IsGameplayMap(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return _mapLookup.ContainsKey(sceneName);
        }

        public static string GetDisplayName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return "SECTOR ALPHA";
            if (_mapLookup.TryGetValue(sceneName, out var data))
            {
                return data.DisplayName;
            }
            return sceneName.ToUpper();
        }

        public static float GetVoidThreshold(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return -10f;
            if (_mapLookup.TryGetValue(sceneName, out var data))
            {
                return data.VoidThreshold;
            }
            return -10f;
        }

        public static MapData GetMapData(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return AvailableMaps[0];
            if (_mapLookup.TryGetValue(sceneName, out var data))
            {
                return data;
            }
            return new MapData(sceneName, sceneName.ToUpper(), "Custom Map");
        }
    }
}
