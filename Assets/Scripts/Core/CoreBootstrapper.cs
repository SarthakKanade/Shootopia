using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Guarantees that essential persistent core managers (NetworkManager, SessionManager,
    /// AudioManager, PlayerProfileManager, CursorManager, LoadingScreenManager)
    /// are initialized exactly once across the application lifecycle without scene duplicates.
    /// </summary>
    public static class CoreBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureCoreManagersInitialized()
        {
            if (NetworkManager.Singleton == null)
            {
                var prefab = Resources.Load<GameObject>("CoreManagers");
                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab);
                    instance.name = "CoreManagers";
                    Object.DontDestroyOnLoad(instance);
                }
                else
                {
                    Debug.LogError("[CoreBootstrapper] Resources/CoreManagers prefab not found!");
                }
            }
        }
    }
}
