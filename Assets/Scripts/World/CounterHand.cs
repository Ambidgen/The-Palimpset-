using UnityEngine;
using UnityEngine.UI;

namespace Palimpsest
{
    /// <summary>What the earlier hand is doing to the page right now.</summary>
    public enum HandState
    {
        /// <summary>Rising out of the vellum; briefly beyond reach.</summary>
        Rising,

        /// <summary>Walking the page looking for something to un-write.</summary>
        Hunting,

        /// <summary>Standing on one of the player's seals and taking it apart.</summary>
        Unwriting,

        /// <summary>Pinching out a wick.</summary>
        Snuffing,

        /// <summary>Feeding on a ripe bloom in the sediment.</summary>
        Drinking,

        /// <summary>Badly hurt: falling back toward the margin to recover.</summary>
        Reeling
    }

    /// <summary>
    /// THE COUNTER-HAND - the earlier hand that is still at work beneath the page.
    ///
    /// <para>
    /// The palimpsest was already literal: kills leave stains, seals leave ghosts, and
    /// the whole record survives death. What was missing was an <i>author</i> for the
    /// undertext. The erasures are appetite without intent; they eat. The counter-hand
    /// is the thing that was writing before you were, and it has not stopped. It does
    /// not eat the page - it <b>edits</b> it. It un-writes your seals, pinches out your
    /// wicks, and it reads your own sediment to feed itself.
    /// </para>
    ///
    /// <para>
    /// It is deliberately not a bigger erasure. Ink cannot touch it (it is not text, it
    /// is a hand), so the quill's stroke is useless against it. It can only be burned:
    /// candlelight scorches it, <c>Shift</c> illumination staggers it, and <c>E</c> -
    /// scraping the page - wounds it worse than anything else, because scraping is the
    /// one verb that removes writing outright. Killing it also poisons the page: the
    /// grave of a hand is the heaviest stain it can carry, so victory over the counter-hand
    /// feeds the sediment that spawns everything else.
    /// </para>
    ///
    /// <para>
    /// It arrives from <see cref="FirstLayer"/> onward, once per layer, and it is
    /// banished - never killed - when the page turns. Each arrival is a generation
    /// deeper, and every generation eats faster.
    /// </para>
    /// </summary>
    public class CounterHand : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Tuning
        // ------------------------------------------------------------------

        /// <summary>The page has to be three hands deep before another hand shows up.</summary>
        public const int FirstLayer = 3;

        /// <summary>Body radius; used for contact and for how deep it stands in a halo.</summary>
        public const float Radius = 0.52f;

        /// <summary>Seconds of health the counter-hand takes off a seal per second.</summary>
        const float UnwriteRate = 26f;

        /// <summary>Fuel taken off a wick per second (an erasure drinks 0.065).</summary>
        const float WickDrain = 0.085f;

        /// <summary>Damage per second while it is standing inside a living halo.</summary>
        const float LightBurnRate = 24f;

        /// <summary>Healing per second while it falls back toward the margin.</summary>
        const float RecoveryRate = 7f;

        /// <summary>Health restored by draining one bloom dry.</summary>
        const float BloomMeal = 34f;

        /// <summary>A single blow at least this heavy puts it into retreat.</summary>
        const float ReelThreshold = 26f;

        /// <summary>How long a retreat lasts.</summary>
        const float ReelLength = 2.6f;

        /// <summary>Seconds spent rising out of the vellum before it can act.</summary>
        const float RiseTime = 0.9f;

        public HandState State { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public int Generation { get; private set; }
        public bool IsUnmade { get; private set; }
        public float Speed { get { return _speed; } }

        /// <summary>What it is doing, in one line, for the overlay above it.</summary>
        public string Caption { get { return _captionLife > 0f ? _caption : string.Empty; } }
        public float CaptionLife { get { return _captionLife; } }

        /// <summary>
        /// The multiplier on everything this hand consumes: how fast it un-writes a
        /// seal, how fast it pinches a wick.
        ///
        /// "Implacable" is the exact word here rather than a flourish, because the
        /// quantity is defined by what it refuses to do. It never relents mid-layer,
        /// never bargains, never tires as the wax runs down and never slows because the
        /// player is losing - there is no term anywhere in this file that reduces it.
        /// Each generation simply raises a rate that has no off switch, which is
        /// precisely what implacable means: incapable of being appeased.
        /// </summary>
        float _implacableAppetite = 1f;

        float _speed = 5.4f;
        float _bodyDamage = 16f;
        float _flash;
        float _scorched;
        float _rise;
        float _invulnerable;
        float _reelTimer;
        float _scratch;
        float _drinkCooldown;
        float _captionLife;
        float _seed;
        string _caption = string.Empty;

        Vector3 _lastBlow;
        Ward _targetWard;
        Candle _targetCandle;

        Transform _view;
        Transform _robe;
        Transform _hood;
        Transform _quill;
        Transform _halo;
        Transform _shadow;
        Transform[] _letters;

        Material _robeMaterial;
        Material _haloMaterial;
        Material _shadowMaterial;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public static CounterHand Create(Transform parent, Vector3 position, int layer, int generation)
        {
            GameObject root = new GameObject("The Counter-Hand");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(position.x, 0f, position.z);

            CounterHand hand = root.AddComponent<CounterHand>();
            hand.Generation = Mathf.Max(1, generation);
            hand.ApplyTuning(layer, hand.Generation);
            hand._seed = Random.Range(0f, 200f);
            hand._lastBlow = root.transform.position;
            hand.State = HandState.Rising;
            hand.BuildView();
            return hand;
        }

        void Awake()
        {
            /*
             * A note from the hand that came before yours.
             * Syllables: 5 - 7 - 5.
             */
            Debug.Log("I hold what fits now / the rest is folded away / yet the page recalls");
        }

        void ApplyTuning(int layer, int generation)
        {
            // Tuned against the two verbs that can reach it: a close-range illuminate
            // lands 46 and a scrape lands 62, so a hand two or three pulses deep dies,
            // and a hand baited into a full halo dies in about four seconds of light.
            MaxHealth = 52f + Mathf.Max(1, layer) * 14f + (generation - 1) * 10f;
            Health = MaxHealth;

            // Capped: an appetite that grew without limit would eventually delete a seal
            // faster than the quill can write one, and the duel would stop being a duel.
            _implacableAppetite = Mathf.Min(1.45f, 1f + (generation - 1) * 0.12f);
            _speed = Mathf.Min(7.20f, 5.10f + layer * 0.22f);
            _bodyDamage = 16f + layer * 1.2f;
        }

        void BuildView()
        {
            // A mirror of the scribe, drawn in the palette of the undertext: taller,
            // thinner, and lit from inside by the same violet the erasures wear.
            _robeMaterial = MaterialFactory.DynamicEmissive(Color.Lerp(Palette.Erasure, Palette.Night, 0.25f), 0.55f);
            _haloMaterial = MaterialFactory.DynamicEmissive(Palette.ErasureGlow, 1.8f);
            _shadowMaterial = MaterialFactory.DynamicEmissive(Palette.Erasure, 0.5f);

            // Everything that rises out of the page hangs off one transform so the
            // arrival can be animated as a single motion.
            GameObject view = new GameObject("Figure");
            view.transform.SetParent(transform, false);
            _view = view.transform;

            _robe = MeshFactory.Primitive(PrimitiveType.Cylinder, "Counter-Robe", _view,
                new Vector3(0f, 0.66f, 0f), new Vector3(0.32f, 0.66f, 0.32f), _robeMaterial).transform;

            _hood = MeshFactory.Primitive(PrimitiveType.Sphere, "Counter-Hood", _view,
                new Vector3(0f, 1.30f, 0f), new Vector3(0.28f, 0.34f, 0.28f), _robeMaterial).transform;

            // No face. Where the scribe has a pale mask the counter-hand has a wound.
            MeshFactory.Primitive(PrimitiveType.Sphere, "Counter-Mask", _view,
                new Vector3(0f, 1.29f, 0.19f), new Vector3(0.16f, 0.20f, 0.08f),
                MaterialFactory.Emissive(Palette.Alert, 0.85f));

            _quill = MeshFactory.Primitive(PrimitiveType.Cylinder, "Counter-Quill", _view,
                new Vector3(-0.34f, 1.06f, 0.14f), new Vector3(0.030f, 0.44f, 0.030f),
                Quaternion.Euler(20f, 0f, 30f), MaterialFactory.Emissive(Palette.ErasureGlow, 2.2f)).transform;

            MeshFactory.Primitive(PrimitiveType.Sphere, "Counter-Nib", _view,
                new Vector3(-0.34f, 0.70f, 0.14f), new Vector3(0.10f, 0.10f, 0.10f),
                MaterialFactory.Emissive(Color.Lerp(Palette.ErasureGlow, Color.white, 0.4f), 2.8f));

            // Loose letters that fell out of an older sentence and never stopped orbiting.
            _letters = new Transform[3];
            for (int i = 0; i < _letters.Length; i++)
            {
                GameObject letter = MeshFactory.Primitive(PrimitiveType.Cube, "Loose Letter " + i, _view,
                    Vector3.zero, new Vector3(0.09f, 0.13f, 0.02f),
                    MaterialFactory.Emissive(Palette.ErasureGlow, 1.8f));
                _letters[i] = letter.transform;
            }

            // Ground shadow and reach-ring, left on the root so they stay on the vellum
            // while the figure rises.
            _shadow = MeshFactory.MeshObject("Counter-Shadow", MeshFactory.UnitDisc, _shadowMaterial, transform).transform;
            _shadow.localPosition = new Vector3(0f, 0.010f, 0f);
            _shadow.localScale = new Vector3(1.15f, 1f, 1.15f);

            _halo = MeshFactory.MeshObject("Counter-Halo", MeshFactory.UnitRing, _haloMaterial, transform).transform;
            _halo.localPosition = new Vector3(0f, 0.030f, 0f);
            _halo.localScale = new Vector3(1.05f, 1f, 1.05f);
        }

        // ------------------------------------------------------------------
        // Simulation - driven by CounterHandDirector only while a run is live
        // ------------------------------------------------------------------

        public void Tick(float dt, Arena arena)
        {
            if (IsUnmade || arena == null) return;

            _flash = Mathf.MoveTowards(_flash, 0f, dt * 2.6f);
            _scorched = Mathf.MoveTowards(_scorched, 0f, dt * 1.4f);
            _invulnerable = Mathf.Max(0f, _invulnerable - dt);
            _captionLife = Mathf.Max(0f, _captionLife - dt);
            _drinkCooldown = Mathf.Max(0f, _drinkCooldown - dt);

            if (State == HandState.Rising)
            {
                Rise(dt);
            }
            else
            {
                BurnInLight(dt, arena);
                if (IsUnmade) return;

                if (!TryDrinkBloom(dt))
                {
                    switch (State)
                    {
                        case HandState.Unwriting: Unwrite(dt, arena); break;
                        case HandState.Snuffing: Snuff(dt, arena); break;
                        case HandState.Reeling: Reel(dt, arena); break;
                        default: Hunt(dt, arena); break;
                    }
                }
            }

            Touch(dt, arena);
            Animate(dt);
        }

        void Rise(float dt)
        {
            _rise = Mathf.Min(1f, _rise + dt / RiseTime);
            float t = Mathf.SmoothStep(0f, 1f, _rise);

            if (_view != null)
            {
                _view.localPosition = new Vector3(0f, Mathf.Lerp(-1.6f, 0f, t), 0f);
                float grow = Mathf.Lerp(0.35f, 1f, t);
                _view.localScale = new Vector3(grow, grow, grow);
            }

            // Untouchable while it is still coming through the page, so an illuminate
            // fired at the arrival ring cannot delete it before it exists.
            _invulnerable = Mathf.Max(0f, (1f - t) * 0.8f);

            if (_rise >= 1f)
            {
                State = HandState.Hunting;
                Say("AN EARLIER HAND IS STILL WRITING", 2.8f);
            }
        }

        void BurnInLight(float dt, Arena arena)
        {
            for (int i = 0; i < arena.CandleCount; i++)
            {
                Candle flame = arena.CandleAt(i);
                if (flame == null || flame.IsOut) continue;

                float reach = flame.Radius + Radius * 0.5f;
                float sqr = (flame.transform.position - transform.position).sqrMagnitude;
                if (sqr > reach * reach) continue;

                _scorched = 1f;
                TakeDamage(LightBurnRate * (0.45f + flame.Fuel) * dt, flame.transform.position, 0f);
                if (IsUnmade) return;
            }
        }

        /// <summary>
        /// The counter-hand reads the same sediment the player does. A ripe bloom is a
        /// meal: it drains the charge, heals the hand, and - because the charge is gone -
        /// stops that spot from erupting. Feeding it is therefore a real trade rather
        /// than a straight penalty.
        /// </summary>
        bool TryDrinkBloom(float dt)
        {
            if (_drinkCooldown > 0f || Health >= MaxHealth) return false;

            Sediment sediment = Sediment.Instance;
            if (sediment == null || sediment.All == null) return false;

            Stratum bloom = null;
            float bestSqr = 1.30f * 1.30f;

            for (int i = 0; i < sediment.All.Count; i++)
            {
                Stratum stratum = sediment.All[i];
                if (stratum == null || !stratum.IsBloom) continue;

                float sqr = (stratum.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bloom = stratum;
                }
            }

            if (bloom == null) return false;

            Health = Mathf.Min(MaxHealth, Health + BloomMeal);
            _drinkCooldown = 5f;
            bloom.ConsumeEruption();

            State = HandState.Drinking;
            Say("IT DRINKS THE BLOOM", 1.9f);
            TransientRing.Spawn(transform.parent, bloom.transform.position, 0.3f, 2.4f,
                Palette.ErasureGlow, 0.7f, 3.0f);

            // It stands still to feed, which is the window the player gets.
            Face(bloom.transform.position, dt);
            return true;
        }

        void Hunt(float dt, Arena arena)
        {
            State = HandState.Hunting;

            // Badly hurt, it stops vandalising and runs for the margin.
            if (Health < MaxHealth * 0.30f)
            {
                BeginReel();
                return;
            }

            Ward ward = arena.NearestWard(transform.position, Arena.PageRadius);
            if (ward != null)
            {
                _targetWard = ward;
                _targetCandle = null;

                float distance = FlatDistance(ward.transform.position);
                if (distance <= 1.30f)
                {
                    State = HandState.Unwriting;
                    Say("IT IS UN-WRITING YOUR SEAL", 2.0f);
                }
                else
                {
                    MoveToward(ward.transform.position, dt, arena, true);
                }
                return;
            }

            Candle candle = WeakestCandle(arena);
            if (candle != null)
            {
                _targetCandle = candle;
                if (FlatDistance(candle.transform.position) <= 1.45f)
                {
                    State = HandState.Snuffing;
                    Say("IT PINCHES THE WICK", 2.0f);
                }
                else
                {
                    MoveToward(candle.transform.position, dt, arena, true);
                }
                return;
            }

            // A page with nothing written on it and nothing burning: the hand drifts
            // toward the middle and waits for you to make something worth ruining.
            MoveToward(Vector3.zero, dt, arena, true);
        }

        void Unwrite(float dt, Arena arena)
        {
            Ward ward = _targetWard;
            if (ward == null || ward.IsBroken)
            {
                _targetWard = null;
                State = HandState.Hunting;
                return;
            }

            // Read the position first: the stroke that kills a ward destroys it, and a
            // destroyed transform must never be read again.
            Vector3 at = ward.transform.position;

            if (FlatDistance(at) > 1.55f)
            {
                MoveToward(at, dt, arena, false);
                return;
            }

            Face(at, dt);
            _scratch += dt;

            bool broken = ward.TakeDamage(UnwriteRate * _implacableAppetite * dt);

            if (_scratch >= 0.24f)
            {
                _scratch = 0f;
                TransientRing.Spawn(transform.parent, at, 0.18f, 1.0f, Palette.ErasureGlow, 0.30f, 2.0f);
            }

            if (broken || ward.IsBroken)
            {
                _targetWard = null;
                State = HandState.Hunting;

                // Overwriting is not cleaning: the counter-hand's own ink ferments like
                // anything else written on this page.
                if (Sediment.Instance != null) Sediment.Instance.Stain(at, 0.30f);
                TransientRing.Spawn(transform.parent, at, 0.25f, 1.9f, Palette.Alert, 0.55f, 2.8f);
                Say("YOUR SEAL IS OVERWRITTEN", 1.8f);
            }
        }

        void Snuff(float dt, Arena arena)
        {
            Candle candle = _targetCandle;
            if (candle == null || candle.IsOut)
            {
                _targetCandle = null;
                State = HandState.Hunting;
                return;
            }

            Vector3 at = candle.transform.position;
            if (FlatDistance(at) > 1.65f)
            {
                MoveToward(at, dt, arena, false);
                return;
            }

            Face(at, dt);
            candle.Drain(WickDrain * _implacableAppetite * dt);

            if (candle.IsOut)
            {
                _targetCandle = null;
                State = HandState.Hunting;
                Say("A FLAME IS PINCHED OUT", 1.9f);
                TransientRing.Spawn(transform.parent, at, 0.3f, 2.0f, Palette.Alert, 0.55f, 1.6f);
            }
        }

        void BeginReel()
        {
            State = HandState.Reeling;
            _reelTimer = ReelLength;
            Say("THE HAND REELS", 1.5f);
        }

        void Reel(float dt, Arena arena)
        {
            _reelTimer -= dt;
            Health = Mathf.Min(MaxHealth, Health + RecoveryRate * dt);

            Vector3 away = transform.position - _lastBlow;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = transform.position;
            if (away.sqrMagnitude < 0.01f) away = new Vector3(1f, 0f, 0f);

            Vector3 margin = away.normalized * (Arena.PageRadius - 1.4f);
            MoveToward(margin, dt, arena, true);

            if (_reelTimer <= 0f) State = HandState.Hunting;
        }

        void Touch(float dt, Arena arena)
        {
            float reach = arena.ScribeRadius + Radius * 0.9f;
            if ((arena.ScribePosition - transform.position).sqrMagnitude < reach * reach)
            {
                arena.WoundScribe(_bodyDamage * dt);
            }
        }

        // ------------------------------------------------------------------
        // Movement
        // ------------------------------------------------------------------

        void MoveToward(Vector3 target, float dt, Arena arena, bool avoidLight)
        {
            Vector3 desired = target - transform.position;
            desired.y = 0f;

            if (desired.sqrMagnitude < 0.0001f) return;

            desired.Normalize();

            // It never walks a perfect line, so its path reads as a hand searching.
            float wobble = Mathf.Sin(Time.time * 1.7f + _seed) * 0.28f;
            desired = Quaternion.AngleAxis(wobble * 34f, Vector3.up) * desired;

            Vector3 avoid = Vector3.zero;
            if (avoidLight)
            {
                for (int i = 0; i < arena.CandleCount; i++)
                {
                    Candle flame = arena.CandleAt(i);
                    if (flame == null || flame.IsOut) continue;

                    Vector3 delta = transform.position - flame.transform.position;
                    delta.y = 0f;
                    float distance = delta.magnitude;
                    float reach = flame.Radius + 1.15f;
                    if (distance < 0.0001f || distance >= reach) continue;

                    avoid += (delta / distance) * (1f - distance / reach) * 1.65f;
                }
            }

            Vector3 sum = desired + avoid;
            if (sum.sqrMagnitude < 0.0001f) sum = avoid.sqrMagnitude > 0.0001f ? avoid : desired;

            Vector3 move = sum.normalized;
            transform.position = ClampToPage(transform.position + move * _speed * dt);

            Quaternion facing = Quaternion.LookRotation(new Vector3(move.x, 0f, move.z), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, facing, 1f - Mathf.Exp(-8f * dt));
        }

        void Face(Vector3 target, float dt)
        {
            Vector3 delta = target - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f) return;

            Quaternion facing = Quaternion.LookRotation(delta.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, facing, 1f - Mathf.Exp(-10f * dt));
        }

        Vector3 ClampToPage(Vector3 position)
        {
            Vector2 flat = new Vector2(position.x, position.z);
            float limit = Arena.PageRadius - 0.7f;
            if (flat.magnitude > limit) flat = flat.normalized * limit;
            return new Vector3(flat.x, 0f, flat.y);
        }

        float FlatDistance(Vector3 other)
        {
            Vector3 delta = other - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        Candle WeakestCandle(Arena arena)
        {
            Candle best = null;
            for (int i = 0; i < arena.CandleCount; i++)
            {
                Candle candle = arena.CandleAt(i);
                if (candle == null || candle.IsOut) continue;
                if (best == null || candle.Fuel < best.Fuel) best = candle;
            }
            return best;
        }

        // ------------------------------------------------------------------
        // Damage
        // ------------------------------------------------------------------

        /// <summary>
        /// Called by the director when the player fires one of the two verbs that can
        /// actually reach this thing: illumination, or scraping the page.
        /// </summary>
        public void ApplyPulse(Vector3 center, float radius, float damage, float knockback)
        {
            if (IsUnmade || radius <= 0f) return;

            Vector3 delta = transform.position - center;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > radius) return;

            float falloff = 1f - Mathf.Clamp01(distance / radius);
            TakeDamage(damage * (0.5f + 0.5f * falloff), center, knockback * (0.5f + falloff));
        }

        public void TakeDamage(float amount, Vector3 from, float knockback)
        {
            if (IsUnmade || _invulnerable > 0f || amount <= 0f) return;

            Health -= amount;
            _flash = 1f;
            _lastBlow = from;

            if (knockback > 0f)
            {
                Vector3 push = transform.position - from;
                push.y = 0f;
                if (push.sqrMagnitude < 0.0001f) push = transform.position;
                if (push.sqrMagnitude < 0.0001f) push = new Vector3(1f, 0f, 0f);
                transform.position = ClampToPage(transform.position + push.normalized * knockback);
            }

            if (Health <= 0f)
            {
                Health = 0f;
                Unmake();
                return;
            }

            if (amount >= ReelThreshold && State != HandState.Reeling) BeginReel();
        }

        /// <summary>
        /// The hand is not killed so much as un-written. Its grave is the heaviest stain
        /// the page can hold, which is the price of winning the duel: the sediment that
        /// spawns every future erasure is fed by the thing you just removed.
        /// </summary>
        void Unmake()
        {
            IsUnmade = true;

            Vector3 at = transform.position;

            if (Sediment.Instance != null) Sediment.Instance.Stain(at, 0.85f);

            TransientRing.Spawn(transform.parent, at, 0.5f, 5.0f, Palette.Alert, 1.0f, 3.4f);
            TransientRing.Spawn(transform.parent, at, 0.2f, 2.2f, Palette.ErasureGlow, 0.65f, 2.6f);

            Say("THE EARLIER HAND IS UN-WRITTEN", 2.6f);

            // The figure goes; the root object stays put until the page turns and takes
            // the whole layer with it, so nothing above can hold a stale reference to a
            // transform that has been destroyed mid-frame.
            if (_view != null) Destroy(_view.gameObject);
            if (_halo != null) Destroy(_halo.gameObject);
            if (_shadow != null) Destroy(_shadow.gameObject);
            _view = null;
            _halo = null;
            _shadow = null;
            _letters = null;

            enabled = false;
        }

        void Say(string line, float seconds)
        {
            _caption = line;
            _captionLife = seconds;
        }

        // ------------------------------------------------------------------
        // Presentation
        // ------------------------------------------------------------------

        void Animate(float dt)
        {
            float t = Time.time + _seed;
            bool working = State == HandState.Unwriting || State == HandState.Snuffing;

            if (_robe != null)
            {
                float breathe = Mathf.Sin(t * (working ? 7.5f : 1.6f)) * (working ? 0.035f : 0.014f);
                _robe.localPosition = new Vector3(0f, 0.66f + breathe, 0f);
            }

            if (_hood != null)
            {
                _hood.localPosition = new Vector3(Mathf.Sin(t * 0.9f) * 0.02f, 1.30f, 0f);
            }

            if (_quill != null)
            {
                float scratch = working ? Mathf.Sin(t * 19f) * 11f : Mathf.Sin(t * 1.3f) * 4f;
                _quill.localRotation = Quaternion.Euler(20f + scratch * 0.4f, 0f, 30f + scratch);
            }

            if (_letters != null)
            {
                for (int i = 0; i < _letters.Length; i++)
                {
                    if (_letters[i] == null) continue;

                    float angle = t * 1.1f + i * (Mathf.PI * 2f / _letters.Length);
                    float orbit = 0.62f + 0.10f * Mathf.Sin(t * 1.8f + i);
                    _letters[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * orbit,
                        0.95f + Mathf.Sin(t * 2.2f + i * 1.7f) * 0.22f,
                        Mathf.Sin(angle) * orbit);
                    _letters[i].localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 8f);
                }
            }

            if (_halo != null)
            {
                float beat = 1f + Mathf.Sin(t * (working ? 6f : 2f)) * 0.07f;
                float scale = 1.05f * beat;
                _halo.localScale = new Vector3(scale, 1f, scale);
                _halo.localRotation = Quaternion.Euler(0f, -t * 26f, 0f);
            }

            if (_haloMaterial != null)
            {
                Color haloColor = Color.Lerp(Palette.ErasureGlow, Color.white, _scorched * 0.55f);
                MaterialFactory.ApplyEmission(_haloMaterial, haloColor,
                    1.4f + _flash * 2.6f + _scorched * 1.6f);
            }

            if (_robeMaterial != null)
            {
                Color robeColor = Color.Lerp(
                    Color.Lerp(Palette.Erasure, Palette.Night, 0.25f),
                    Color.white, _flash * 0.65f);
                MaterialFactory.ApplyColor(_robeMaterial, robeColor);
                MaterialFactory.ApplyEmission(_robeMaterial,
                    Color.Lerp(Palette.ErasureGlow, Color.white, _flash), 0.55f + _flash * 2.4f);
            }

            if (_shadowMaterial != null)
            {
                MaterialFactory.ApplyEmission(_shadowMaterial, Palette.Erasure,
                    0.35f + Mathf.Clamp01(1f - Health / MaxHealth) * 0.9f);
            }
        }

        void OnDestroy()
        {
            if (_robeMaterial != null) Destroy(_robeMaterial);
            if (_haloMaterial != null) Destroy(_haloMaterial);
            if (_shadowMaterial != null) Destroy(_shadowMaterial);
        }
    }

    /// <summary>
    /// Runs the counter-hand without the arena having to know it exists.
    ///
    /// <para>
    /// The hand is bolted onto a finished game, so this director is deliberately
    /// additive: it attaches itself at runtime, finds the layer transform the arena
    /// already builds, and drives the hand from the arena's public surface only. It
    /// reads the page turning off <see cref="Arena.Layer"/>, and it detects the two
    /// verbs that can hurt the hand by watching the arena's cooldown readouts rise -
    /// a cooldown only ever counts down, so any increase is the frame the player
    /// fired. That keeps the duel in one file instead of threaded through the arena.
    /// </para>
    /// </summary>
    public class CounterHandDirector : MonoBehaviour
    {
        /// <summary>Seconds after a page turns before the next hand comes up through it.</summary>
        const float ArrivalDelay = 7f;

        /// <summary>Illuminate: wide, clean light. Stings, and staggers.</summary>
        const float IlluminateRadius = 7.5f;
        const float IlluminateDamage = 46f;
        const float IlluminateKnockback = 1.5f;

        /// <summary>Scrape: narrow, and it removes writing outright - so it hurts most.</summary>
        const float ScrapeRadius = 4.4f;
        const float ScrapeDamage = 62f;
        const float ScrapeKnockback = 2.4f;

        public static CounterHandDirector Instance { get; private set; }

        /// <summary>True once a hand is on the page and still working.</summary>
        public bool IsHandAtWork { get { return _hand != null && !_hand.IsUnmade; } }
        public CounterHand Hand { get { return _hand; } }

        Arena _arena;
        CounterHand _hand;
        Transform _layerRoot;

        int _seenLayer = -1;
        int _generation;
        float _arrivalTimer = ArrivalDelay;
        bool _unmadeThisLayer;

        float _illuminateWatch;
        float _scrapeWatch;

        Camera _view;
        Canvas _canvas;
        RectTransform _canvasRect;
        RectTransform _marker;
        Image _barFill;
        Text _nameplate;
        Text _caption;
        float _captionLife;
        string _captionLine = string.Empty;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            if (Instance != null) return;
            Build();
        }

        public static CounterHandDirector Build()
        {
            GameObject go = new GameObject("Counter-Hand Director");
            return go.AddComponent<CounterHandDirector>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildOverlay();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            Bind();
            if (_arena == null) return;

            float dt = Time.deltaTime;
            if (dt > 0.08f) dt = 0.08f;

            // The arena only ever destroys the *children* of its layer transform, so a
            // change of layer number is a reliable signal that the previous hand - and
            // every seal and wick it was working on - has been taken off the page.
            if (_arena.Layer != _seenLayer)
            {
                _seenLayer = _arena.Layer;
                _hand = null;
                _unmadeThisLayer = false;
                _arrivalTimer = ArrivalDelay;

                // The verb watches are deliberately *not* reset here: a cooldown that is
                // still counting down when the page turns would otherwise read as a rise
                // on the next frame and give the arriving hand a free hit.
            }

            if (_arena.IsTitle)
            {
                _arrivalTimer = ArrivalDelay;
                if (_canvas != null) _canvas.gameObject.SetActive(false);
                return;
            }

            if (_arena.IsOver || _arena.IsPaused) return;

            ReadVerbs();

            if (_arena.Layer >= CounterHand.FirstLayer && _hand == null && !_unmadeThisLayer)
            {
                _arrivalTimer -= dt;
                if (_arrivalTimer <= 0f) Spawn();
            }

            if (_hand != null)
            {
                if (!_hand.IsUnmade)
                {
                    _hand.Tick(dt, _arena);
                    if (_hand.IsUnmade) _unmadeThisLayer = true;
                }

                if (_hand.CaptionLife > 0f)
                {
                    _captionLine = _hand.Caption;
                    _captionLife = _hand.CaptionLife;
                }
            }

            UpdateOverlay(dt);
        }

        void Bind()
        {
            if (_arena != null) return;

            _arena = Arena.Instance;
            if (_arena == null) return;

            _layerRoot = null;
        }

        // ------------------------------------------------------------------
        // Spawning
        // ------------------------------------------------------------------

        void Spawn()
        {
            Transform root = LayerRoot();
            if (root == null)
            {
                _arrivalTimer = 0.5f;
                return;
            }

            _generation++;
            Vector3 at = ArrivalPoint();

            _hand = CounterHand.Create(root, at, _arena.Layer, _generation);

            TransientRing.Spawn(root, at, 0.4f, 5.4f, Palette.Alert, 1.2f, 3.2f);
            TransientRing.Spawn(root, at, 0.2f, 2.4f, Palette.ErasureGlow, 0.7f, 2.4f);
        }

        /// <summary>
        /// The hand always comes up through the margin, on the far side of the page
        /// from the scribe, so its arrival is a thing you see coming rather than a
        /// thing that happens to you.
        /// </summary>
        Vector3 ArrivalPoint()
        {
            Vector3 scribe = _arena.ScribePosition;
            float radius = Arena.PageRadius - 1.6f;
            Vector3 best = new Vector3(radius, 0f, 0f);
            float bestSqr = -1f;

            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                Vector3 candidate = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                float sqr = (candidate - scribe).sqrMagnitude;
                if (sqr > bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>
        /// Resolves the transform the arena builds layers under. The name is fixed in
        /// Arena.BuildPage, and the arena never destroys the root itself, so this is
        /// stable for a whole session; the deep search is only a fallback.
        /// </summary>
        Transform LayerRoot()
        {
            if (_layerRoot != null) return _layerRoot;
            if (_arena == null) return null;

            Transform page = _arena.transform.Find("The Page");
            if (page != null)
            {
                Transform layer = page.Find("Layer");
                if (layer != null)
                {
                    _layerRoot = layer;
                    return _layerRoot;
                }
            }

            _layerRoot = FindDeep(_arena.transform, "Layer");
            return _layerRoot;
        }

        static Transform FindDeep(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;

                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ------------------------------------------------------------------
        // The two verbs that can reach it
        // ------------------------------------------------------------------

        /// <summary>
        /// A cooldown only ever counts down, so watching the arena's public readouts is
        /// enough to know the exact frame a verb fired - no event plumbing required.
        /// </summary>
        void ReadVerbs()
        {
            float illuminate = _arena.IlluminateCooldownRemaining;
            if (illuminate > _illuminateWatch + 0.001f && _hand != null && !_hand.IsUnmade)
            {
                _hand.ApplyPulse(_arena.ScribePosition, IlluminateRadius, IlluminateDamage, IlluminateKnockback);
            }
            _illuminateWatch = illuminate;

            float scrape = _arena.ScrapeCooldownRemaining;
            if (scrape > _scrapeWatch + 0.001f && _hand != null && !_hand.IsUnmade)
            {
                _hand.ApplyPulse(_arena.ScribePosition, ScrapeRadius, ScrapeDamage, ScrapeKnockback);
            }
            _scrapeWatch = scrape;
        }

        // ------------------------------------------------------------------
        // Overlay: a nameplate and a bar that hang over the hand
        // ------------------------------------------------------------------

        void BuildOverlay()
        {
            GameObject root = new GameObject("Counter-Hand Overlay");
            root.transform.SetParent(transform, false);

            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 220;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRect = root.GetComponent<RectTransform>();

            GameObject markerObject = new GameObject("Marker");
            markerObject.transform.SetParent(root.transform, false);
            _marker = markerObject.AddComponent<RectTransform>();

            // The canvas root pivots on its centre, and ScreenPointToLocalPointInRectangle
            // returns a point relative to that pivot, so the marker must be anchored to
            // the centre too or every world anchor would land half a screen off.
            _marker.anchorMin = new Vector2(0.5f, 0.5f);
            _marker.anchorMax = new Vector2(0.5f, 0.5f);
            _marker.pivot = new Vector2(0.5f, 0.5f);
            _marker.sizeDelta = Vector2.zero;

            Font serif = ResolveSerif();

            _caption = OverlayText(_marker, "Caption", serif, 22, FontStyle.Italic,
                new Vector2(0f, 96f), new Vector2(680f, 30f), Palette.Alert);
            _nameplate = OverlayText(_marker, "Nameplate", serif, 24, FontStyle.Bold,
                new Vector2(0f, 62f), new Vector2(520f, 30f), Palette.Text);

            GameObject frame = new GameObject("Bar Frame");
            frame.transform.SetParent(_marker, false);
            Image frameImage = frame.AddComponent<Image>();
            frameImage.color = Palette.Alpha(Palette.Night, 0.85f);
            frameImage.raycastTarget = false;
            Layout(frame.GetComponent<RectTransform>(), new Vector2(0f, 38f), new Vector2(232f, 12f));

            GameObject fill = new GameObject("Bar Fill");
            fill.transform.SetParent(frame.transform, false);
            _barFill = fill.AddComponent<Image>();
            _barFill.color = Palette.ErasureGlow;
            _barFill.raycastTarget = false;

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(2f, 0f);
            fillRect.sizeDelta = new Vector2(228f, -4f);

            root.SetActive(false);
        }

        static void Layout(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        static Text OverlayText(Transform parent, string name, Font font, int size, FontStyle style,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = font;
            text.text = string.Empty;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Layout(go.GetComponent<RectTransform>(), anchoredPosition, sizeDelta);
            return text;
        }

        static Font ResolveSerif()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { /* Older engines ship Arial.ttf instead. */ }

            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { /* Fall through to an OS font. */ }
            }

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Georgia", "Palatino", "Times New Roman", "Liberation Serif", "DejaVu Serif", "Arial" }, 32);
            }

            return font;
        }

        void UpdateOverlay(float dt)
        {
            if (_canvas == null) return;

            _captionLife = Mathf.Max(0f, _captionLife - dt);

            bool live = _hand != null && !_hand.IsUnmade;
            bool showing = live || _captionLife > 0f;

            if (_canvas.gameObject.activeSelf != showing) _canvas.gameObject.SetActive(showing);
            if (!showing) return;

            Vector3 anchor = _hand != null
                ? _hand.transform.position + Vector3.up * 1.75f
                : _lastAnchor;

            if (_hand != null) _lastAnchor = anchor;

            Camera camera = View;
            if (camera == null) return;

            Vector3 screen = camera.WorldToScreenPoint(anchor);
            if (screen.z <= 0f)
            {
                _canvas.gameObject.SetActive(false);
                return;
            }

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local))
            {
                return;
            }

            _marker.anchoredPosition = local;

            if (live)
            {
                _nameplate.text = "THE COUNTER-HAND  " + Hud.Roman(_hand.Generation);
                _nameplate.color = Palette.Text;

                float ratio = _hand.MaxHealth > 0f ? Mathf.Clamp01(_hand.Health / _hand.MaxHealth) : 0f;
                _barFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, 228f * ratio), -4f);
                _barFill.color = Color.Lerp(Palette.Alert, Palette.ErasureGlow, ratio);
                _barFill.gameObject.SetActive(true);
                _nameplate.gameObject.SetActive(true);
            }
            else
            {
                _barFill.gameObject.SetActive(false);
                _nameplate.gameObject.SetActive(false);
            }

            _caption.text = _captionLife > 0f ? _captionLine : string.Empty;
            _caption.color = Palette.Alpha(Palette.Alert, Mathf.Clamp01(_captionLife));
        }

        Vector3 _lastAnchor;

        Camera View
        {
            get
            {
                if (_view == null) _view = Camera.main;
                if (_view == null) _view = FindObjectOfType<Camera>();
                return _view;
            }
        }
    }
}
