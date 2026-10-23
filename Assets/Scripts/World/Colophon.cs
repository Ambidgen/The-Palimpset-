using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Palimpsest
{
    /// <summary>
    /// THE COLOPHON - the hands that are still standing in the margin.
    ///
    /// <para>
    /// A colophon is the note a scribe leaves at the end of a manuscript naming the
    /// hands that made it. This page has one, and it is not a credit screen: it is the
    /// list of agents that wrote this repository, read out of the ledger at
    /// <c>Assets/Resources/.ai_contributors.json</c> and summoned onto the vellum.
    /// Every hand that ever wrote this page is still standing in the margin, and it
    /// is still interested in what happens to it.
    /// </para>
    ///
    /// <para>
    /// A witness is not an antagonist. It does not hunt, it is not edible, and ink
    /// passes through it. What it has instead is a <b>field of regard</b>: a ring on
    /// the page where the ground behaves differently because a hand is watching it.
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>A seal written inside the field is <b>endorsed</b>: it inherits
    /// resilience, exactly as if it had been built on an old ghost-ring. Building
    /// under the eye is the cheapest strength on the page.</item>
    /// <item>A stain inside the field <b>ferments faster</b>. The hand that wrote
    /// this part of the page is still interested in the violence done on it, so
    /// killing under the eye ripens the silt roughly twice as fast.</item>
    /// <item>A hand has finite <b>patience</b>. Once it has endorsed its quota for
    /// the layer it looks away, its ring goes dark, and its field becomes ordinary
    /// vellum again - which is the counterplay. Spend a few cheap seals to blind a
    /// witness, and then do your killing on the ground it has stopped watching.</item>
    /// <item>A hand recorded as having <b>taken something away</b> never looks away.
    /// Its field is a dead zone: it eats seals instead of endorsing them, and it
    /// rots blots at between two and four times the rate they would manage alone.
    /// Do not build there. Do not kill there.</item>
    /// </list>
    ///
    /// <para>
    /// One hand is summoned per layer from the second onward, in the order the ledger
    /// was written, so descending the folio re-reads the page's own history. When the
    /// ledger runs out the page does not invent new hands: it <b>deepens</b> one that
    /// is already standing, widening its field and raising its patience. When the run
    /// ends, the hand whose field you built in most signs the folio.
    /// </para>
    /// </summary>
    public class Witness : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Tuning
        // ------------------------------------------------------------------

        /// <summary>Radius of a first-summoned hand's field of regard.</summary>
        public const float FieldRadius = 4.8f;

        /// <summary>How far out toward the margin a witness stands.</summary>
        const float StandRadius = Arena.PageRadius - 1.9f;

        /// <summary>Resilience an endorsed seal inherits.</summary>
        const float EndorseBase = 16f;
        const float EndorsePerDepth = 6f;

        /// <summary>Seals one hand will endorse per layer, plus one per depth.</summary>
        const int PatienceBase = 3;

        /// <summary>Extra charge per second a watched stain accumulates.</summary>
        const float FermentRate = 0.045f;

        /// <summary>What an erasing hand does to that rate.</summary>
        const float ErasingFerment = 2.2f;

        /// <summary>Damage per second an erasing hand does to seals in its field.</summary>
        const float ErasingBite = 7f;

        /// <summary>Damage applied the instant a seal is set inside an erasing field.</summary>
        const float ErasingSnap = 3.5f;

        /// <summary>Radians per second a wandering hand drifts around the margin.</summary>
        const float WanderSpeed = 0.055f;

        /// <summary>Seconds a hand takes to come up through the vellum.</summary>
        const float RiseTime = 1.2f;

        /// <summary>How often the page is re-read for new seals and ripening blots.</summary>
        const float ScanInterval = 0.15f;

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        public Contributor Hand { get; private set; }

        /// <summary>How many times this hand has been summoned; one on its first arrival.</summary>
        public int Depth { get; private set; }

        /// <summary>Seals this hand has endorsed, for the whole run.</summary>
        public int Testimony { get; private set; }

        /// <summary>Blooms this hand's regard forced open.</summary>
        public int Ripened { get; private set; }

        /// <summary>Endorsements it still has patience for this layer.</summary>
        public int Patience { get; private set; }

        public Color Hue { get; private set; }

        public HandDisposition Disposition
        {
            get { return Hand != null ? Hand.Disposition : HandDisposition.Endorsing; }
        }

        /// <summary>
        /// True while the hand is still watching its field. A hand that has spent its
        /// patience for the layer looks away - except an erasing hand, which never
        /// does, because wanting the page blank is not a mood that passes.
        /// </summary>
        public bool IsAttentive
        {
            get { return Disposition == HandDisposition.Erasing || Patience > 0; }
        }

        /// <summary>Radius of the field of regard, which grows each time the hand is deepened.</summary>
        public float Reach { get { return FieldRadius + (Depth - 1) * 0.55f; } }

        public string Name { get { return Hand != null ? Hand.ShortName : "A HAND"; } }

        Transform _view;
        Transform _robe;
        Transform _hood;
        Transform _face;
        Transform _field;
        Transform _eye;

        Material _robeMaterial;
        Material _faceMaterial;
        Material _fieldMaterial;
        Material _eyeMaterial;

        Mesh _fieldMesh;

        readonly List<Ward> _watched = new List<Ward>();

        float _angle;
        float _rise;
        float _flash;
        float _seed;
        float _scan;
        bool _lookedAway;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public static Witness Create(Transform parent, Contributor hand, float angle)
        {
            GameObject root = new GameObject("Witness");
            if (parent != null) root.transform.SetParent(parent, false);

            Witness witness = root.AddComponent<Witness>();
            witness.Hand = hand;
            witness.Depth = 1;
            witness.Patience = witness.PatienceForDepth();
            witness._angle = angle;
            witness._seed = Random.Range(0f, 100f);
            witness.Hue = hand != null ? hand.Hue : Palette.Witness;
            witness.transform.position = witness.StandPosition();
            witness.BuildView();
            return witness;
        }

        void BuildView()
        {
            Color robe = Color.Lerp(Palette.Night, Hue, 0.22f);

            _robeMaterial = MaterialFactory.DynamicEmissive(robe, 0.30f);
            _faceMaterial = MaterialFactory.DynamicEmissive(Hue, 1.4f);
            _fieldMaterial = MaterialFactory.DynamicEmissive(Hue, 0.55f);
            _eyeMaterial = MaterialFactory.DynamicEmissive(Hue, 1.1f);

            // Everything that comes up out of the page hangs off one transform so the
            // arrival can be animated as a single motion.
            GameObject view = new GameObject("Figure");
            view.transform.SetParent(transform, false);
            _view = view.transform;

            _robe = MeshFactory.Primitive(PrimitiveType.Cylinder, "Robe", _view,
                new Vector3(0f, 0.68f, 0f), new Vector3(0.30f, 0.68f, 0.30f), _robeMaterial).transform;

            _hood = MeshFactory.Primitive(PrimitiveType.Sphere, "Hood", _view,
                new Vector3(0f, 1.32f, 0f), new Vector3(0.27f, 0.31f, 0.27f), _robeMaterial).transform;

            // No face. Where the scribe has a mask and the counter-hand has a wound,
            // a witness has only the regard: a flat, lit oval that never blinks.
            _face = MeshFactory.Primitive(PrimitiveType.Sphere, "Regard", _view,
                new Vector3(0f, 1.30f, 0.17f), new Vector3(0.13f, 0.17f, 0.06f), _faceMaterial).transform;

            // The ring on the vellum is the readable boundary of the hand's attention.
            // It is left on the root, not on the figure, so it does not rise with it.
            _field = BuildField();

            _eye = MeshFactory.MeshObject("Eye", MeshFactory.UnitRing, _eyeMaterial, transform).transform;
            _eye.localPosition = new Vector3(0f, 0.030f, 0f);
            _eye.localScale = new Vector3(1.05f, 1f, 1.05f);
        }

        Transform BuildField()
        {
            if (_fieldMesh != null) Destroy(_fieldMesh);

            float reach = Mathf.Max(0.2f, Reach);
            _fieldMesh = MeshFactory.BuildRing(reach - 0.055f, reach, 96);

            Transform ring = MeshFactory.MeshObject("Field Of Regard", _fieldMesh, _fieldMaterial, transform).transform;
            ring.localPosition = new Vector3(0f, 0.012f, 0f);
            return ring;
        }

        // ------------------------------------------------------------------
        // Summoning and page turns
        // ------------------------------------------------------------------

        /// <summary>
        /// Summoned a second time: the hand does not duplicate, it comes up deeper.
        /// Its field widens and it has more patience, which is the page's way of
        /// re-reading a hand it has already read once.
        /// </summary>
        public void Deepen()
        {
            Depth++;
            Patience = PatienceForDepth();
            _lookedAway = false;
            _flash = 1f;

            if (_field != null) Destroy(_field.gameObject);
            _field = BuildField();
        }

        /// <summary>A hand's attention is a per-layer budget, renewed when the page turns.</summary>
        public void OnLayerTurned()
        {
            Patience = PatienceForDepth();
            _lookedAway = false;
        }

        int PatienceForDepth()
        {
            return PatienceBase + Depth;
        }

        /// <summary>True once, when the hand has just spent the last of its patience.</summary>
        public bool TakeLookedAway()
        {
            bool value = _lookedAway;
            _lookedAway = false;
            return value;
        }

        // ------------------------------------------------------------------
        // Simulation - driven by ColophonDirector only while a run is live
        // ------------------------------------------------------------------

        public void Tick(float dt, Arena arena, Transform layerRoot)
        {
            if (Hand == null) return;

            _rise = Mathf.Min(1f, _rise + dt / RiseTime);
            _flash = Mathf.MoveTowards(_flash, 0f, dt * 1.8f);

            if (Disposition == HandDisposition.Wandering) _angle += WanderSpeed * dt;
            transform.position = StandPosition();

            // The page is read on a fixed cadence rather than every frame: a hand
            // noticing a new seal is not something that needs 120Hz fidelity.
            _scan -= dt;
            if (_scan <= 0f)
            {
                float step = ScanInterval - _scan;
                _scan = ScanInterval;

                if (arena != null && !arena.IsOver && !arena.IsPaused)
                {
                    ReadSeals(layerRoot, step);
                    ReadStains(step);
                }
            }

            Face(arena, dt);
            Animate(dt);
        }

        /// <summary>
        /// Seals written under the eye are endorsed. An erasing hand bites them
        /// instead, and it bites them for as long as they stand there.
        /// </summary>
        void ReadSeals(Transform layerRoot, float step)
        {
            if (layerRoot == null) return;

            float sqrReach = Reach * Reach;

            for (int i = _watched.Count - 1; i >= 0; i--)
            {
                Ward ward = _watched[i];
                if (ward == null || ward.IsBroken)
                {
                    _watched.RemoveAt(i);
                    continue;
                }

                if (Disposition == HandDisposition.Erasing && IsInside(ward.transform.position, sqrReach))
                {
                    ward.TakeDamage(ErasingBite * step);
                }
            }

            for (int i = 0; i < layerRoot.childCount; i++)
            {
                Ward ward = layerRoot.GetChild(i).GetComponent<Ward>();
                if (ward == null || ward.IsBroken) continue;
                if (_watched.Contains(ward)) continue;
                if (!IsInside(ward.transform.position, sqrReach)) continue;

                _watched.Add(ward);

                if (Disposition == HandDisposition.Erasing)
                {
                    ward.TakeDamage(ErasingSnap);
                    continue;
                }

                if (Patience <= 0) continue;

                float power = EndorseBase + (Depth - 1) * EndorsePerDepth;
                if (Disposition == HandDisposition.Wandering) power *= 1.35f;

                ward.Embed(power);
                Testimony++;
                Patience--;
                _flash = 1f;

                TransientRing.Spawn(layerRoot, ward.transform.position, 0.22f, 1.30f, Hue, 0.42f, 2.6f);

                if (Patience <= 0) _lookedAway = true;
            }
        }

        /// <summary>
        /// Stains under the eye ferment. This is the price of the endorsement: the
        /// ground where building is cheapest is also the ground where a kill leaves
        /// the heaviest consequence - until the hand looks away.
        /// </summary>
        void ReadStains(float step)
        {
            Sediment sediment = Sediment.Instance;
            if (sediment == null || sediment.All == null) return;
            if (!IsAttentive) return;

            float rate = FermentRate * (Disposition == HandDisposition.Erasing ? ErasingFerment : 1f);
            float sqrReach = Reach * Reach;

            List<Stratum> all = sediment.All;
            for (int i = 0; i < all.Count; i++)
            {
                Stratum stratum = all[i];
                if (stratum == null || stratum.Kind != StratumKind.Stain || stratum.IsSpent) continue;
                if (!IsInside(stratum.transform.position, sqrReach)) continue;

                if (stratum.Ferment(rate * step))
                {
                    Ripened++;
                    TransientRing.Spawn(transform.parent, stratum.transform.position,
                        0.25f, 1.9f, Hue, 0.55f, 2.6f);
                }
            }
        }

        bool IsInside(Vector3 point, float sqrReach)
        {
            Vector3 delta = point - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= sqrReach;
        }

        Vector3 StandPosition()
        {
            float radius = StandRadius;

            // A wandering hand will not hold a station; it drifts in and out as it goes.
            if (Disposition == HandDisposition.Wandering)
            {
                radius -= 0.85f * (0.5f + 0.5f * Mathf.Sin(Time.time * 0.27f + _seed));
            }

            return new Vector3(Mathf.Cos(_angle) * radius, 0f, Mathf.Sin(_angle) * radius);
        }

        void Face(Arena arena, float dt)
        {
            Vector3 target = Vector3.zero;

            if (arena != null && _rise >= 1f && IsAttentive)
            {
                Vector3 scribe = arena.ScribePosition;
                Vector3 delta = scribe - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= Reach * Reach) target = scribe;
            }

            Vector3 look = target - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude < 0.0001f) return;

            Quaternion facing = Quaternion.LookRotation(look.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, facing, 1f - Mathf.Exp(-3.2f * dt));
        }

        // ------------------------------------------------------------------
        // Presentation
        // ------------------------------------------------------------------

        void Animate(float dt)
        {
            float t = Time.time + _seed;
            float rise = Mathf.SmoothStep(0f, 1f, _rise);

            // A hand that has looked away is still standing there, but it is no
            // longer lit, and it breathes more slowly than one that is watching.
            float attention = IsAttentive ? 1f : 0.26f;
            float beat = 0.5f + 0.5f * Mathf.Sin(t * (IsAttentive ? 1.6f : 0.5f));

            if (_view != null)
            {
                _view.localPosition = new Vector3(0f, Mathf.Lerp(-1.3f, 0f, rise), 0f);
                float grow = Mathf.Lerp(0.45f, 1f, rise);
                _view.localScale = new Vector3(grow, grow, grow);
            }

            if (_robe != null)
            {
                _robe.localPosition = new Vector3(0f, 0.68f + Mathf.Sin(t * 1.25f) * 0.016f, 0f);
            }

            if (_hood != null)
            {
                _hood.localPosition = new Vector3(0f, 1.32f + Mathf.Sin(t * 1.10f) * 0.012f, 0f);
            }

            if (_face != null)
            {
                // The regard widens very slightly as it breathes, so the figure never
                // reads as a statue even when it is standing perfectly still.
                _face.localScale = new Vector3(0.13f + beat * 0.014f, 0.17f, 0.06f);
            }

            if (_faceMaterial != null)
            {
                MaterialFactory.ApplyEmission(_faceMaterial, Hue,
                    (0.85f + beat * 0.75f) * attention + _flash * 2.2f);
            }

            if (_robeMaterial != null)
            {
                Color robe = Color.Lerp(Color.Lerp(Palette.Night, Hue, 0.22f), Color.white, _flash * 0.45f);
                MaterialFactory.ApplyColor(_robeMaterial, robe);
                MaterialFactory.ApplyEmission(_robeMaterial, Hue,
                    (0.16f + beat * 0.14f) * attention + _flash * 1.4f);
            }

            if (_fieldMaterial != null)
            {
                MaterialFactory.ApplyEmission(_fieldMaterial, Hue,
                    (0.30f + beat * 0.24f) * attention + _flash * 1.1f);
            }

            if (_eye != null)
            {
                float scale = 1.05f + beat * 0.10f;
                _eye.localScale = new Vector3(scale, 1f, scale);
                _eye.localRotation = Quaternion.Euler(0f, -t * 18f, 0f);
                if (_eyeMaterial != null)
                {
                    MaterialFactory.ApplyEmission(_eyeMaterial, Hue,
                        (0.55f + beat * 0.35f) * attention + _flash * 1.6f);
                }
            }
        }

        void OnDestroy()
        {
            if (_robeMaterial != null) Destroy(_robeMaterial);
            if (_faceMaterial != null) Destroy(_faceMaterial);
            if (_fieldMaterial != null) Destroy(_fieldMaterial);
            if (_eyeMaterial != null) Destroy(_eyeMaterial);
            if (_fieldMesh != null) Destroy(_fieldMesh);
        }
    }

    /// <summary>
    /// Summons and runs the colophon without the arena having to know it exists.
    ///
    /// Like the counter-hand's director, this is additive: it attaches itself at
    /// runtime, hangs its witnesses off the page transform the arena already builds
    /// (never off the layer transform, because the point of a witness is that a page
    /// turn does not remove it), and drives everything from the arena's public
    /// surface. Seals are found by reading the layer transform for
    /// <see cref="Ward"/> components and stains by asking <see cref="Sediment"/>, so
    /// <c>Arena.cs</c> was not modified to add any of this.
    /// </summary>
    public class ColophonDirector : MonoBehaviour
    {
        /// <summary>The colophon opens on the second layer: the page must have been written once.</summary>
        public const int FirstLayer = 2;

        /// <summary>Never more than this many hands standing at once.</summary>
        const int MaxWitnesses = 8;

        /// <summary>Golden angle, so two consecutive hands never stand on the same spot.</summary>
        const float GoldenAngle = 2.39996323f;

        public static ColophonDirector Instance { get; private set; }

        public List<Witness> Witnesses { get { return _witnesses; } }

        /// <summary>Seals endorsed across every hand so far this run.</summary>
        public int EndorsedTotal
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _witnesses.Count; i++)
                {
                    if (_witnesses[i] != null) total += _witnesses[i].Testimony;
                }
                return total;
            }
        }

        /// <summary>The line the colophon writes on the page when the run ends.</summary>
        public string Verdict { get; private set; }

        Arena _arena;
        List<Contributor> _ledger;
        readonly List<Witness> _witnesses = new List<Witness>();
        readonly List<Plate> _plates = new List<Plate>();

        Transform _root;
        Transform _layerRoot;
        SfxBank _sfx;
        CameraRig _rig;

        int _seenLayer;
        int _summoned;
        bool _armed;
        bool _stale;
        bool _verdictTaken;

        Canvas _canvas;
        RectTransform _canvasRect;
        Text _toast;
        Text _readout;
        Text _verdictLead;
        Text _verdictName;
        Text _verdictTail;
        string _verdictLine = string.Empty;
        string _toastLine = string.Empty;
        float _toastLife;
        Camera _view;

        /// <summary>One nameplate, hanging over one witness.</summary>
        sealed class Plate
        {
            public RectTransform Marker;
            public Text Name;
            public Text Epithet;
            public Text Tally;
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

        public static ColophonDirector Build()
        {
            GameObject go = new GameObject("Colophon Director");
            return go.AddComponent<ColophonDirector>();
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
                if (_armed || _witnesses.Count > 0) Reset();
                UpdateOverlay(dt);
                return;
            }

            if (_arena.IsOver)
            {
                _stale = true;
            }
            else if (_stale)
            {
                // A new folio is a new page of witnesses, laid over the old hands.
                _stale = false;
                Reset();
            }

            if (!_armed) Arm();
            if (!_armed) return;

            // The layer transform is stable for the life of the arena, but a resolve
            // that happened before the page finished building would have missed it.
            if (_layerRoot == null) _layerRoot = LayerRoot();

            if (_arena.Layer != _seenLayer)
            {
                _seenLayer = _arena.Layer;

                for (int i = 0; i < _witnesses.Count; i++)
                {
                    if (_witnesses[i] != null) _witnesses[i].OnLayerTurned();
                }

                if (_arena.Layer >= FirstLayer) Summon();
            }

            bool live = !_arena.IsOver && !_arena.IsPaused;

            for (int i = 0; i < _witnesses.Count; i++)
            {
                Witness witness = _witnesses[i];
                if (witness == null) continue;

                witness.Tick(live ? dt : 0f, _arena, _layerRoot);

                if (live && witness.TakeLookedAway())
                {
                    Announce(witness.Name + " LOOKS AWAY");
                }
            }

            UpdateVerdict();
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
            _ledger = ContributorLedger.Load();

            Transform page = PageRoot();
            if (page == null) return;

            GameObject root = new GameObject("Colophon");
            root.transform.SetParent(page, false);
            _root = root.transform;
            _layerRoot = LayerRoot();

            _summoned = 0;
            _seenLayer = _arena.Layer;
            _verdictTaken = false;
            _verdictLine = string.Empty;
            Verdict = string.Empty;
            _armed = true;

            Debug.Log("[Palimpsest] The colophon opens. " + _ledger.Count + " hand(s) are on record.");
        }

        void Reset()
        {
            for (int i = 0; i < _witnesses.Count; i++)
            {
                if (_witnesses[i] != null) Destroy(_witnesses[i].gameObject);
            }
            _witnesses.Clear();

            for (int i = 0; i < _plates.Count; i++)
            {
                if (_plates[i] != null && _plates[i].Marker != null) Destroy(_plates[i].Marker.gameObject);
            }
            _plates.Clear();

            if (_root != null) Destroy(_root.gameObject);
            _root = null;

            _armed = false;
            _summoned = 0;
            _verdictTaken = false;
            _verdictLine = string.Empty;
            Verdict = string.Empty;
            _toastLife = 0f;
            _toastLine = string.Empty;
        }

        /// <summary>The page transform, which a page turn never clears.</summary>
        Transform PageRoot()
        {
            if (_arena == null) return null;

            Transform page = _arena.transform.Find("The Page");
            return page != null ? page : FindDeep(_arena.transform, "The Page");
        }

        /// <summary>The layer transform, where the living seals are parented.</summary>
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
        // Summoning
        // ------------------------------------------------------------------

        void Summon()
        {
            if (_root == null || _ledger == null || _ledger.Count == 0) return;

            bool fresh = false;
            int touched = -1;

            if (_witnesses.Count < Mathf.Min(MaxWitnesses, _ledger.Count))
            {
                Contributor hand = _ledger[_summoned % _ledger.Count];
                Witness witness = Witness.Create(_root, hand, 0.6f + _summoned * GoldenAngle);
                _witnesses.Add(witness);
                AddPlate(witness);

                touched = _witnesses.Count - 1;
                fresh = true;
            }
            else if (_witnesses.Count > 0)
            {
                // The ledger is exhausted. The page does not invent a new author; it
                // reads one it has already read, deeper.
                touched = _summoned % _witnesses.Count;
                if (_witnesses[touched] != null) _witnesses[touched].Deepen();
            }

            _summoned++;
            if (touched < 0 || touched >= _witnesses.Count) return;

            Witness summoned = _witnesses[touched];
            if (summoned == null) return;

            RefreshPlate(touched);

            TransientRing.Spawn(_root, summoned.transform.position, 0.4f, summoned.Reach * 1.9f,
                summoned.Hue, 1.1f, 3.0f);

            if (_sfx != null) _sfx.Play(Sfx.Layer, 0.5f, fresh ? 1.05f : 0.82f);
            if (_rig != null) _rig.Shake(0.10f);

            if (fresh)
            {
                Announce("A HAND COMES TO THE MARGIN");
                Announce(summoned.Name, true);
            }
            else
            {
                Announce(summoned.Name + " IS SUMMONED DEEPER");
            }
        }

        void Announce(string line, bool append = false)
        {
            if (append && !string.IsNullOrEmpty(_toastLine) && _toastLife > 0f)
            {
                _toastLine = _toastLine + "\n" + line;
            }
            else
            {
                _toastLine = line;
            }

            _toastLife = 3.6f;
        }

        // ------------------------------------------------------------------
        // The verdict
        // ------------------------------------------------------------------

        /// <summary>
        /// When the page is lost or held, the colophon is written: the hand whose
        /// field you did most of your building in signs the folio. It is not a score.
        /// It is an attribution, and it is drawn from the repository's own record.
        /// </summary>
        void UpdateVerdict()
        {
            if (!_arena.IsOver)
            {
                _verdictTaken = false;
                return;
            }

            if (_verdictTaken) return;
            _verdictTaken = true;

            Witness best = null;
            for (int i = 0; i < _witnesses.Count; i++)
            {
                Witness witness = _witnesses[i];
                if (witness == null) continue;
                if (best == null || witness.Testimony > best.Testimony) best = witness;
            }

            if (best == null || best.Testimony <= 0)
            {
                _verdictLine = "NO HAND SAW ENOUGH OF YOUR WORK TO SIGN IT";
                Verdict = _verdictLine;
                return;
            }

            string lead = _arena.IsVictory
                ? "THE FOLIO IS SIGNED IN THE MANNER OF"
                : "YOU WROTE IN THE MANNER OF";

            string tally = best.Testimony + (best.Testimony == 1 ? " SEAL ENDORSED" : " SEALS ENDORSED")
                + "     " + best.Ripened + (best.Ripened == 1 ? " BLOOM" : " BLOOMS")
                + " RIPENED UNDER ITS EYE";

            _verdictLine = lead + "\n" + best.Name + "\n" + tally;
            Verdict = _verdictLine;
        }

        // ------------------------------------------------------------------
        // Overlay
        // ------------------------------------------------------------------

        void BuildOverlay()
        {
            GameObject root = new GameObject("Colophon Overlay");
            root.transform.SetParent(transform, false);

            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 320;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRect = root.GetComponent<RectTransform>();

            Font serif = ResolveSerif();

            // Announcements, under the arena's own toast line.
            _toast = MakeText(_canvasRect, "Colophon Toast", serif, 26, FontStyle.Normal,
                TextAnchor.UpperCenter, Palette.Witness,
                new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(1500f, 64f));

            // A standing readout, under the arena's own counters.
            _readout = MakeText(_canvasRect, "Colophon Readout", serif, 19, FontStyle.Normal,
                TextAnchor.UpperRight, Palette.Muted,
                new Vector2(1f, 1f), new Vector2(-38f, -188f), new Vector2(620f, 28f));

            // The colophon itself, written below the arena's ending card.
            _verdictLead = MakeText(_canvasRect, "Colophon Lead", serif, 20, FontStyle.Italic,
                TextAnchor.MiddleCenter, Palette.Muted,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -248f), new Vector2(1400f, 30f));

            _verdictName = MakeText(_canvasRect, "Colophon Name", serif, 28, FontStyle.Bold,
                TextAnchor.MiddleCenter, Palette.Witness,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -282f), new Vector2(1600f, 40f));

            _verdictTail = MakeText(_canvasRect, "Colophon Tally", serif, 18, FontStyle.Normal,
                TextAnchor.MiddleCenter, Palette.Muted,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -314f), new Vector2(1200f, 28f));

            root.SetActive(false);
        }

        static Text MakeText(Transform parent, string name, Font font, int size, FontStyle style,
            TextAnchor anchor, Color color, Vector2 anchorPoint, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = font;
            text.text = string.Empty;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorPoint;
            rect.anchorMax = anchorPoint;
            rect.pivot = anchorPoint;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            return text;
        }

        void AddPlate(Witness witness)
        {
            if (_canvasRect == null) return;

            GameObject markerObject = new GameObject("Plate");
            markerObject.transform.SetParent(_canvasRect, false);

            RectTransform marker = markerObject.AddComponent<RectTransform>();

            // The canvas pivots on its centre and ScreenPointToLocalPointInRectangle
            // returns a point relative to that pivot, so the marker must be anchored
            // to the centre or every plate would land half a screen out.
            marker.anchorMin = new Vector2(0.5f, 0.5f);
            marker.anchorMax = new Vector2(0.5f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.sizeDelta = Vector2.zero;

            Font serif = ResolveSerif();

            Plate plate = new Plate();
            plate.Marker = marker;
            plate.Name = MakeText(marker, "Name", serif, 19, FontStyle.Bold, TextAnchor.MiddleCenter,
                witness.Hue, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(1200f, 26f));
            plate.Epithet = MakeText(marker, "Epithet", serif, 14, FontStyle.Italic, TextAnchor.MiddleCenter,
                Palette.Muted, new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(1200f, 22f));
            plate.Tally = MakeText(marker, "Tally", serif, 14, FontStyle.Normal, TextAnchor.MiddleCenter,
                Palette.Muted, new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(700f, 22f));

            _plates.Add(plate);
        }

        void RefreshPlate(int index)
        {
            if (index < 0 || index >= _plates.Count || index >= _witnesses.Count) return;

            Plate plate = _plates[index];
            Witness witness = _witnesses[index];
            if (plate == null || witness == null) return;

            plate.Name.text = witness.Name + (witness.Depth > 1 ? "  " + Hud.Roman(witness.Depth) : string.Empty);
            plate.Epithet.text = witness.Hand != null ? witness.Hand.Epithet : string.Empty;
            plate.Tally.text = TallyFor(witness);
        }

        static string TallyFor(Witness witness)
        {
            // An erasing hand has no patience to spend and no endorsements to count;
            // it is simply never a safe place to write.
            if (witness.Disposition == HandDisposition.Erasing) return "EATS WHAT IS WRITTEN HERE";

            if (!witness.IsAttentive) return "LOOKS AWAY - THE GROUND IS ORDINARY";
            return "ENDORSED " + witness.Testimony + "     PATIENCE " + witness.Patience;
        }

        void UpdateOverlay(float dt)
        {
            if (_canvas == null || _arena == null) return;

            _toastLife = Mathf.Max(0f, _toastLife - dt);

            bool over = _arena.IsOver;
            bool anyWitness = _witnesses.Count > 0;
            bool show = !_arena.IsTitle && (anyWitness || _toastLife > 0f || over);

            if (_canvas.gameObject.activeSelf != show) _canvas.gameObject.SetActive(show);
            if (!show) return;

            _toast.text = _toastLine;
            _toast.color = Palette.Alpha(Palette.Witness, Mathf.Clamp01(_toastLife / 1.2f));

            if (_readout != null)
            {
                bool reading = anyWitness && !_arena.IsTitle;
                _readout.gameObject.SetActive(reading);
                if (reading)
                {
                    int ripened = 0;
                    for (int i = 0; i < _witnesses.Count; i++)
                    {
                        if (_witnesses[i] != null) ripened += _witnesses[i].Ripened;
                    }

                    _readout.text = "WITNESSES " + _witnesses.Count
                        + "      ENDORSED " + EndorsedTotal
                        + "      RIPENED " + ripened;
                }
            }

            string lead = string.Empty;
            string name = string.Empty;
            string tally = string.Empty;

            if (over && !string.IsNullOrEmpty(_verdictLine))
            {
                string[] lines = _verdictLine.Split('\n');
                if (lines.Length > 0) lead = lines[0];
                if (lines.Length > 1) name = lines[1];
                if (lines.Length > 2) tally = lines[2];
            }

            _verdictLead.text = lead;
            _verdictName.text = name;
            _verdictTail.text = tally;

            Camera camera = View;
            bool platesVisible = anyWitness && !over && camera != null;

            for (int i = 0; i < _plates.Count; i++)
            {
                Plate plate = _plates[i];
                if (plate == null || plate.Marker == null) continue;

                bool visible = platesVisible && i < _witnesses.Count && _witnesses[i] != null;
                if (plate.Marker.gameObject.activeSelf != visible) plate.Marker.gameObject.SetActive(visible);
                if (!visible) continue;

                Witness witness = _witnesses[i];
                plate.Tally.text = TallyFor(witness);
                plate.Name.color = witness.IsAttentive
                    ? witness.Hue
                    : Palette.Alpha(witness.Hue, 0.45f);

                Vector3 screen = camera.WorldToScreenPoint(witness.transform.position + Vector3.up * 1.72f);
                if (screen.z <= 0f)
                {
                    plate.Marker.gameObject.SetActive(false);
                    continue;
                }

                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local))
                {
                    plate.Marker.anchoredPosition = local;
                }
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
