using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using VelocityWars.Core;

namespace VelocityWars
{
    public class TripMineManager : MonoBehaviour
    {
        public static TripMineManager Instance { get; private set; }

        [SerializeField] private GameEvent _onLifeLost;
        private List<TripMineController> _activeMines = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (_onLifeLost != null)
                _onLifeLost.RegisterListener(OnLifeLost);
        }

        private void OnDisable()
        {
            if (_onLifeLost != null)
                _onLifeLost.UnregisterListener(OnLifeLost);
        }

        public void Register(TripMineController m) => _activeMines.Add(m);
        public void Unregister(TripMineController m) => _activeMines.Remove(m);

        private void OnLifeLost()
        {
            // For now, assume player is always ownerID 0 in single-player
            // Destroy all player mines (ownerID >= 0 usually means player)
            DestroyOwnerMines(0); 
        }

        public void DestroyOwnerMines(int ownerID)
        {
            var toKill = _activeMines.Where(m => m.OwnerID == ownerID).ToList();
            foreach (var m in toKill)
            {
                if (m != null) m.ForceExplode();
            }
        }
    }
}
