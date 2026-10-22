using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// A short-lived expanding ring of light. Used for every impact, every seal and
    /// every layer transition. Because materials here are opaque, the ring "fades"
    /// by shrinking and drifting back toward the night colour instead of alpha.
    /// </summary>
    public class TransientRing : MonoBehaviour
    {
        float _life;
        float _duration;
        float _fromScale;
        float _toScale;
        Color _color;
        MeshRenderer _renderer;
        Material _material;

        public static TransientRing Spawn(Transform parent, Vector3 position,
            float fromScale, float toScale, Color color, float duration, float intensity = 2.2f)
        {
            GameObject go = MeshFactory.MeshObject("Ink Ring", MeshFactory.UnitRing,
                MaterialFactory.DynamicEmissive(color, intensity), parent);
            go.transform.position = new Vector3(position.x, 0.055f, position.z);
            go.transform.localScale = new Vector3(fromScale, 1f, fromScale);

            TransientRing ring = go.AddComponent<TransientRing>();
            ring._life = 0f;
            ring._duration = Mathf.Max(0.05f, duration);
            ring._fromScale = fromScale;
            ring._toScale = toScale;
            ring._color = color;
            ring._renderer = go.GetComponent<MeshRenderer>();
            ring._material = ring._renderer != null ? ring._renderer.sharedMaterial : null;
            return ring;
        }

        void Update()
        {
            _life += Time.deltaTime;
            float t = Mathf.Clamp01(_life / _duration);

            float scale = Mathf.Lerp(_fromScale, _toScale, Mathf.SmoothStep(0f, 1f, t));
            transform.localScale = new Vector3(scale, 1f, scale);

            if (_material != null)
            {
                Color dimmed = Color.Lerp(_color, Palette.Night, t * 0.85f);
                MaterialFactory.ApplyEmission(_material, dimmed, 2.2f * (1f - t));
            }

            if (t >= 1f) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
