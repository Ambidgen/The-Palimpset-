using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Palimpsest
{
    /// <summary>
    /// THE GLOSS - marginal annotations the page writes about itself.
    ///
    /// <para>
    /// A gloss is a reader's note in the margin of a manuscript: not the text
    /// itself, not the author's intent, but the accumulated commentary of every
    /// hand that has ever read this page. The palimpsest has been written over so
    /// many times that the vellum has developed a voice. It comments on what
    /// happens to it.
    /// </para>
    ///
    /// <para>
    /// Each gloss is a fragment of text that appears near where something
    /// significant occurred - an erasure un-written, a flame snuffed, a layer
    /// turned, the counter-hand at work. It stays for a short while, then fades.
    /// The page is not making a permanent record here; it is muttering to itself.
    /// </para>
    ///
    /// <para>
    /// Mechanically, a gloss is not inert. The page's own text impedes the
    /// undertext: an erasure that passes through a gloss takes a small, steady
    /// wound, as though the weight of the annotation were burning it. This is a
    /// passive defence - the margin is not a weapon, but it is not nothing. The
    /// page defends itself in the only way a text can: by being there.
    /// </para>
    ///
    /// <para>
    /// The system is bolted onto the finished arena the same way the counter-hand
    /// and the colophon are: a runtime director that attaches itself, reads the
    /// page through the arena's public surface, and never modifies the arena to
    /// know it exists.
    /// </para>
    /// </summary>
    public class Gloss : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Tuning
        // ------------------------------------------------------------------

        /// <summary>Radius of the annotation's field on the vellum.</summary>
        public const float FieldRadius = 2.2f;

        /// <summary>Seconds the gloss remains on the page before it fades.</summary>
        const float Lifetime = 24f;

        /// <summary>Seconds the gloss takes to appear fully.</summary>
        const float FadeInTime = 1.0f;

        /// <summary>Seconds the gloss takes to fade at the end of its life.</summary>
        const float FadeOutTime = 1.8f;

        /// <summary>
        /// Damage per second an erasure takes at the centre of the field. Falloff
        /// reduces this toward the edge, so a gloss is not a wall but a gradient.
        /// </summary>
        const float ErasureDamageRate = 4.0f;

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        /// <summary>The text the page wrote here.</summary>
        public string Text { get; private set; }

        /// <summary>Seconds since the gloss appeared.</summary>
        public float Age { get; private set; }

        /// <summary>True once the gloss has faded past its lifetime.</summary>
        public bool IsExpired { get { return Age >= Lifetime; } }

        /// <summary>0 when the gloss is fresh, 1 when it is about to expire.</summary>
        public float LifeRatio { get { return Lifetime > 0f ? Mathf.Clamp01(Age / Lifetime) : 1f; } }

        // ------------------------------------------------------------------
        // Visual
        // ------------------------------------------------------------------

        Transform _ring;
        Material _ringMaterial;
        Material _glowMaterial;
        float _seed;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        /// <summary>
        /// Places a gloss on the page at the given world position with the given text.
        /// The gloss hangs off the page transform (not the layer), so a page turn does
        /// not destroy it; it still fades naturally on its own timer.
        /// </summary>
        public static Gloss Create(Transform parent, Vector3 position, string text)
        {
            GameObject root = new GameObject("Gloss");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(position.x, 0f, position.z);

            Gloss gloss = root.AddComponent<Gloss>();
            gloss.Text = string.IsNullOrEmpty(text) ? "THE PAGE NOTES SOMETHING" : text;
            gloss._seed = Random.Range(0f, 200f);
            gloss.BuildView();
            return gloss;
        }

        void Awake()
        {
            /*
             * A note from the hand that annotates.
             * Syllables: 5 - 7 - 5.
             */
            Debug.Log("Words at the margin / slow the dark that reads the page / annotation holds");
        }

        void BuildView()
        {
            _ringMaterial = MaterialFactory.DynamicEmissive(Palette.Gloss, 0.6f);
            _glowMaterial = MaterialFactory.DynamicEmissive(Palette.Gloss, 0.25f);

            // A small ring on the vellum marking the annotation's reach.
            _ring = MeshFactory.MeshObject("Gloss Ring", MeshFactory.UnitRing, _ringMaterial, transform).transform;
            _ring.localPosition = new Vector3(0f, 0.014f, 0f);
            _ring.localScale = new Vector3(FieldRadius, 1f, FieldRadius);

            // A faint inner disc, barely visible: the ghost of the annotation itself.
            MeshFactory.MeshObject("Gloss Glow", MeshFactory.UnitDisc, _glowMaterial, transform)
                .transform.localPosition = new Vector3(0f, 0.010f, 0f);
        }

        // ------------------------------------------------------------------
        // Simulation - driven by GlossDirector
        // ------------------------------------------------------------------

        /// <summary>
        /// Advances the gloss's age and wounds any erasure that passes through its
        /// field. The damage is a gradient: the centre of the annotation burns most,
        /// the edge barely at all, so the margin reads as a thickening of the text
        /// rather than a barrier.
        /// </summary>
        public void Tick(float dt, Arena arena, Transform layerRoot)
        {
            Age += dt;
            Animate(dt);

            if (arena == null || arena.IsOver || layerRoot == null) return;

            // Damage erasures inside the field. The layer root holds every living
            // erasure as a child, so iterating its children is cheaper than a
            // scene-wide search.
            float damage = ErasureDamageRate + arena.Layer * 0.5f;

            for (int i = 0; i < layerRoot.childCount; i++)
            {
                Erasure erasure = layerRoot.GetChild(i).GetComponent<Erasure>();
                if (erasure == null || erasure.IsDead) continue;

                Vector3 delta = erasure.transform.position - transform.position;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr > FieldRadius * FieldRadius) continue;

                float falloff = 1f - Mathf.Clamp01(Mathf.Sqrt(sqr) / FieldRadius);
                erasure.TakeDamage(damage * falloff * dt);
            }
        }

        /// <summary>
        /// The alpha the overlay label should use for this gloss. Fades in over
        /// <see cref="FadeInTime"/>, holds steady, then fades out over
        /// <see cref="FadeOutTime"/>.
        /// </summary>
        public float GetAlpha()
        {
            if (Age < FadeInTime) return Mathf.Clamp01(Age / FadeInTime);
            if (Age > Lifetime - FadeOutTime) return Mathf.Clamp01((Lifetime - Age) / FadeOutTime);
            return 1f;
        }

        // ------------------------------------------------------------------
        // Presentation
        // ------------------------------------------------------------------

        void Animate(float dt)
        {
            float t = Time.time + _seed;
            float alpha = GetAlpha();
            float beat = 0.5f + 0.5f * Mathf.Sin(t * 1.4f);

            if (_ring != null)
            {
                float scale = FieldRadius * (1f + beat * 0.025f);
                _ring.localScale = new Vector3(scale, 1f, scale);
                _ring.localRotation = Quaternion.Euler(0f, -t * 10f, 0f);
            }

            if (_ringMaterial != null)
            {
                MaterialFactory.ApplyEmission(_ringMaterial, Palette.Gloss,
                    (0.30f + beat * 0.22f) * alpha);
            }

            if (_glowMaterial != null)
            {
                MaterialFactory.ApplyEmission(_glowMaterial, Palette.Gloss,
                    (0.10f + beat * 0.06f) * alpha);
            }
        }

        void OnDestroy()
        {
            if (_ringMaterial != null) Destroy(_ringMaterial);
            if (_glowMaterial != null) Destroy(_glowMaterial);
        }
    }

    /// <summary>
    /// Runs the gloss system without the arena having to know it exists.
    ///
    /// <para>
    /// The director is additive: it attaches itself at runtime, hangs its glosses
    /// off the page transform the arena already builds (so they survive layer turns
    /// and fade on their own timer), and drives everything from the arena's public
    /// surface. Erasures are found by iterating the layer root for
    /// <see cref="Erasure"/> components; candles by reading the arena's
    /// <see cref="Arena.CandleAt"/> and checking <c>IsOut</c>; the counter-hand
    /// through <see cref="CounterHandDirector"/>. <c>Arena.cs</c> was not modified
    /// to add any of this.
    /// </para>
    /// </summary>
    public class GlossDirector : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Tuning
        // ------------------------------------------------------------------

        /// <summary>Never more than this many glosses on the page at once.</summary>
        const int MaxGlosses = 8;

        /// <summary>Minimum seconds between two glosses appearing.</summary>
        const float GateLength = 2.5f;

        /// <summary>Seconds between atmospheric glosses when nothing else is happening.</summary>
        const float AtmosphericInterval = 20f;

        /// <summary>
        /// Seconds after a layer change before the director starts generating kill
        /// glosses again. The arena destroys all erasures when clearing a layer, so
        /// without this gate every death during the transition would leave an
        /// annotation the page did not earn.
        /// </summary>
        const float SettleTime = 0.8f;

        // ------------------------------------------------------------------
        // Phrase libraries
        // ------------------------------------------------------------------

        /// <summary>
        /// Written where an erasure was un-written. The page is noting its own
        /// defence: something tried to eat it, and the text held.
        /// </summary>
        static readonly string[] KillPhrases =
        {
            "HERE THE UNDERTEXT WAS SILENCED",
            "A BLOT DIED AT THIS MARGIN",
            "WHAT ROSE HERE WAS UN-WRITTEN",
            "THE INK HELD",
            "AN ERASURE RETURNS TO SILT",
            "HERE THE QUIET FOLLOWS THE STROKE",
            "A SHAPE THAT WILL NOT RETURN",
            "THE PAGE REFUSES WHAT WAS WRITTEN HERE",
            "SOMETHING DIED BENEATH THE QUILL",
            "A VOID FILLED WITH INK",
            "THE UNDERTEXT RETREATS",
            "HERE THE SCRIBE HELD THE LINE",
            "THE PAGE DEFENDS ITS OWN",
            "WHAT CAME FROM BENEATH IS SILENT NOW",
            "A STROKE THAT CLOSED A MOUTH",
        };

        /// <summary>Written where a flame went out. The page is noting its loss.</summary>
        static readonly string[] CandlePhrases =
        {
            "A WICK WENT DARK HERE",
            "THE FLAME FAILED AT THIS EDGE",
            "WAX RAN OUT BENEATH THIS MARGIN",
            "A FLAME THE PAGE REMEMBERS",
            "HERE THE LIGHT GAVE WAY",
            "THE WICK COULD NOT HOLD",
            "A FIRE THE UNDERTEXT DRANK",
            "DARKNESS AT THIS MARGIN",
            "THE FLAME DIED AS THE HANDS WATCHED",
            "WAX AND SMOKE - THE PAGE NOTES IT",
        };

        /// <summary>Written when a layer completes. The page is noting its own deepening.</summary>
        static readonly string[] LayerPhrases =
        {
            "THE PAGE TURNS",
            "THE VELLUM REMEMBERS ANOTHER HAND",
            "WHAT WAS WRITTEN IS NOW UNDERWRITING",
            "THE FOLIO DEEPENS",
            "A NEW LAYER UPON THE OLD",
            "THE PAGE BEARS ANOTHER READING",
            "WHAT IS ABOVE WAS ONCE BENEATH",
            "THE SCRIBE WRITES OVER THE SCRIBE",
        };

        /// <summary>Written where the counter-hand was at work.</summary>
        static readonly string[] HandPhrases =
        {
            "AN EARLIER HAND PASSED THIS WAY",
            "THE RIVAL SCRIBE WORKED HERE",
            "A QUILL NOT YOUR OWN MARKED THIS MARGIN",
            "THE UNDERTEXT HAS AN AUTHOR",
            "SOMEONE ELSE'S INK IS HERE",
            "THE COUNTER-HAND LEFT ITS TRACE",
            "A SECOND SCRIBE WAS HERE BEFORE YOU",
            "THE PAGE WAS EDITED",
        };

        /// <summary>
        /// Written when nothing in particular is happening. The page is muttering.
        /// The atmosphere is not commentary on an event; it is the sound of vellum
        /// remembering itself.
        /// </summary>
        static readonly string[] AtmosphericPhrases =
        {
            "THE PAGE IS DEEP",
            "THE MARGIN WATCHES",
            "UNDERNEATH, OLDER HANDS STILL MOVE",
            "EVERY SEAL LEAVES A RING",
            "THE VELLUM GROWS HEAVY",
            "WHAT IS WRITTEN HERE WILL BE WRITTEN OVER",
            "THE INK REMEMBERS",
            "A QUIET CORNER OF THE FOLIO",
            "THE UNDERTEXT STIRS",
            "THE PAGE READS ITSELF",
            "BENEATH THIS WORD, ANOTHER",
            "THE SCRIBE'S HAND SHAPES WHAT COMES NEXT",
            "THE MARGIN HOLDS WHAT THE CENTRE FORGETS",
            "AN ANNOTATION ON NOTHING IN PARTICULAR",
        };

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        public static GlossDirector Instance { get; private set; }

        /// <summary>Every gloss currently on the page.</summary>
        public List<Gloss> Glosses { get { return _glosses; } }

        /// <summary>Total glosses written this run.</summary>
        public int Written { get; private set; }

        Arena _arena;
        SfxBank _sfx;
        CameraRig _rig;

        readonly List<Gloss> _glosses = new List<Gloss>();
        Transform _pageRoot;
        Transform _layerRoot;

        // Event tracking: the director watches the arena's public surface for state
        // changes and translates them into annotations.
        Dictionary<int, Vector3> _trackedErasures = new Dictionary<int, Vector3>();
        HashSet<int> _deadCandles = new HashSet<int>();
        int _seenLayer = -1;
        float _gate;
        float _atmosphericTimer = AtmosphericInterval * 0.5f;
        bool _suppressEvents;
        bool _armed;

        // Counter-hand state tracking.
        HandState _seenHandState = HandState.Rising;
        int _seenHandGeneration;

        // Phrase selection seed, so consecutive glosses do not repeat.
        int _lastKillIndex = -1;
        int _lastCandleIndex = -1;
        int _lastAtmosphericIndex = -1;

        // Overlay.
        Canvas _canvas;
        RectTransform _canvasRect;
        readonly List<GlossLabel> _labels = new List<GlossLabel>();
        Camera _view;

        sealed class GlossLabel
        {
            public RectTransform Root;
            public Text Content;
        }

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            if (Instance != null) return;
            Build();
        }

        public static GlossDirector Build()
        {
            GameObject go = new GameObject("Gloss Director");
            return go.AddComponent<GlossDirector>();
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

            if (_arena.IsTitle)
            {
                if (_armed || _glosses.Count > 0) Reset();
                UpdateOverlay(dt);
                return;
            }

            if (!_armed) Arm();
            if (!_armed) return;

            if (_layerRoot == null) _layerRoot = LayerRoot();

            _gate = Mathf.Max(0f, _gate - dt);

            // A layer change destroys all erasures and candles; the suppress gate
            // prevents a flood of glosses from the resulting state transitions.
            if (_seenLayer >= 0 && _arena.Layer != _seenLayer)
            {
                OnLayerTurned();
            }

            bool live = !_arena.IsOver && !_arena.IsPaused;

            if (live)
            {
                TrackErasures();
                TrackCandles();
                TrackCounterHand();
                TrackAtmospheric(dt);
            }

            TickGlosses(live ? dt : 0f);
            PruneGlosses();
            UpdateOverlay(dt);
        }

        void Bind()
        {
            if (_arena != null) return;

            _arena = Arena.Instance;
            if (_arena == null) return;

            _sfx = FindObjectOfType<SfxBank>();
            _rig = FindObjectOfType<CameraRig>();
        }

        // ------------------------------------------------------------------
        // Arming and resetting
        // ------------------------------------------------------------------

        void Arm()
        {
            _pageRoot = PageRoot();
            if (_pageRoot == null) return;

            _layerRoot = LayerRoot();
            _seenLayer = _arena.Layer;
            _seenHandGeneration = 0;
            _seenHandState = HandState.Rising;
            _suppressEvents = true;
            _gate = SettleTime;
            _atmosphericTimer = AtmosphericInterval * 0.5f;
            Written = 0;
            _armed = true;

            Debug.Log("[Palimpsest] The gloss opens. The page begins to comment on itself.");
        }

        void Reset()
        {
            for (int i = 0; i < _glosses.Count; i++)
            {
                if (_glosses[i] != null) Destroy(_glosses[i].gameObject);
            }
            _glosses.Clear();

            for (int i = 0; i < _labels.Count; i++)
            {
                if (_labels[i] != null && _labels[i].Root != null) Destroy(_labels[i].Root.gameObject);
            }
            _labels.Clear();

            _trackedErasures.Clear();
            _deadCandles.Clear();
            _armed = false;
            _seenLayer = -1;
            _suppressEvents = false;
            Written = 0;
        }

        // ------------------------------------------------------------------
        // Transform resolution
        // ------------------------------------------------------------------

        /// <summary>The page transform, which a page turn never clears.</summary>
        Transform PageRoot()
        {
            if (_arena == null) return null;
            Transform page = _arena.transform.Find("The Page");
            return page != null ? page : FindDeep(_arena.transform, "The Page");
        }

        /// <summary>The layer transform, where living erasures and candles are parented.</summary>
        Transform LayerRoot()
        {
            if (_layerRoot != null) return _layerRoot;
            if (_arena == null) return null;

            Transform page = PageRoot();
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
            if (root == null) return null;
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
        // Event detection
        // ------------------------------------------------------------------

        void OnLayerTurned()
        {
            _seenLayer = _arena.Layer;
            _trackedErasures.Clear();
            _deadCandles.Clear();
            _suppressEvents = true;
            _gate = SettleTime;

            // A layer turn is itself a gloss-worthy event.
            AddGloss(RandomMarginPoint(), GenerateLayerText());
        }

        /// <summary>
        /// Watches the layer root for erasures that have been destroyed since the
        /// last frame. When one disappears, its last-known position becomes a gloss.
        /// </summary>
        void TrackErasures()
        {
            Transform layerRoot = _layerRoot != null ? _layerRoot : LayerRoot();
            if (layerRoot == null) return;

            // Collect current erasure IDs and their positions.
            HashSet<int> currentIds = new HashSet<int>();
            for (int i = 0; i < layerRoot.childCount; i++)
            {
                Erasure erasure = layerRoot.GetChild(i).GetComponent<Erasure>();
                if (erasure == null || erasure.IsDead) continue;

                int id = erasure.GetInstanceID();
                currentIds.Add(id);
                _trackedErasures[id] = erasure.transform.position;
            }

            // Any tracked ID not in the current set was destroyed this frame.
            List<int> removed = new List<int>();
            foreach (var kvp in _trackedErasures)
            {
                if (!currentIds.Contains(kvp.Key))
                {
                    removed.Add(kvp.Key);
                    if (!_suppressEvents && _gate <= 0f)
                    {
                        AddGloss(kvp.Value, GenerateKillText());
                    }
                }
            }

            for (int i = 0; i < removed.Count; i++)
                _trackedErasures.Remove(removed[i]);

            // The suppress gate decays after being set by a layer change.
            if (_suppressEvents) _suppressEvents = false;
        }

        /// <summary>
        /// Watches the arena's candles for flames that have gone out. A candle going
        /// dark is a loss the page notes at the margin.
        /// </summary>
        void TrackCandles()
        {
            if (_arena == null) return;

            for (int i = 0; i < _arena.CandleCount; i++)
            {
                Candle candle = _arena.CandleAt(i);
                if (candle == null) continue;

                int id = candle.GetInstanceID();
                if (candle.IsOut && !_deadCandles.Contains(id))
                {
                    _deadCandles.Add(id);
                    if (!_suppressEvents && _gate <= 0f)
                    {
                        AddGloss(candle.transform.position, GenerateCandleText());
                    }
                }
            }
        }

        /// <summary>
        /// Watches the counter-hand for state transitions. When it starts un-writing
        /// a seal or snuffing a wick, the page annotates the spot.
        /// </summary>
        void TrackCounterHand()
        {
            if (CounterHandDirector.Instance == null) return;
            CounterHandDirector director = CounterHandDirector.Instance;

            if (!director.IsHandAtWork)
            {
                if (director.Hand != null && director.Hand.IsUnmade)
                {
                    int gen = director.Hand.Generation;
                    if (gen != _seenHandGeneration)
                    {
                        _seenHandGeneration = gen;
                        if (_gate <= 0f)
                        {
                            AddGloss(director.Hand.transform.position, GenerateHandText());
                        }
                    }
                }
                return;
            }

            CounterHand hand = director.Hand;
            if (hand == null) return;

            int currentGen = hand.Generation;
            if (currentGen != _seenHandGeneration)
            {
                _seenHandGeneration = currentGen;
                _seenHandState = HandState.Rising;
            }

            HandState state = hand.State;
            if (state != _seenHandState)
            {
                _seenHandState = state;

                if ((state == HandState.Unwriting || state == HandState.Snuffing) && _gate <= 0f)
                {
                    AddGloss(hand.transform.position, GenerateHandText());
                }
            }
        }

        /// <summary>
        /// When nothing dramatic is happening the page still mutters. A timer
        /// generates an atmospheric gloss at a random point on the margin.
        /// </summary>
        void TrackAtmospheric(float dt)
        {
            _atmosphericTimer -= dt;
            if (_atmosphericTimer > 0f || _gate > 0f) return;

            _atmosphericTimer = AtmosphericInterval + Random.Range(-3f, 5f);
            AddGloss(RandomMarginPoint(), GenerateAtmosphericText());
        }

        // ------------------------------------------------------------------
        // Gloss management
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a gloss to the page. If the page is already at capacity, the oldest
        /// gloss is removed to make room. The new gloss is placed on the page
        /// transform so it survives layer turns.
        /// </summary>
        void AddGloss(Vector3 position, string text)
        {
            if (_pageRoot == null) _pageRoot = PageRoot();
            if (_pageRoot == null) return;

            if (_glosses.Count >= MaxGlosses)
            {
                // Remove the oldest gloss.
                Gloss oldest = _glosses[0];
                _glosses.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            // Clamp the position to the page.
            Vector2 flat = new Vector2(position.x, position.z);
            float limit = Arena.PageRadius - 0.8f;
            if (flat.magnitude > limit) flat = flat.normalized * limit;
            Vector3 clamped = new Vector3(flat.x, 0f, flat.y);

            Gloss gloss = Gloss.Create(_pageRoot, clamped, text);
            _glosses.Add(gloss);
            Written++;

            _gate = GateLength;

            AddLabel(gloss);

            if (_sfx != null) _sfx.Play(Sfx.Gloss, 0.45f, Random.Range(0.90f, 1.15f));
            if (_rig != null) _rig.Shake(0.03f);
        }

        void TickGlosses(float dt)
        {
            for (int i = 0; i < _glosses.Count; i++)
            {
                if (_glosses[i] != null) _glosses[i].Tick(dt, _arena, _layerRoot);
            }
        }

        void PruneGlosses()
        {
            for (int i = _glosses.Count - 1; i >= 0; i--)
            {
                if (_glosses[i] == null || _glosses[i].IsExpired)
                {
                    RemoveLabel(i);
                    if (_glosses[i] != null) Destroy(_glosses[i].gameObject);
                    _glosses.RemoveAt(i);
                }
            }
        }

        // ------------------------------------------------------------------
        // Phrase generation
        // ------------------------------------------------------------------

        string GenerateKillText()
        {
            return PickPhrase(KillPhrases, ref _lastKillIndex);
        }

        string GenerateCandleText()
        {
            return PickPhrase(CandlePhrases, ref _lastCandleIndex);
        }

        string GenerateLayerText()
        {
            int layer = _arena != null ? _arena.Layer : 1;

            // Deeper layers get a more specific annotation.
            if (layer >= 5 && Random.value > 0.45f)
            {
                return "LAYER " + Hud.Roman(layer) + " IS BENEATH US NOW";
            }

            return PickPhrase(LayerPhrases, ref _lastAtmosphericIndex);
        }

        string GenerateHandText()
        {
            return PickPhrase(HandPhrases, ref _lastKillIndex);
        }

        string GenerateAtmosphericText()
        {
            // At high silt pressure, the atmospheric phrases shift toward the sediment.
            if (_arena != null && _arena.SiltPressure > 0.5f && Random.value > 0.5f)
            {
                int stains = _arena.StainCount;
                int blooms = _arena.BloomCount;

                if (blooms > 0) return "THE SILT BLOOMS - " + blooms + " VENT" + (blooms == 1 ? "" : "S") + " OPEN";
                if (stains > 30) return "THE PAGE IS HEAVY WITH " + stains + " STAINS";
            }

            return PickPhrase(AtmosphericPhrases, ref _lastAtmosphericIndex);
        }

        static string PickPhrase(string[] phrases, ref int lastIndex)
        {
            if (phrases.Length == 0) return "THE PAGE NOTES SOMETHING";

            int index = Random.Range(0, phrases.Length);
            // Avoid repeating the last phrase from this library.
            if (phrases.Length > 1 && index == lastIndex) index = (index + 1) % phrases.Length;
            lastIndex = index;
            return phrases[index];
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>A random point on the margin, near the edge of the page.</summary>
        Vector3 RandomMarginPoint()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Arena.PageRadius - Random.Range(0.6f, 2.8f);
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        // ------------------------------------------------------------------
        // Overlay: screen-space labels that track each gloss's world position
        // ------------------------------------------------------------------

        void BuildOverlay()
        {
            GameObject root = new GameObject("Gloss Overlay");
            root.transform.SetParent(transform, false);

            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 280;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRect = root.GetComponent<RectTransform>();

            root.SetActive(false);
        }

        void AddLabel(Gloss gloss)
        {
            if (_canvasRect == null) return;

            GameObject labelRoot = new GameObject("Gloss Label");
            labelRoot.transform.SetParent(_canvasRect, false);

            RectTransform rect = labelRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 28f);

            Font serif = ResolveSerif();

            Text text = labelRoot.AddComponent<Text>();
            text.font = serif;
            text.text = gloss.Text;
            text.fontSize = 16;
            text.fontStyle = FontStyle.Italic;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Palette.Gloss;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            GlossLabel label = new GlossLabel();
            label.Root = rect;
            label.Content = text;
            _labels.Add(label);
        }

        void RemoveLabel(int index)
        {
            if (index < 0 || index >= _labels.Count) return;

            GlossLabel label = _labels[index];
            if (label != null && label.Root != null) Destroy(label.Root.gameObject);
            _labels.RemoveAt(index);
        }

        void UpdateOverlay(float dt)
        {
            if (_canvas == null) return;

            bool anyGloss = _glosses.Count > 0;
            bool show = anyGloss && _arena != null && !_arena.IsTitle;

            if (_canvas.gameObject.activeSelf != show) _canvas.gameObject.SetActive(show);
            if (!show) return;

            Camera camera = View;
            if (camera == null) return;

            // Keep label count in sync with gloss count.
            while (_labels.Count < _glosses.Count)
            {
                AddLabel(_glosses[_labels.Count]);
            }
            while (_labels.Count > _glosses.Count)
            {
                RemoveLabel(_labels.Count - 1);
            }

            for (int i = 0; i < _glosses.Count; i++)
            {
                Gloss gloss = _glosses[i];
                GlossLabel label = _labels[i];
                if (gloss == null || label == null || label.Root == null) continue;

                float alpha = gloss.GetAlpha();

                // Position the label above the gloss's world position.
                Vector3 anchor = gloss.transform.position + Vector3.up * 0.6f;
                Vector3 screen = camera.WorldToScreenPoint(anchor);

                if (screen.z <= 0f)
                {
                    label.Root.gameObject.SetActive(false);
                    continue;
                }

                label.Root.gameObject.SetActive(true);

                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local))
                {
                    label.Root.anchoredPosition = local;
                }

                // The text fades with the gloss, and it shifts from gold toward muted
                // as it ages, so a fresh annotation reads clearly and an old one recedes.
                float textAlpha = alpha * 0.85f;
                label.Content.color = Palette.Alpha(
                    Color.Lerp(Palette.Gloss, Palette.Muted, gloss.LifeRatio * 0.5f),
                    textAlpha);
                label.Content.text = gloss.Text;
            }
        }

        Camera View
        {
            get
            {
                if (_view == null) _view = Camera.main;
                if (_view == null) _view = FindObjectOfType<Camera>();
                return _view;
            }
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
    }
}
