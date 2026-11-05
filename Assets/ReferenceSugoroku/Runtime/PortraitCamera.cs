using UnityEngine;
namespace ReferenceSugoroku
{
    [RequireComponent(typeof(Camera))]
    public sealed class PortraitCamera : MonoBehaviour
    {
        public float portraitSize = 6.625f;
        public float referenceAspect = 9f / 16f;
        Camera target;
        void OnEnable() { target = GetComponent<Camera>(); Apply(); }
        void LateUpdate() { Apply(); }
        void Apply()
        {
            if (target && target.aspect > 0f)
                target.orthographicSize = portraitSize * Mathf.Max(1f, referenceAspect / target.aspect);
        }
    }
}
