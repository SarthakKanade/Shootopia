using UnityEngine;

namespace VelocityWars.Pickups
{
    public class HealthCrate : MonoBehaviour
    {
        [SerializeField] private float _healAmount = 40f;

        public event System.Action OnPickedUp;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                // Optionally check if already at max health if we wanted to prevent wasting it, 
                // but PRD says "Crate disappears (wasted)" if at 100 HP.
                health.Heal(_healAmount);
                OnPickedUp?.Invoke();
                gameObject.SetActive(false);
            }
        }
    }
}