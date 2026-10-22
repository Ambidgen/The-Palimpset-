using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// The things that eat the page.
    ///
    /// An erasure wants two things, in this order: the nearest living flame, and
    /// failing that, the scribe. It is not intelligent, only hungry. Candlelight
    /// burns it, wards cut it, and the quill's ink un-writes it.
    /// </summary>
    public class Erasure : MonoBehaviour
    {
        public float Radius { get { return _size; } }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float Speed { get; private set; }
        public bool IsDead { get { return Health <= 0f; } }

        public float BodyDamage { get { return _bodyDamage; } }
        public bool Provoked { get; private set; }

        float _size = 0.55f;
        float _bodyDamage = 14f;
        float _seed;
        float _flash;
        float _tetherPulse;

        Transform _body;
        Transform _skirt;
        Transform _ring;
        MeshRenderer _ringRenderer;
        MeshRenderer _bodyRenderer;
        Material _ringMaterial;
        Material _bodyMaterial;
        Material _skirtMaterial;
        Transform[] _motes;

        public static Erasure Create(Transform parent, Vector3 position, float size, float speed,
            float health, float bodyDamage)
        {
            GameObject root = new GameObject("Erasure");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(position.x, 0f, position.z);

            Erasure erasure = root.AddComponent<Erasure>();
            erasure._size = size;
            erasure.Speed = speed;
            erasure.MaxHealth = health;
            erasure.Health = health;
            erasure._bodyDamage = bodyDamage;
            erasure._seed = Random.Range(0f, 128f);
            erasure.BuildView();
            return erasure;
        }

        void BuildView()
        {
            _bodyMaterial = MaterialFactory.DynamicEmissive(Palette.Erasure, 1.5f);
            _body = MeshFactory.Primitive(PrimitiveType.Sphere, "Body", transform,
                new Vector3(0f, _size * 0.85f, 0f),
                new Vector3(_size * 1.15f, _size * 1.35f, _size * 1.15f),
                _bodyMaterial).transform;

            _skirtMaterial = MaterialFactory.DynamicEmissive(Palette.ErasureGlow, 0.8f);
            _skirt = MeshFactory.Primitive(PrimitiveType.Cylinder, "Skirt", transform,
                new Vector3(0f, _size * 0.22f, 0f),
                new Vector3(_size * 1.5f, _size * 0.24f, _size * 1.5f),
                _skirtMaterial).transform;

            // Ground reach: how close is too close.
            _ring = MeshFactory.MeshObject("Reach", MeshFactory.UnitRing,
                MaterialFactory.DynamicEmissive(Palette.ErasureGlow, 1.1f), transform).transform;
            _ring.localPosition = new Vector3(0f, 0.026f, 0f);
            _ring.localScale = new Vector3(_size * 1.9f, 1f, _size * 1.9f);
            _ringRenderer = _ring.GetComponent<MeshRenderer>();
            _ringMaterial = _ringRenderer.sharedMaterial;

            _motes = new Transform[3];
            for (int i = 0; i < _motes.Length; i++)
            {
                GameObject mote = MeshFactory.Primitive(PrimitiveType.Sphere, "Mote " + i, transform,
                    Vector3.zero, Vector3.one * (_size * 0.26f),
                    MaterialFactory.Emissive(Palette.ErasureGlow, 2.4f));
                _motes[i] = mote.transform;
            }
        }

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------

        public void Tick(float dt, Arena arena)
        {
            if (IsDead) return;

            _flash = Mathf.MoveTowards(_flash, 0f, dt * 3f);
            _tetherPulse += dt;

            Vector3 target = arena.ScribePosition;
            Candle candle = Provoked ? null : arena.NearestLivingCandle(transform.position);
            if (candle != null)
            {
                target = candle.transform.position;
            }

            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            Vector3 direction = distance > 0.001f ? toTarget / distance : Vector3.zero;

            // Keep a little personal space so the swarm reads as individuals.
            Vector3 separation = arena.SeparationFor(this);
            direction = (direction + separation * 0.85f).normalized;

            float speed = Speed * (1f - Mathf.Clamp01(_flash) * 0.28f);
            transform.position += direction * speed * dt;

            Animate(dt);

            // Candlelight scorches.
            for (int i = 0; i < arena.CandleCount; i++)
            {
                Candle flame = arena.CandleAt(i);
                if (flame == null || flame.IsOut) continue;
                float reach = flame.Radius + _size * 0.75f;
                float sqr = (flame.transform.position - transform.position).sqrMagnitude;
                if (sqr < reach * reach)
                {
                    TakeDamage(arena.ErasureBurnRate * dt);
                    if (IsDead) return;
                }
            }

            // Touching a flame drinks it.
            if (candle != null && distance < candle.Radius * 0.42f + _size)
            {
                candle.Drain(dt * 0.065f);
            }

            // Wards are cut, and cut back.
            Ward ward = arena.NearestWard(transform.position, _size + 0.55f);
            if (ward != null)
            {
                ward.TakeDamage(dt * 26f);
                TakeDamage(dt * 9f);
                if (IsDead) return;
            }

            // The scribe bleeds ink.
            float scribeReach = arena.ScribeRadius + _size * 0.9f;
            if ((arena.ScribePosition - transform.position).sqrMagnitude < scribeReach * scribeReach)
            {
                arena.WoundScribe(_bodyDamage * dt);
            }
        }

        void Animate(float dt)
        {
            float t = Time.time + _seed;

            if (_body != null)
            {
                _body.localPosition = new Vector3(
                    Mathf.Sin(t * 2.1f) * 0.05f,
                    _size * 0.85f + Mathf.Sin(t * 2.7f) * 0.06f,
                    Mathf.Cos(t * 1.9f) * 0.05f);
                float squash = 1f + Mathf.Sin(t * 3.3f) * 0.10f;
                _body.localScale = new Vector3(_size * 1.15f * squash, _size * 1.35f / squash, _size * 1.15f * squash);
            }

            if (_skirt != null)
            {
                _skirt.localRotation = Quaternion.Euler(0f, t * 40f, 0f);
            }

            if (_ring != null)
            {
                float beat = 1f + Mathf.Sin(t * 2.2f) * 0.06f;
                _ring.localScale = new Vector3(_size * 1.9f * beat, 1f, _size * 1.9f * beat);
            }

            if (_motes != null)
            {
                for (int i = 0; i < _motes.Length; i++)
                {
                    if (_motes[i] == null) continue;
                    float angle = t * 1.6f + i * (Mathf.PI * 2f / _motes.Length);
                    float radius = _size * (1.3f + 0.22f * Mathf.Sin(t * 2f + i));
                    _motes[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * radius,
                        0.15f + _size * 0.7f + Mathf.Sin(t * 3f + i) * 0.18f,
                        Mathf.Sin(angle) * radius);
                }
            }

            if (_bodyMaterial != null)
            {
                Color heat = Color.Lerp(Palette.InkGlow, Color.white, _flash);
                MaterialFactory.ApplyColor(_bodyMaterial, Color.Lerp(Palette.Erasure, heat, _flash * 0.7f));
                MaterialFactory.ApplyEmission(_bodyMaterial, Color.Lerp(Palette.ErasureGlow, Color.white, _flash), 1.4f + _flash * 3f);
            }

            if (_ringMaterial != null)
            {
                MaterialFactory.ApplyEmission(_ringMaterial,
                    Color.Lerp(Palette.ErasureGlow, Color.white, _flash), 0.9f + _flash * 2f);
            }
        }

        /// <summary>
        /// Provoked erasures ignore the flames and come for the scribe. The arena uses
        /// this when the page starts to turn, so every layer ends in a short siege.
        /// </summary>
        public void SetProvoked(bool provoked)
        {
            Provoked = provoked;
            _flash = Mathf.Max(_flash, 0.6f);
        }

        /// <summary>Applies damage from any source. Returns true if this blow was fatal.</summary>
        public bool TakeDamage(float amount)
        {
            if (IsDead) return false;
            Health -= Mathf.Max(0f, amount);
            _flash = Mathf.Min(1f, _flash + 0.55f);
            if (Health <= 0f)
            {
                Health = 0f;
                return true;
            }
            return false;
        }

        void OnDestroy()
        {
            if (_ringMaterial != null) Destroy(_ringMaterial);
            if (_bodyMaterial != null) Destroy(_bodyMaterial);
            if (_skirtMaterial != null) Destroy(_skirtMaterial);
        }
    }
}
