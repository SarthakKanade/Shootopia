using UnityEngine;
using UnityEngine.UI;

namespace Shootopia
{
    public class ScreenTintManager : MonoBehaviour
    {
        public static ScreenTintManager Instance;
        
        [SerializeField] private Image _tintImage;
        [SerializeField] private float _fadeSpeed = 5f;
        
        private bool _isActive = false;
        private float _targetAlpha = 0f;
        private float _maxAlpha = 0.12f;

        void Awake()
        {
            Instance = this;
            if (_tintImage != null)
            {
                Color c = _tintImage.color;
                c.a = 0;
                _tintImage.color = c;
            }
        }

        public void SetTintActive(bool active)
        {
            _isActive = active;
            _targetAlpha = active ? _maxAlpha : 0f;
        }

        void Update()
        {
            if (_tintImage == null) return;

            Color c = _tintImage.color;
            c.a = Mathf.MoveTowards(c.a, _targetAlpha, Time.deltaTime * _fadeSpeed);
            _tintImage.color = c;
        }
    }
}
