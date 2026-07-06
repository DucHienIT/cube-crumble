using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeBurst.Systems
{
    /// Squashes the button while pressed. Unscaled time so it works when paused.
    public class UIPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 _baseScale = Vector3.one;
        bool _down;

        void Awake() => _baseScale = transform.localScale;

        public void OnPointerDown(PointerEventData e) => _down = true;
        public void OnPointerUp(PointerEventData e) => _down = false;
        public void OnPointerExit(PointerEventData e) => _down = false;

        void OnDisable()
        {
            _down = false;
            transform.localScale = _baseScale;
        }

        void Update()
        {
            var target = _down ? _baseScale * 0.92f : _baseScale;
            float k = 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, target, k);
        }
    }

    /// Gentle sine bob + sway for decorative UI elements (title tiles, floating cubes).
    /// Hand-rolled instead of DOTween so GameManager's DOTween.KillAll never stops it.
    public class UIMotion : MonoBehaviour
    {
        public float BobAmp = 8f;
        public float BobSpeed = 1.2f;
        public float RotAmp = 3f;
        public float Phase;

        RectTransform _rt;
        Vector2 _basePos;
        float _baseRot;
        bool _ready;

        void Start()
        {
            _rt = (RectTransform)transform;
            _basePos = _rt.anchoredPosition;
            _baseRot = _rt.localEulerAngles.z;
            _ready = true;
        }

        void Update()
        {
            if (!_ready) return;
            float t = Time.unscaledTime * BobSpeed + Phase;
            _rt.anchoredPosition = _basePos + new Vector2(0f, Mathf.Sin(t) * BobAmp);
            _rt.localRotation = Quaternion.Euler(0f, 0f, _baseRot + Mathf.Sin(t * 0.7f) * RotAmp);
        }
    }
}
