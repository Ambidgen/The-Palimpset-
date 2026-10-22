using UnityEngine;

namespace Palimpsest
{
    /// <summary>What kind of mark an earlier hand left under the current page.</summary>
    public enum StratumKind
    {
        /// <summary>The ring a seal leaves behind when the page turns above it.</summary>
        Ghost = 0,

        /// <summary>The blot left where an erasure was un-written.</summary>
        Stain = 1
    }

    /// <summary>
    /// One mark in the sediment beneath the live page.
    ///
    /// A stratum is not decoration: it is the page's memory made mechanical. Two
    /// kinds exist, and they pull in opposite directions.
    ///
    /// <list type="bullet">
    /// <item><b>Ghost</b> - the ring of a ward that survived a completed layer. It
    /// is inert and safe, and it is the best place on the page to inscribe: a new
    /// ward written over an old one absorbs its strength.</item>
    /// <item><b>Stain</b> - the blot left where an erasure died. It ferments. Once
    /// its charge fills it becomes a <i>bloom</i> and starts disgorging fresh
    /// erasures from that exact spot, so your own kill count becomes the map of
    /// future danger. Ink cauterises a stain into a ghost; light scours it.</item>
    /// </list>
    ///
    /// Every stratum therefore records provenance: how old it is, how strong it
    /// still is, and how many times it has erupted.
    /// </summary>
    public class Stratum : MonoBehaviour
    {
        /// <summary>Strength a stain needs before it is ever allowed to bloom.</summary>
        public const float BloomStrength = 0.52f;

        /// <summary>A stain that has erupted this many times is spent and goes quiet.</summary>
        public const int MaxGushes = 3;

        /// <summary>Below this strength a stain is only a smear, never a hazard.</summary>
        const float FaintStrength = 0.10f;

        public StratumKind Kind { get; private set; }
        public float Strength { get; private set; }
        public float Charge { get; private set; }
        public int Age { get; private set; }
        public int Gushes { get; private set; }

        /// <summary>True once a stain has erupted all it can and has gone inert.</summary>
        public bool IsSpent
        {
            get { return Kind == StratumKind.Stain && Gushes >= MaxGushes; }
        }

        /// <summary>
        /// A bloom is a stain that is strong enough, fully charged and unspent. It
        /// is a visible promise that something will come out of that spot.
        /// </summary>
        public bool IsBloom
        {
            get
            {
                return Kind == StratumKind.Stain
                    && !IsSpent
                    && Strength >= BloomStrength
                    && Charge >= 1f;
            }
        }

        public bool CanErupt { get { return IsBloom && _eruptionCooldown <= 0f; } }

        /// <summary>
        /// The health bonus a ward inherits by being written over this mark. Older
        /// and stronger marks pass on more, which makes continuity pay.
        /// </summary>
        public float ResilienceBonus
        {
            get
            {
                if (Kind == StratumKind.Stain) return 20f + Strength * 24f;
                return 28f + Mathf.Min(Age, 4) * 7f;
            }
        }

        GameObject _ghostRoot;
        GameObject _stainRoot;
        Transform _ghostRing;
        Transform _ghostMark;
        Transform _stainBlot;
        Transform _stainRing;
        Material _material;
        float _seed;
        float _flash;
        float _eruptionCooldown;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public static Stratum Create(Transform parent, StratumKind kind, Vector3 position,
            float strength, int age, int gushes, float charge)
        {
            GameObject root = new GameObject(kind == StratumKind.Stain ? "Stain" : "Ghost Seal");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(position.x, 0f, position.z);

            Stratum stratum = root.AddComponent<Stratum>();
            stratum.Kind = kind;
            stratum.Strength = Mathf.Clamp01(strength);
            stratum.Age = Mathf.Max(0, age);
            stratum.Gushes = Mathf.Clamp(gushes, 0, MaxGushes);
            stratum.Charge = Mathf.Clamp01(charge);
            stratum._seed = Random.Range(0f, 200f);
            stratum.BuildView();
            return stratum;
        }

        void BuildView()
        {
            // One dynamic material per mark: every stratum animates its own colour and
            // emission as it ferments, so the material cannot be shared.
            _material = MaterialFactory.DynamicEmissive(Palette.InkGlow, 0.6f);

            // ---- ghost: deliberately the same ring the living ward uses ----
            _ghostRoot = new GameObject("Ghost");
            _ghostRoot.transform.SetParent(transform, false);

            _ghostRing = MeshFactory.MeshObject("Ghost Ring", MeshFactory.UnitRing, _material,
                _ghostRoot.transform).transform;
            _ghostRing.localPosition = new Vector3(0f, 0.020f, 0f);

            _ghostMark = MeshFactory.Primitive(PrimitiveType.Cube, "Ghost Mark", _ghostRoot.transform,
                new Vector3(0f, 0.21f, 0f), new Vector3(0.05f, 0.42f, 0.05f), _material).transform;
            _ghostMark.localRotation = Quaternion.Euler(0f, _seed * 37f, 12f);

            // ---- stain: a spill with a hot rim ----
            _stainRoot = new GameObject("Stain");
            _stainRoot.transform.SetParent(transform, false);

            _stainBlot = MeshFactory.MeshObject("Blot", MeshFactory.UnitBlot, _material,
                _stainRoot.transform).transform;
            _stainBlot.localPosition = new Vector3(0f, 0.014f, 0f);
            _stainBlot.localRotation = Quaternion.Euler(0f, _seed * 53f, 0f);

            _stainRing = MeshFactory.MeshObject("Stain Rim", MeshFactory.UnitRing, _material,
                _stainRoot.transform).transform;
            _stainRing.localPosition = new Vector3(0f, 0.024f, 0f);

            ApplyKind();
            RefreshScale();
        }

        void ApplyKind()
        {
            bool stain = Kind == StratumKind.Stain;
            if (_ghostRoot != null) _ghostRoot.SetActive(!stain);
            if (_stainRoot != null) _stainRoot.SetActive(stain);
        }

        void RefreshScale()
        {
            if (_stainBlot != null)
            {
                float blot = 0.42f + Strength * 0.62f + (IsBloom ? 0.10f : 0f);
                _stainBlot.localScale = new Vector3(blot, 1f, blot);
            }

            if (_stainRing != null)
            {
                float rim = 0.58f + Strength * 0.62f;
                _stainRing.localScale = new Vector3(rim, 1f, rim);
            }

            if (_ghostRing != null)
            {
                // An older ghost gets read as a thicker, more established rule.
                float ring = 0.58f + Mathf.Min(Age, 5) * 0.022f;
                _ghostRing.localScale = new Vector3(ring, 1f, ring);
            }
        }

        // ------------------------------------------------------------------
        // Simulation (driven by Sediment only while the game is running)
        // ------------------------------------------------------------------

        /// <summary>
        /// Advances fermentation. Only stains do anything with time; ghosts simply
        /// wait to be written over.
        /// </summary>
        public void Tick(float dt, int layer)
        {
            _eruptionCooldown = Mathf.Max(0f, _eruptionCooldown - dt);

            if (Kind != StratumKind.Stain || IsSpent) return;
            if (Strength < FaintStrength) return;

            // Deeper folios ferment faster; so do stronger blots.
            float chargeTime = Mathf.Lerp(38f, 17f, Mathf.Clamp01(Strength));
            chargeTime /= 1f + Mathf.Max(0, layer - 1) * 0.12f;

            Charge = Mathf.Min(1f, Charge + dt / Mathf.Max(5f, chargeTime));

            if (IsBloom) RefreshScale();
        }

        /// <summary>Ink written over a stain seals it into a clean ring.</summary>
        public void Cauterise()
        {
            if (Kind == StratumKind.Ghost) return;

            Kind = StratumKind.Ghost;
            Charge = 0f;
            Gushes = 0;
            Strength = Mathf.Max(Strength, 0.34f);
            _eruptionCooldown = 0f;

            ApplyKind();
            RefreshScale();
            Flash();
        }

        /// <summary>
        /// Light scours a stain. Returns true when the stain was reduced all the way
        /// down and converted into a harmless ghost.
        /// </summary>
        public bool Sour(float amount)
        {
            if (Kind != StratumKind.Stain) return false;

            Strength = Mathf.Max(0f, Strength - Mathf.Max(0f, amount));
            Charge = 0f;
            Flash();

            if (Strength <= FaintStrength)
            {
                Cauterise();
                return true;
            }

            RefreshScale();
            return false;
        }

        /// <summary>Reduces strength without changing kind (used when a page is washed).</summary>
        public void Thin(float amount)
        {
            Strength = Mathf.Max(0f, Strength - Mathf.Max(0f, amount));
            Charge = Mathf.Min(Charge, 0.4f);
            RefreshScale();
        }

        /// <summary>
        /// Increments the generation counter; called when a page turns. Named for the
        /// act rather than the noun so it cannot collide with the Age property above.
        /// </summary>
        public void AdvanceGeneration()
        {
            Age++;

            if (Kind == StratumKind.Stain)
            {
                Strength = Mathf.Max(0f, Strength - 0.06f);
                Charge = Mathf.Min(Charge, 0.55f);
            }

            RefreshScale();
        }

        /// <summary>Strengthens an existing blot when two kills land in the same place.</summary>
        public void Strengthen(float amount)
        {
            if (Kind != StratumKind.Stain) return;
            Strength = Mathf.Clamp01(Strength + Mathf.Max(0f, amount));
            Flash();
            RefreshScale();
        }

        /// <summary>Marks one eruption and starts this bloom's recovery.</summary>
        public void ConsumeEruption()
        {
            Gushes++;
            Charge = 0f;
            _eruptionCooldown = Mathf.Lerp(18f, 9f, Mathf.Clamp01(Strength));
            Flash();
            RefreshScale();
        }

        public void Flash()
        {
            _flash = 1f;
        }

        // ------------------------------------------------------------------
        // Presentation
        // ------------------------------------------------------------------

        void Update()
        {
            if (_material == null) return;

            float dt = Time.deltaTime;
            _flash = Mathf.MoveTowards(_flash, 0f, dt * 2.4f);

            float t = Time.time + _seed;
            float decay = Mathf.Lerp(1f, 0.40f, Mathf.Clamp01(Age / 11f));

            Color color;
            float emission;

            if (Kind == StratumKind.Stain)
            {
                bool bloom = IsBloom;

                float heat = Mathf.Clamp01(Charge * 0.66f + Strength * 0.44f);
                color = Color.Lerp(Palette.Erasure, Palette.ErasureGlow, 0.30f + heat * 0.70f);
                emission = 0.42f + heat * 2.10f;

                if (IsSpent)
                {
                    // A spent bloom is a scar: inert, dull, still legible.
                    color = Palette.Undertext;
                    emission = 0.28f;
                }
                else if (bloom)
                {
                    float throb = 0.5f + 0.5f * Mathf.Sin(t * 3.1f);
                    color = Color.Lerp(color, Palette.Alert, 0.30f + throb * 0.35f);
                    emission += 1.05f + throb * 0.85f;
                }

                if (_stainRing != null)
                {
                    _stainRing.localRotation = Quaternion.Euler(0f, t * (bloom ? 34f : 9f), 0f);
                }

                if (_stainBlot != null)
                {
                    float breathe = 1f + Mathf.Sin(t * 1.7f) * (bloom ? 0.09f : 0.03f);
                    float blot = 0.42f + Strength * 0.62f;
                    _stainBlot.localScale = new Vector3(blot * breathe, 1f, blot * breathe);
                }
            }
            else
            {
                // Ghosts sit just inside the living ink palette so that rebuilding on
                // an old seal feels like an inheritance rather than a coincidence.
                color = Color.Lerp(Palette.InkDeep, Palette.InkGlow, 0.45f);
                emission = 0.38f + 0.16f * Mathf.Sin(t * 1.5f);

                if (_ghostMark != null)
                {
                    _ghostMark.localRotation = Quaternion.Euler(
                        0f, _seed * 37f + Mathf.Sin(t * 0.4f) * 8f, 12f);
                }
            }

            emission = (emission + _flash * 2.6f) * decay;
            MaterialFactory.ApplyColor(_material, color);
            MaterialFactory.ApplyEmission(_material, color, emission);
        }

        void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
