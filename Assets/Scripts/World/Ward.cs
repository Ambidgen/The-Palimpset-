using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// A ward is the player's one permanent act. It is a small rotating glyph that
    /// damages any erasure that touches it, and it persists in the layer's tally.
    ///
    /// Wards are deliberately fragile: they are a delaying tactic, not a wall. The
    /// player's real instrument is movement.
    /// </summary>
    public class Ward : MonoBehaviour
    {
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsBroken { get { return Health <= 0f; } }

        Transform _glyph;
        Transform _ring;
        MeshRenderer _ringRenderer;
        Material _ringMaterial;
        float _flash;
        float _seed;
        float _spin;

        public static Ward Create(Transform parent, Vector3 position)
        {
            GameObject root = new GameObject("Ward");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(position.x, 0f, position.z);

            Ward ward = root.AddComponent<Ward>();
            ward.MaxHealth = 60f;
            ward.Health = ward.MaxHealth;
            ward._seed = Random.Range(0f, 64f);
            ward._spin = Random.Range(28f, 46f) * (Random.value > 0.5f ? 1f : -1f);
            ward.BuildView();

            TransientRing.Spawn(parent, root.transform.position, 0.2f, 1.15f, Palette.InkGlow, 0.45f, 2.6f);
            return ward;
        }

        void BuildView()
        {
            _ring = MeshFactory.MeshObject("Ward Ring", MeshFactory.UnitRing,
                MaterialFactory.DynamicEmissive(Palette.InkGlow, 2.4f), transform).transform;
            _ring.localPosition = new Vector3(0f, 0.03f, 0f);
            _ring.localScale = new Vector3(0.62f, 1f, 0.62f);
            _ringRenderer = _ring.GetComponent<MeshRenderer>();
            _ringMaterial = _ringRenderer.sharedMaterial;

            GameObject glyph = MeshFactory.Primitive(PrimitiveType.Cube, "Glyph", transform,
                new Vector3(0f, 0.34f, 0f), new Vector3(0.20f, 0.34f, 0.20f),
                MaterialFactory.Emissive(Palette.InkGlow, 2.8f));
            _glyph = glyph.transform;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _flash = Mathf.MoveTowards(_flash, 0f, dt * 4f);

            if (_glyph != null)
            {
                _glyph.localRotation = Quaternion.Euler(_spin * Time.time * 0.6f, _spin * Time.time, _spin * Time.time * 0.35f);
                float pulse = 0.34f + Mathf.Sin((Time.time + _seed) * 2.4f) * 0.03f;
                _glyph.localScale = new Vector3(0.20f, pulse, 0.20f);
            }

            if (_ring != null)
            {
                _ring.localRotation = Quaternion.Euler(0f, -_spin * Time.time * 0.4f, 0f);
            }

            if (_ringMaterial != null)
            {
                Color c = Color.Lerp(Palette.InkGlow, Palette.Alert, _flash * 0.75f);
                MaterialFactory.ApplyEmission(_ringMaterial, c, 1.4f + _flash * 2.5f);
            }
        }

        /// <summary>Returns true if the ward broke on this hit.</summary>
        public bool TakeDamage(float amount)
        {
            if (IsBroken) return false;

            Health -= Mathf.Max(0f, amount);
            _flash = 1f;

            if (Health <= 0f)
            {
                Health = 0f;
                Break();
                return true;
            }
            return false;
        }

        /// <summary>Ink from the quill repairs a ward, up to its original strength.</summary>
        public void Reinforce(float amount)
        {
            if (IsBroken) return;
            Health = Mathf.Min(MaxHealth, Health + Mathf.Max(0f, amount));
            _flash = Mathf.Max(_flash, 0.25f);
        }

        void Break()
        {
            TransientRing.Spawn(transform.parent, transform.position, 0.35f, 1.5f, Palette.ErasureGlow, 0.5f, 2.0f);
            SendMessageUpwards("OnWardBroken", this, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_ringMaterial != null) Destroy(_ringMaterial);
        }
    }
}
