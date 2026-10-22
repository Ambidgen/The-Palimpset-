using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// The scribe. A cloaked silhouette with a quill, driven entirely through
    /// InputRouter so the same script works on the old and new input backends.
    /// </summary>
    public class PalimpsestPlayer : MonoBehaviour
    {
        public const float InscribeCooldown = 0.26f;

        public float Radius { get { return 0.46f; } }
        public float Speed { get { return 9.2f; } }
        public bool CanInscribe { get { return _cooldown <= 0f; } }
        public float CooldownRemaining { get { return Mathf.Max(0f, _cooldown); } }
        public Vector3 EyePosition { get { return transform.position + Vector3.up * 0.75f; } }

        Transform _robe;
        Transform _hood;
        Transform _quill;
        Transform _halo;
        Transform _tracer;
        Material _haloMaterial;
        Material _tracerMaterial;

        float _cooldown;
        float _walkCycle;
        float _bob;
        float _lash;
        bool _tracerVisible;

        public static PalimpsestPlayer Create(Transform parent, Vector3 position)
        {
            GameObject root = new GameObject("The Scribe");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(position.x, 0f, position.z);

            PalimpsestPlayer player = root.AddComponent<PalimpsestPlayer>();
            player.BuildView();
            return player;
        }

        void BuildView()
        {
            Material robeMaterial = MaterialFactory.Lit(new Color(0.115f, 0.105f, 0.150f), 0.25f);

            _robe = MeshFactory.Primitive(PrimitiveType.Cylinder, "Robe", transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(0.34f, 0.52f, 0.34f), robeMaterial).transform;

            _hood = MeshFactory.Primitive(PrimitiveType.Sphere, "Hood", transform,
                new Vector3(0f, 1.03f, 0f), new Vector3(0.29f, 0.31f, 0.29f),
                MaterialFactory.Lit(new Color(0.085f, 0.080f, 0.115f), 0.3f)).transform;

            // A pale mask so the figure reads from a distance.
            MeshFactory.Primitive(PrimitiveType.Sphere, "Face", transform,
                new Vector3(0f, 1.02f, 0.20f), new Vector3(0.17f, 0.19f, 0.09f),
                MaterialFactory.Emissive(Palette.Vellum, 0.35f));

            GameObject quill = MeshFactory.Primitive(PrimitiveType.Cylinder, "Quill", transform,
                new Vector3(0.34f, 0.86f, 0.16f), new Vector3(0.028f, 0.36f, 0.028f),
                MaterialFactory.Emissive(Palette.InkGlow, 1.6f));
            quill.transform.localRotation = Quaternion.Euler(24f, 0f, -28f);
            _quill = quill.transform;

            MeshFactory.Primitive(PrimitiveType.Sphere, "Nib", transform,
                new Vector3(0.34f, 0.57f, 0.16f), new Vector3(0.10f, 0.10f, 0.10f),
                MaterialFactory.Emissive(Color.white, 2.6f));

            // Readiness halo.
            _halo = MeshFactory.MeshObject("Quill Halo", MeshFactory.UnitRing,
                MaterialFactory.DynamicEmissive(Palette.InkGlow, 1.6f), transform).transform;
            _halo.localPosition = new Vector3(0f, 0.028f, 0f);
            _halo.localScale = new Vector3(0.85f, 1f, 0.85f);
            _haloMaterial = _halo.GetComponent<MeshRenderer>().sharedMaterial;

            // Aiming tracer, stretched between the scribe and the cursor.
            _tracerMaterial = MaterialFactory.DynamicEmissive(Palette.InkGlow, 0.85f);
            GameObject tracer = MeshFactory.Primitive(PrimitiveType.Cube, "Tracer", transform,
                Vector3.zero, Vector3.one, _tracerMaterial);
            _tracer = tracer.transform;
            _tracer.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------

        public void Tick(float dt, Arena arena)
        {
            _cooldown = Mathf.Max(0f, _cooldown - dt);
            _lash = Mathf.MoveTowards(_lash, 0f, dt * 5.5f);

            Vector2 move = InputRouter.Move;
            Vector3 velocity = new Vector3(move.x, 0f, move.y) * Speed;
            Vector3 next = transform.position + velocity * dt;

            float limit = Arena.PageRadius - 0.5f;
            Vector2 flat = new Vector2(next.x, next.z);
            if (flat.magnitude > limit) flat = flat.normalized * limit;
            next = new Vector3(flat.x, 0f, flat.y);
            transform.position = next;

            bool walking = move.sqrMagnitude > 0.01f;
            if (walking)
            {
                _walkCycle += dt * 9f;
                Quaternion facing = Quaternion.LookRotation(new Vector3(move.x, 0f, move.y), Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, facing, 1f - Mathf.Exp(-14f * dt));
            }

            _bob = Mathf.Lerp(_bob, walking ? Mathf.Abs(Mathf.Sin(_walkCycle)) * 0.07f : 0f, 1f - Mathf.Exp(-9f * dt));
            if (_robe != null) _robe.localPosition = new Vector3(0f, 0.52f + _bob, 0f);
            if (_hood != null) _hood.localPosition = new Vector3(0f, 1.03f + _bob * 1.15f, 0f);

            if (_quill != null)
            {
                _quill.localRotation = Quaternion.Euler(24f - _lash * 42f, 0f, -28f);
            }

            UpdateHalo();
            UpdateTracer(arena);
        }

        void UpdateHalo()
        {
            if (_halo == null) return;

            float ready = CanInscribe ? 1f : 1f - Mathf.Clamp01(_cooldown / InscribeCooldown);
            float pulse = 0.86f + Mathf.Sin(Time.time * 4f) * 0.05f;
            _halo.localScale = new Vector3(0.85f * pulse, 1f, 0.85f * pulse);
            _halo.localRotation = Quaternion.Euler(0f, Time.time * 40f, 0f);

            if (_haloMaterial != null)
            {
                Color color = Color.Lerp(Palette.InkDeep, Palette.InkGlow, ready);
                MaterialFactory.ApplyEmission(_haloMaterial, color, 0.8f + ready * 1.6f);
            }
        }

        void UpdateTracer(Arena arena)
        {
            if (_tracer == null || arena == null) return;

            Vector3 target = arena.AimPoint;
            Vector3 origin = EyePosition;
            Vector3 flatTarget = new Vector3(target.x, origin.y, target.z);
            Vector3 delta = flatTarget - origin;
            float distance = delta.magnitude;

            bool show = distance > 0.9f && (arena.PointerState == InputFrame.Inscribing || arena.PointerState == InputFrame.Ready);
            if (_tracer.gameObject.activeSelf != show) _tracer.gameObject.SetActive(show);
            if (!show) return;

            Vector3 direction = delta / distance;
            _tracer.position = origin + direction * (distance * 0.5f);
            _tracer.rotation = Quaternion.LookRotation(direction, Vector3.up);
            _tracer.localScale = new Vector3(0.030f, 0.012f, distance);

            if (_tracerMaterial != null)
            {
                bool inReach = distance <= Arena.Reach;
                float strength = arena.PointerState == InputFrame.Inscribing ? 0.45f : 1.4f;
                MaterialFactory.ApplyEmission(_tracerMaterial,
                    inReach ? Palette.InkGlow : Palette.Alert, inReach ? strength : 1.1f);
            }
        }

        /// <summary>Called after a successful inscription to animate the quill's snap.</summary>
        public void Lash()
        {
            _lash = 1f;
            if (_quill != null) _quill.localRotation = Quaternion.Euler(24f - _lash * 42f, 0f, -28f);
        }

        public void SpendInk()
        {
            _cooldown = InscribeCooldown;
        }
    }
}
