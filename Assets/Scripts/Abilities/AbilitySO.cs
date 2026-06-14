using UnityEngine;

namespace VelocityWars.Core
{
    public abstract class AbilitySO : ScriptableObject
    {
        public string abilityName;
        public string description;
        public Sprite icon;
        public float duration;

        public abstract void Activate(GameObject player, GameContext context);
        public abstract void Deactivate(GameObject player);
    }

    public enum GameContext { SinglePlayer, Multiplayer }
}