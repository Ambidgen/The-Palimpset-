using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// A candle is both the layer's objective and its only shelter.
    ///
    /// Erasures are burned by candlelight, so a living candle carves a safe circle
    /// out of the page. But the flame consumes its own wax: every candle is a
    /// countdown, and a layer threshold demands that a certain number of them are
    /// still alight at once. Protection and progress are therefore the same resource,
    /// which is the central tension of the game.
    /// </summary>
    public class Candle : MonoBehaviour
    {
        public const float MaxFuel = 1f;

        public float Fuel { get; private set; }
        public bool IsOut { get { return Fuel <= 0f; } }
        public float Radius { get { return IsOut ? 0f : 1.65f + Fuel * 2.05f; } }

        Transform _flame;
        Transform _halo;
        Material _haloMaterial;
        Light _light;
        Transform _wax;

        float _burnRate = 0.017f;
        float _hitFlash;
        float _seed;

        public static Candle Create(Transform parent, Vector3 position, float startFuel, float burnRate)
        {
            GameObject root = new GameObject("Candle");
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            Candle candle = root.AddComponent<Candle>();
            candle.Fuel = Mathf.Clamp01(startFuel);
            candle._burnRate = burnRate;
            candle._seed = Random.Range(0f, 100f);
            candle.BuildView();
            candle.Refresh();
            return candle;
        }

        void BuildView()
        {
            // Halo on the floor: the readable boundary of safety.
            _halo = MeshFactory.MeshObject("Halo", MeshFactory.UnitDisc,
                MaterialFactory.DynamicLit(Color.Lerp(Palette.VellumDeep, Palette.EmberDeep, 0.45f)), transform)
                .transform;
            _halo.localPosition = new Vector3(0f, 0.018f, 0f);
            _haloMaterial = _halo.GetComponent<MeshRenderer>().sharedMaterial;

            // Wax: a squat cylinder with a lip.
            _wax = MeshFactory.Primitive(PrimitiveType.Cylinder, "Wax", transform,
                new Vector3(0f, 0.22f, 0f), new Vector3(0.30f, 0.22f, 0.30f),
                MaterialFactory.Lit(Palette.Wax)).transform;

            MeshFactory.Primitive(PrimitiveType.Cylinder, "Wax Lip", transform,
                new Vector3(0f, 0.45f, 0f), new Vector3(0.325f, 0.035f, 0.325f),
                MaterialFactory.Lit(Palette.Alpha(Palette.Wax, 1f)));

            // Wick and flame.
            MeshFactory.Primitive(PrimitiveType.Cylinder, "Wick", transform,
                new Vector3(0f, 0.50f, 0f), new Vector3(0.028f, 0.075f, 0.028f),
                MaterialFactory.Lit(Palette.Ink));

            GameObject flame = MeshFactory.Primitive(PrimitiveType.Sphere, "Flame", transform,
                new Vector3(0f, 0.63f, 0f), new Vector3(0.13f, 0.22f, 0.13f),
                MaterialFactory.Emissive(Palette.Ember, 4.2f));
            _flame = flame.transform;

            GameObject lightObject = new GameObject("Flame Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = Palette.Ember;
            _light.range = 9f;
            _light.intensity = 2.2f;
            _light.shadows = LightShadows.None;
        }

        public void Tick(float dt)
        {
            if (!IsOut)
            {
                Fuel = Mathf.Max(0f, Fuel - _burnRate * dt);
                if (Fuel <= 0f) Snuff();
            }

            _hitFlash = Mathf.MoveTowards(_hitFlash, 0f, dt * 3.2f);
            Refresh();

            if (!IsOut && _flame != null)
            {
                float flicker = 0.86f + Mathf.PerlinNoise(_seed + Time.time * 6.3f, 0.31f) * 0.34f;
                _flame.localScale = new Vector3(0.13f * flicker, 0.22f * (0.85f + flicker * 0.35f), 0.13f * flicker);
            }
        }

        void Refresh()
        {
            bool out_ = IsOut;

            if (_flame != null) _flame.gameObject.SetActive(!out_);
            if (_light != null)
            {
                _light.enabled = !out_;
                if (!out_)
                {
                    float flicker = 0.82f + Mathf.PerlinNoise(_seed + Time.time * 5.1f, 0.77f) * 0.36f;
                    _light.intensity = (1.6f + Fuel * 1.4f) * flicker + _hitFlash * 3.5f;
                    _light.range = 5f + Fuel * 5.5f;
                }
            }

            if (_wax != null)
            {
                float height = Mathf.Lerp(0.06f, 0.22f, Mathf.Clamp01(Fuel * 1.15f));
                _wax.localScale = new Vector3(0.30f, height, 0.30f);
                _wax.localPosition = new Vector3(0f, height, 0f);
            }

            if (_halo != null)
            {
                float radius = Radius;
                _halo.localScale = new Vector3(radius, 1f, radius);
                _halo.gameObject.SetActive(!out_ && radius > 0.01f);

                if (_haloMaterial != null)
                {
                    Color haloColor = Color.Lerp(Palette.EmberDeep, Palette.Ember, Mathf.Clamp01(Fuel));
                    haloColor = Color.Lerp(haloColor, Color.white, _hitFlash * 0.5f);
                    MaterialFactory.ApplyEmission(_haloMaterial, Palette.Alpha(haloColor, 1f), 0.30f + _hitFlash * 0.9f);
                }
            }
        }

        /// <summary>Erasures drink the flame; called each frame they are inside the aura.</summary>
        public void Drain(float amount)
        {
            if (IsOut) return;
            Fuel = Mathf.Max(0f, Fuel - Mathf.Max(0f, amount));
            _hitFlash = Mathf.Min(1f, _hitFlash + amount * 26f);
            if (Fuel <= 0f) Snuff();
        }

        /// <summary>Light from the player's quill; feeds rather than drains.</summary>
        public void Kindle(float amount)
        {
            if (IsOut) return;
            Fuel = Mathf.Min(MaxFuel, Fuel + Mathf.Max(0f, amount));
        }

        /// <summary>
        /// An extinguished candle is not lost: a fresh stroke of ink relights it.
        /// This is what stops a layer from becoming unwinnable once its flames die.
        /// </summary>
        public void Relight(float fuel)
        {
            if (!IsOut) return;
            Fuel = Mathf.Clamp01(Mathf.Max(fuel, 0.18f));
            _hitFlash = 1f;
            Refresh();
            TransientRing.Spawn(transform.parent, transform.position, 0.25f, 1.5f, Palette.Ember, 0.55f, 2.4f);
        }

        void OnDestroy()
        {
            if (_haloMaterial != null) Destroy(_haloMaterial);
        }

        void Snuff()
        {
            Fuel = 0f;
            if (_flame != null) _flame.gameObject.SetActive(false);
            if (_light != null) _light.enabled = false;
            if (_halo != null) _halo.gameObject.SetActive(false);

            TransientRing.Spawn(transform.parent, transform.position, 0.3f, 2.2f, Palette.ErasureGlow, 0.7f, 1.2f);
            SendMessageUpwards("OnCandleSnuffed", this, SendMessageOptions.DontRequireReceiver);
        }
    }
}
