using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Palimpsest
{
    /// <summary>
    /// THE WATERMARK - the hand that made the sheet.
    ///
    /// <para>
    /// The sediment answered <i>what the page remembers</i>. The counter-hand answered
    /// <i>who wrote it</i>. The colophon answered <i>who is still watching</i>. The gloss
    /// answered <i>what the page says about itself</i>. The lector answered <i>who reads it,
    /// and how</i>. Every one of those is a hand that wrote, or watched, or read. The
    /// watermark answers the question underneath all of them: <b>who made the sheet?</b>
    /// </para>
    ///
    /// <para>
    /// A watermark is the maker's mark pressed into wet paper by the wire of the mould, and
    /// it is the one thing on a leaf that is older than every word on it. It is not an
    /// author. The hand that laid the wire wrote nothing, signed no colophon, and is
    /// therefore the only hand on the page that appears in no ledger at all. It is not even
    /// a presence: it is ground - the ribs of the sheet, and the figure tied into them.
    /// Every folio starts as a fresh overwriting of the same paper, so the wire is the one
    /// mark on the palimpsest that is the same in every reading of it.
    /// </para>
    ///
    /// <para>
    /// Mechanically the wire is a place rather than a person, and it is the first system on
    /// this page that is about <i>where</i> rather than <i>who</i>. The chain lines are the
    /// ribs of the mould:
    /// </para>
    ///
    /// <para>
    /// A seal written on the wire is <b>keyed</b>: the wire holds ink better than loose
    /// vellum, so the seal inherits resilience. The wire can only take so much of that per
    /// layer, and the margin says how much is left. A stain left on the wire <b>conducts</b>:
    /// the wire carries the undertext the way it once carried water, so a blot lying on a rib
    /// ferments faster than one on plain vellum. The cheapest ground on the page to build on
    /// is therefore also the ground where your own killing ripens soonest. When a keyed seal
    /// is broken the wire <b>tears</b>, leaving residue of its own where the sheet was pulled
    /// tightest, and a wick standing on the wire burns a little slower because the wire cools
    /// the wax pooling against it.
    /// </para>
    ///
    /// <para>
    /// The <b>maker's mark</b> is a single figure tied into the wire at a fixed place on every
    /// page - the one point on the leaf that is the same in every folio. Stains in its field
    /// are steadily soured, because the sheet's own memory resists being written over there,
    /// and one seal per layer is <b>countersigned</b> by it: the strongest ground the page
    /// offers, and the only seal that leaves a ghost rather than a stain when it falls.
    /// </para>
    ///
    /// <para>
    /// And the sheet can be <b>held to the light</b>. A watermark is invisible in reflected
    /// light and unmistakable in transmitted light. When the scribe illuminates the page the
    /// whole sheet flares, the ribs come up through the vellum, and for a few seconds anything
    /// of the undertext crossing the wire is cut by it. Light is the one verb the undertext
    /// cannot answer, and the wire is the part of the page that is made of light's way through
    /// it.
    /// </para>
    ///
    /// <para>
    /// The system is bolted onto the finished arena the way every other layer was. A runtime
    /// director attaches itself, hangs the wire off the <i>page</i> transform (the sheet
    /// outlives every page turn), reads seals and erasures out of the layer transform, stains
    /// through <see cref="Sediment"/>, candles through the arena's public surface, and detects
    /// the illuminate verb by watching the arena's cooldown readout, exactly as the
    /// counter-hand does. <c>Arena.cs</c> was not modified, so deleting <c>Watermark.cs</c>
    /// removes the sheet's own ground without removing the game.
    /// </para>
    /// </summary>
    public class Watermark : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Tuning
        // ------------------------------------------------------------------

        /// <summary>How many chain lines the mould laid across the sheet.</summary>
        public const int ChainCount = 7;

        /// <summary>Distance between two chain lines.</summary>
        public const float ChainSpacing = 2.6f;

        /// <summary>Half the visible thickness of one chain line.</summary>
        public const float ChainHalfWidth = 0.075f;

        /// <summary>Distance between two laid lines: the fine grain of the sheet.</summary>
        public const float LaidSpacing = 0.30f;

        /// <summary>The wire stops short of the deckle, so the margin stays bare.</summary>
        public const float SheetRadius = 11.25f;

        /// <summary>Radius of the maker's mark's field on the vellum.</summary>
        public const float MarkRadius = 1.15f;

        /// <summary>
        /// The one fixed point on the page: where the wire was tied. Every folio is the same
        /// sheet, so this is the only ground that does not move between runs.
        /// </summary>
        public static readonly Vector3 MarkPosition = new Vector3(-5.2f, 0f, 5.2f);

        /// <summary>How close a seal has to be to a rib to be keyed by it.</summary>
        public const float WireReach = 0.50f;

        /// <summary>Seconds the sheet stays lit after an illumination.</summary>
        public const float RevealLength = 6.5f;

        /// <summary>Seconds over which the transmitted light fades at the end.</summary>
        const float RevealFade = 1.5f;

        /// <summary>Health a keyed seal inherits from the wire.</summary>
        const float KeyPower = 10f;

        /// <summary>Health the maker's mark adds to the one seal it countersigns per layer.</summary>
        const float CountersignPower = 20f;

        /// <summary>Extra charge per second a stain picks up while lying on a rib.</summary>
        const float WireFermentRate = 0.014f;

        /// <summary>Strength per second the mark's own field takes out of a stain.</summary>
        const float MarkSourRate = 0.020f;

        /// <summary>Wax per second returned to a wick standing on the wire.</summary>
        const float SteadyWaxRate = 0.006f;

        /// <summary>Damage per second the lit wire does to the undertext crossing it.</summary>
        const float BurnBase = 3.4f;
        const float BurnPerLayer = 0.45f;

        /// <summary>Scan cadences: seals are read often, the sediment and the mark slowly.</summary>
        const float ScanInterval = 0.14f;
        const float SedimentInterval = 0.25f;
        const float MarkInterval = 0.80f;

        /// <summary>Residue a torn wire leaves where the keyed seal stood.</summary>
        const float TearStrength = 0.26f;

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        public static Watermark Instance { get; private set; }

        /// <summary>Seals the wire has taken on this layer.</summary>
        public int KeyedCount { get; private set; }

        /// <summary>How many seals the wire will take this layer.</summary>
        public int Budget { get; private set; }

        /// <summary>Seals keyed across the whole folio, for the attribution.</summary>
        public int KeyedTotal { get; private set; }

        /// <summary>True once the maker's mark has countersigned its seal this layer.</summary>
        public bool IsCountersigned { get; private set; }

        /// <summary>True while the sheet is lit from beneath.</summary>
        public bool IsRevealed { get { return _revealTimer > 0f; } }

        /// <summary>Seconds of transmitted light left.</summary>
        public float RevealRemaining { get { return _revealTimer; } }

        /// <summary>0 (barely visible) to 1 (held to the light).</summary>
        public float Reveal { get { return _reveal; } }

        sealed class KeyedSeal
        {
            public Ward Ward;
            public Vector3 Position;
            public bool Countersigned;
            public float Born;
        }

        readonly Dictionary<int, KeyedSeal> _keyed = new Dictionary<int, KeyedSeal>();

        Transform _markRing;
        Light _lamp;

        Material _laidMaterial;
        Material _chainMaterial;
        Material _markMaterial;
        Material _ringMaterial;

        Mesh _laidMesh;
        Mesh _chainMesh;
        Mesh _markMesh;
        Mesh _ringMesh;

        int _layer = 1;
        float _scan;
        float _sediment;
        float _markTimer;
        float _reveal;
        float _revealTimer;
        float _lastCooldown;
        float _lastScrape;
        float _graceUntil;
        float _seed;

        bool _tearFlag;
        bool _keyFlag;
        bool _countersignFlag;
        bool _revealFlag;
        bool _rememberFlag;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        /// <summary>
        /// Lays the wire onto the page. It hangs off the page transform (not the layer),
        /// because a page turn does not remove the fact that the sheet was made.
        /// </summary>
        public static Watermark Create(Transform parent)
        {
            if (Instance != null) return Instance;

            GameObject root = new GameObject("Watermark");
            if (parent != null) root.transform.SetParent(parent, false);
            return root.AddComponent<Watermark>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _seed = Random.Range(0f, 40f);
            Budget = Mathf.Min(8, 4 + _layer / 2);

            /*
             * A note from the hand that made the sheet.
             * Syllables: 5 - 7 - 5.
             */
            Debug.Log("The hand made the sheet / it never signed what it held / I write on its wire");

            BuildView();
        }

        void BuildView()
        {
            _laidMaterial = MaterialFactory.DynamicEmissive(Palette.Wire, 0.05f);
            _chainMaterial = MaterialFactory.DynamicEmissive(Palette.Wire, 0.30f);
            _markMaterial = MaterialFactory.DynamicEmissive(Palette.Wire, 0.40f);
            _ringMaterial = MaterialFactory.DynamicEmissive(Palette.Wire, 0.25f);

            // The wire lies just above the vellum and below everything written on it.
            _laidMesh = BuildLaidMesh();
            MeshFactory.MeshObject("Laid Lines", _laidMesh, _laidMaterial, transform)
                .transform.localPosition = new Vector3(0f, 0.0095f, 0f);

            _chainMesh = BuildChainMesh();
            MeshFactory.MeshObject("Chain Lines", _chainMesh, _chainMaterial, transform)
                .transform.localPosition = new Vector3(0f, 0.0115f, 0f);

            // The maker's mark: a small tower tied into the wire, with its field around it.
            _markMesh = BuildMarkMesh();
            MeshFactory.MeshObject("Maker's Mark", _markMesh, _markMaterial, transform)
                .transform.localPosition = new Vector3(0f, 0.0128f, 0f);

            _ringMesh = MeshFactory.BuildRing(0.955f, 1f, 72);
            _markRing = MeshFactory.MeshObject("Mark Field", _ringMesh, _ringMaterial, transform).transform;
            _markRing.localPosition = new Vector3(MarkPosition.x, 0.0126f, MarkPosition.z);
            _markRing.localScale = new Vector3(MarkRadius, 1f, MarkRadius);

            // Transmitted light: what the sheet looks like when held up to the lamp.
            GameObject lampObject = new GameObject("Transmitted Light");
            lampObject.transform.SetParent(transform, false);
            lampObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            _lamp = lampObject.AddComponent<Light>();
            _lamp.type = LightType.Point;
            _lamp.color = Palette.Wire;
            _lamp.range = 24f;
            _lamp.intensity = 0f;
            _lamp.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------
        // Mesh construction
        //
        // The wire is a handful of flat rectangles, so it is built here rather
        // than in MeshFactory: the sheet's own geometry is nobody else's
        // business, and keeping it in this file is what lets the file be
        // deleted without a trace.
        // ------------------------------------------------------------------

        /// <summary>Fine parallel lines down the leaf: the grain of the mould.</summary>
        static Mesh BuildLaidMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            int steps = Mathf.FloorToInt(SheetRadius / LaidSpacing);
            for (int i = -steps; i <= steps; i++)
            {
                float x = i * LaidSpacing;
                float halfLength = Mathf.Sqrt(Mathf.Max(0f, SheetRadius * SheetRadius - x * x)) - 0.06f;
                if (halfLength < 0.35f) continue;

                AddRect(vertices, triangles, x, 0f, 0.010f, halfLength);
            }

            return Finish(vertices, triangles, "WatermarkLaid");
        }

        /// <summary>The seven heavier ribs laid across the grain.</summary>
        static Mesh BuildChainMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int i = 0; i < ChainCount; i++)
            {
                float z = ChainZ(i);
                float halfLength = Mathf.Sqrt(Mathf.Max(0f, SheetRadius * SheetRadius - z * z)) - 0.05f;
                if (halfLength < 0.40f) continue;

                AddRect(vertices, triangles, 0f, z, halfLength, ChainHalfWidth);
            }

            return Finish(vertices, triangles, "WatermarkChain");
        }

        /// <summary>The maker's mark: a tower tied into the wire, built from flat bars.</summary>
        static Mesh BuildMarkMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            float cx = MarkPosition.x;
            float cz = MarkPosition.z;

            AddRect(vertices, triangles, cx, cz - 0.36f, 0.40f, 0.055f);        // base
            AddRect(vertices, triangles, cx, cz - 0.02f, 0.085f, 0.34f);        // stem
            AddRect(vertices, triangles, cx, cz + 0.22f, 0.30f, 0.048f);        // crossbar
            AddRect(vertices, triangles, cx - 0.235f, cz + 0.40f, 0.055f, 0.105f); // merlon left
            AddRect(vertices, triangles, cx + 0.235f, cz + 0.40f, 0.055f, 0.105f); // merlon right
            AddRect(vertices, triangles, cx, cz + 0.47f, 0.072f, 0.072f);       // crown
            AddRect(vertices, triangles, cx - 0.44f, cz - 0.30f, 0.048f, 0.085f); // tick left
            AddRect(vertices, triangles, cx + 0.44f, cz - 0.30f, 0.048f, 0.085f); // tick right

            return Finish(vertices, triangles, "WatermarkMark");
        }

        /// <summary>Adds one upward-facing rectangle to a mesh under construction.</summary>
        static void AddRect(List<Vector3> vertices, List<int> triangles,
            float centreX, float centreZ, float halfX, float halfZ)
        {
            int i = vertices.Count;

            vertices.Add(new Vector3(centreX - halfX, 0f, centreZ - halfZ));
            vertices.Add(new Vector3(centreX + halfX, 0f, centreZ - halfZ));
            vertices.Add(new Vector3(centreX + halfX, 0f, centreZ + halfZ));
            vertices.Add(new Vector3(centreX - halfX, 0f, centreZ + halfZ));

            triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 1);
            triangles.Add(i); triangles.Add(i + 3); triangles.Add(i + 2);
        }

        static Mesh Finish(List<Vector3> vertices, List<int> triangles, string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------
        // Geometry queries
        // ------------------------------------------------------------------

        /// <summary>The z of one rib. The chain lines run across the leaf.</summary>
        public static float ChainZ(int index)
        {
            return (index - (ChainCount - 1) * 0.5f) * ChainSpacing;
        }

        /// <summary>True when a point on the vellum lies on the wire.</summary>
        public static bool OnWire(Vector3 point)
        {
            for (int i = 0; i < ChainCount; i++)
            {
                if (Mathf.Abs(point.z - ChainZ(i)) <= WireReach) return true;
            }
            return false;
        }

        /// <summary>True when a point lies in the maker's mark's field.</summary>
        public static bool InMark(Vector3 point)
        {
            float dx = point.x - MarkPosition.x;
            float dz = point.z - MarkPosition.z;
            return dx * dx + dz * dz <= MarkRadius * MarkRadius;
        }

        // ------------------------------------------------------------------
        // Simulation - driven by WatermarkDirector only while a run is live
        // ------------------------------------------------------------------

        public void Tick(float dt, Arena arena, Transform layerRoot)
        {
            if (arena == null) return;

            bool live = !arena.IsOver && !arena.IsPaused && !arena.IsTitle;

            _revealTimer = Mathf.Max(0f, _revealTimer - dt);
            _reveal = Mathf.MoveTowards(_reveal, TargetReveal(), dt * 4f);

            if (live) DetectLight(arena);

            if (arena.Layer != _layer) OnLayerTurned(arena.Layer);

            if (live)
            {
                _scan -= dt;
                if (_scan <= 0f)
                {
                    _scan = ScanInterval;
                    ScanWards(arena, layerRoot);
                }

                _sediment -= dt;
                if (_sediment <= 0f)
                {
                    _sediment = SedimentInterval;
                    ReadSediment();
                }

                _markTimer = Mathf.Max(0f, _markTimer - dt);

                SteadyWax(dt, arena);
                CutTheUndertext(dt, arena, layerRoot);
            }

            Animate();
        }

        /// <summary>
        /// The player's illumination is detected the way the counter-hand detects it: a
        /// cooldown only ever counts down, so any rise is the exact frame the verb fired.
        /// </summary>
        void DetectLight(Arena arena)
        {
            float cooldown = arena.IlluminateCooldownRemaining;

            if (cooldown > _lastCooldown + 0.25f && cooldown >= 1f) BeginReveal();

            _lastCooldown = cooldown;
        }

        void BeginReveal()
        {
            _revealTimer = RevealLength;
            _revealFlag = true;
        }

        float TargetReveal()
        {
            if (_revealTimer <= 0f) return 0f;
            if (_revealTimer >= RevealFade) return 1f;
            return _revealTimer / RevealFade;
        }

        void OnLayerTurned(int layer)
        {
            _layer = layer;
            _keyed.Clear();

            IsCountersigned = false;
            KeyedCount = 0;
            Budget = Mathf.Min(8, 4 + layer / 2);

            // The arena has just cleared every seal on the page. Nothing that vanishes
            // in this window was torn by the undertext, so nothing may leave residue, and
            // the scan waits for the old seals to finish being destroyed before it reads
            // the new layer's vellum.
            _graceUntil = Time.time + 1f;
            _scan = 0.30f;
        }

        // ------------------------------------------------------------------
        // Keying and tearing
        // ------------------------------------------------------------------

        void ScanWards(Arena arena, Transform layerRoot)
        {
            // Scraping is the player's own verb and destroys their own seals; the wire
            // does not tear when the scribe chooses to take the page back. The seals the
            // scrape took are reaped in the same breaths that follow it, so a short grace
            // is all that is needed to tell the player's own erasure from the undertext's.
            float scrape = arena.ScrapeCooldownRemaining;
            if (scrape > _lastScrape + 0.25f && scrape >= 1f) _graceUntil = Time.time + 0.6f;
            _lastScrape = scrape;

            ReapTornSeals(layerRoot);

            if (layerRoot == null) return;

            for (int i = 0; i < layerRoot.childCount; i++)
            {
                Transform child = layerRoot.GetChild(i);
                if (child == null) continue;

                Ward ward = child.GetComponent<Ward>();
                if (ward == null || ward.IsBroken) continue;

                int id = ward.GetInstanceID();
                if (_keyed.ContainsKey(id)) continue;

                Vector3 position = ward.transform.position;

                // The maker's mark is older than the wire's budget: it always countersigns
                // the first seal written inside it, even when the wire itself is full.
                bool countersign = InMark(position) && !IsCountersigned;

                if (!countersign && !OnWire(position)) continue;
                if (!countersign && KeyedCount >= Budget) continue;

                ward.Embed(countersign ? CountersignPower : KeyPower);
                _keyed[id] = new KeyedSeal
                {
                    Ward = ward,
                    Position = position,
                    Countersigned = countersign,
                    Born = Time.time
                };

                if (!countersign) KeyedCount++;
                KeyedTotal++;

                if (countersign)
                {
                    IsCountersigned = true;
                    _countersignFlag = true;

                    TransientRing.Spawn(layerRoot, position, 0.2f, 1.7f, Palette.Wire, 0.55f, 2.8f);
                }
                else
                {
                    _keyFlag = true;

                    TransientRing.Spawn(layerRoot, position, 0.2f, 1.05f, Palette.Wire, 0.40f, 2.2f);
                }
            }
        }

        /// <summary>
        /// A keyed seal that is gone was broken by the undertext or by the rival hand. The
        /// wire it was tied to gives way: a keyed seal leaves residue, and a countersigned
        /// one leaves the ring the mark still remembers.
        /// </summary>
        void ReapTornSeals(Transform layerRoot)
        {
            if (_keyed.Count == 0) return;

            List<int> gone = null;

            foreach (KeyValuePair<int, KeyedSeal> entry in _keyed)
            {
                if (entry.Value == null) continue;
                if (entry.Value.Ward != null) continue;

                if (gone == null) gone = new List<int>();
                gone.Add(entry.Key);
            }

            if (gone == null) return;

            for (int i = 0; i < gone.Count; i++)
            {
                KeyedSeal seal = _keyed[gone[i]];
                _keyed.Remove(gone[i]);

                if (seal == null) continue;
                if (!seal.Countersigned) KeyedCount = Mathf.Max(0, KeyedCount - 1);
                if (Time.time < _graceUntil) continue;
                if (Time.time - seal.Born < 0.4f) continue;

                Sediment sediment = Sediment.Instance;

                if (seal.Countersigned)
                {
                    // The mark is older than the seal, so it keeps the ring of it.
                    if (sediment != null) sediment.Ghost(seal.Position, 0.5f);
                    _rememberFlag = true;
                }
                else
                {
                    if (sediment != null) sediment.Stain(seal.Position, TearStrength);
                    _tearFlag = true;
                }

                Transform parent = layerRoot != null ? layerRoot : transform;
                TransientRing.Spawn(parent, seal.Position, 0.18f, 1.30f, Palette.Wire, 0.36f, 1.8f);
            }
        }

        // ------------------------------------------------------------------
        // The page behaves differently on the wire
        // ------------------------------------------------------------------

        /// <summary>
        /// The wire conducts. A blot lying on a rib gathers charge faster than one on plain
        /// vellum, which is the cost of having built your seals on the cheapest ground.
        /// </summary>
        void ReadSediment()
        {
            Sediment sediment = Sediment.Instance;
            if (sediment == null) return;

            List<Stratum> all = sediment.All;
            if (all == null) return;

            for (int i = 0; i < all.Count; i++)
            {
                Stratum stratum = all[i];
                if (stratum == null || stratum.Kind != StratumKind.Stain || stratum.IsSpent) continue;

                Vector3 position = stratum.transform.position;
                if (InMark(position)) continue;
                if (!OnWire(position)) continue;

                stratum.Ferment(WireFermentRate * SedimentInterval);
            }

            // The maker's mark is the one place the sheet refuses the overwriting: blots in
            // its field are soured slowly, whatever the light is doing.
            if (_markTimer <= 0f)
            {
                _markTimer = MarkInterval;

                for (int i = 0; i < all.Count; i++)
                {
                    Stratum stratum = all[i];
                    if (stratum == null || stratum.Kind != StratumKind.Stain) continue;
                    if (!InMark(stratum.transform.position)) continue;

                    stratum.Sour(MarkSourRate * MarkInterval);
                }
            }
        }

        /// <summary>Wax pools against the wire, and a wick standing in it burns slower.</summary>
        void SteadyWax(float dt, Arena arena)
        {
            for (int i = 0; i < arena.CandleCount; i++)
            {
                Candle candle = arena.CandleAt(i);
                if (candle == null || candle.IsOut) continue;
                if (!OnWire(candle.transform.position)) continue;

                candle.Kindle(SteadyWaxRate * dt);
            }
        }

        /// <summary>
        /// Held to the light, the ribs of the sheet cut what crosses them. The undertext is
        /// the only thing on the page that the light reaches at all, and the wire is where
        /// the light gets into the paper.
        /// </summary>
        void CutTheUndertext(float dt, Arena arena, Transform layerRoot)
        {
            if (_reveal <= 0.05f || layerRoot == null) return;

            float amount = (BurnBase + arena.Layer * BurnPerLayer) * _reveal * dt;

            for (int i = 0; i < layerRoot.childCount; i++)
            {
                Transform child = layerRoot.GetChild(i);
                if (child == null) continue;

                Erasure erasure = child.GetComponent<Erasure>();
                if (erasure == null || erasure.IsDead) continue;
                if (!OnWire(erasure.transform.position)) continue;

                // The arena notices the death on its own next tick; this only wounds.
                erasure.TakeDamage(amount);
            }
        }

        // ------------------------------------------------------------------
        // Presentation
        // ------------------------------------------------------------------

        void Animate()
        {
            float t = Time.time + _seed;
            float breath = 0.5f + 0.5f * Mathf.Sin(t * 1.15f);
            float reveal = _reveal;

            if (_laidMaterial != null)
            {
                MaterialFactory.ApplyEmission(_laidMaterial, Palette.Wire,
                    0.045f + reveal * 1.10f + breath * 0.010f);
            }

            if (_chainMaterial != null)
            {
                MaterialFactory.ApplyEmission(_chainMaterial, Palette.Wire,
                    0.30f + reveal * 2.30f + breath * 0.06f);
            }

            if (_markMaterial != null)
            {
                MaterialFactory.ApplyEmission(_markMaterial, Palette.Wire,
                    0.38f + reveal * 2.90f + breath * 0.10f);
            }

            if (_ringMaterial != null)
            {
                MaterialFactory.ApplyEmission(_ringMaterial, Palette.Wire,
                    0.20f + reveal * 2.10f + breath * 0.12f);
            }

            if (_lamp != null)
            {
                _lamp.enabled = reveal > 0.01f;
                _lamp.intensity = reveal * 0.60f;
            }

            if (_markRing != null)
            {
                float girth = MarkRadius * (1f + 0.015f * Mathf.Sin(t * 2.1f) + reveal * 0.020f);
                _markRing.localScale = new Vector3(girth, 1f, girth);
                _markRing.localRotation = Quaternion.Euler(0f, t * (8f + reveal * 30f), 0f);
            }
        }

        // ------------------------------------------------------------------
        // One-shot events, read by the director
        // ------------------------------------------------------------------

        /// <summary>True once, on the frame a keyed seal was torn off the wire.</summary>
        public bool TakeTorn()
        {
            bool value = _tearFlag;
            _tearFlag = false;
            return value;
        }

        /// <summary>True once, on the frame the wire took a new seal.</summary>
        public bool TakeKeyed()
        {
            bool value = _keyFlag;
            _keyFlag = false;
            return value;
        }

        /// <summary>True once, on the frame the maker's mark countersigned a seal.</summary>
        public bool TakeCountersigned()
        {
            bool value = _countersignFlag;
            _countersignFlag = false;
            return value;
        }

        /// <summary>True once, on the frame the sheet was held to the light.</summary>
        public bool TakeRevealed()
        {
            bool value = _revealFlag;
            _revealFlag = false;
            return value;
        }

        /// <summary>True once, when the mark kept the ring of a fallen countersigned seal.</summary>
        public bool TakeRemembered()
        {
            bool value = _rememberFlag;
            _rememberFlag = false;
            return value;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_laidMaterial != null) Destroy(_laidMaterial);
            if (_chainMaterial != null) Destroy(_chainMaterial);
            if (_markMaterial != null) Destroy(_markMaterial);
            if (_ringMaterial != null) Destroy(_ringMaterial);

            if (_laidMesh != null) Destroy(_laidMesh);
            if (_chainMesh != null) Destroy(_chainMesh);
            if (_markMesh != null) Destroy(_markMesh);
            if (_ringMesh != null) Destroy(_ringMesh);
        }
    }

    /// <summary>
    /// Runs the watermark without the arena having to know it exists.
    ///
    /// <para>
    /// The director is additive in the same way every other layer is. It attaches itself at
    /// runtime, hangs the wire off the page transform (the sheet outlives every page turn),
    /// reads seals and erasures out of the layer transform, stains through
    /// <see cref="Sediment"/>, and candles through the arena's public surface. It never
    /// touches <c>Arena.cs</c>, and it adds no verb: everything the wire does is a
    /// consequence of where the scribe has written, where the undertext has died, and whether
    /// the page is being held up to the light.
    /// </para>
    ///
    /// <para>
    /// It is the only system on this page that does not come out of the ledger of hands. The
    /// colophon summons the authors and the lector reads them back; the watermark predates
    /// both, because the sheet had to exist before anything could be written on it. That is
    /// the point of the layer: a palimpsest is not only a record of who wrote, but of the one
    /// hand that made the ground everyone else wrote on and never named.
    /// </para>
    /// </summary>
    public class WatermarkDirector : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Tuning
        // ------------------------------------------------------------------

        /// <summary>The sheet is there from the first layer: it predates the first hand.</summary>
        public const int FirstLayer = 1;

        /// <summary>Seconds after a page turn before the wire's small dramas are announced.</summary>
        const float SettleTime = 1.6f;

        /// <summary>How long an announcement stays on the screen.</summary>
        const float ToastLife = 3.6f;

        /// <summary>Minimum seconds between two of the wire's quiet announcements.</summary>
        const float ToastGate = 5f;

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        public static WatermarkDirector Instance { get; private set; }

        /// <summary>The sheet currently on the page, if any.</summary>
        public Watermark Sheet { get; private set; }

        /// <summary>The last line the page said about its own making.</summary>
        public string LastAnnouncement { get; private set; }

        Arena _arena;
        SfxBank _sfx;
        CameraRig _rig;

        Transform _pageRoot;
        Transform _layerRoot;

        int _seenLayer;
        float _gate;
        bool _armed;
        bool _stale;

        Canvas _canvas;
        RectTransform _canvasRect;
        RectTransform _plate;
        Text _readout;
        Text _plateName;
        Text _plateWords;
        Text _toast;
        string _toastLine = string.Empty;
        float _toastLife;
        Camera _view;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            if (Instance != null) return;
            Build();
        }

        public static WatermarkDirector Build()
        {
            GameObject go = new GameObject("Watermark Director");
            return go.AddComponent<WatermarkDirector>();
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

            _gate = Mathf.Max(0f, _gate - dt);

            if (_arena.IsTitle)
            {
                if (_armed || Sheet != null) Dismiss();
                UpdateOverlay(dt);
                return;
            }

            if (_arena.IsOver)
            {
                _stale = true;
            }
            else if (_stale)
            {
                // A new folio is a new hand on the same sheet: the wire is unchanged, but
                // what it has taken has to be counted again from nothing. The new sheet is
                // laid on the next frame, once the old one has finished being destroyed.
                _stale = false;
                Dismiss();
                UpdateOverlay(dt);
                return;
            }

            if (!_armed) Arm();
            if (!_armed) return;

            if (_layerRoot == null) _layerRoot = LayerRoot();

            if (_arena.Layer != _seenLayer)
            {
                _seenLayer = _arena.Layer;
                _gate = SettleTime;
            }

            bool live = !_arena.IsOver && !_arena.IsPaused && !_arena.IsTitle;

            if (Sheet != null)
            {
                Sheet.Tick(live ? dt : 0f, _arena, _layerRoot);
                if (live) HandleEvents();
            }

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
        // Arming and dissolving
        // ------------------------------------------------------------------

        void Arm()
        {
            _pageRoot = PageRoot();
            if (_pageRoot == null) return;

            _layerRoot = LayerRoot();
            _seenLayer = _arena.Layer;
            _gate = SettleTime;
            _toastLine = string.Empty;
            _toastLife = 0f;
            LastAnnouncement = string.Empty;

            Sheet = Watermark.Create(_pageRoot);
            _armed = true;

            Debug.Log("[Palimpsest] The watermark is bound, from layer " + Hud.Roman(FirstLayer)
                + ". The sheet is older than the first hand: " + Watermark.ChainCount
                + " chain lines, and one mark that never signed the ledger.");
        }

        void Dismiss()
        {
            if (Sheet != null) Destroy(Sheet.gameObject);
            Sheet = null;

            if (_plate != null) _plate.gameObject.SetActive(false);

            _armed = false;
            _seenLayer = 0;
            _toastLine = string.Empty;
            _toastLife = 0f;
            LastAnnouncement = string.Empty;
        }

        Transform PageRoot()
        {
            if (_arena == null) return null;

            Transform page = _arena.transform.Find("The Page");
            return page != null ? page : FindDeep(_arena.transform, "The Page");
        }

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
        // The wire's small dramas
        // ------------------------------------------------------------------

        void HandleEvents()
        {
            Watermark sheet = Sheet;
            if (sheet == null) return;

            if (sheet.TakeRevealed() && _gate <= 0f)
            {
                Announce("THE SHEET IS HELD TO THE LIGHT");
                if (_sfx != null) _sfx.Play(Sfx.Gloss, 0.55f, 1.35f);
                if (_rig != null) _rig.Shake(0.03f);
            }

            if (sheet.TakeCountersigned())
            {
                Announce("THE MAKER'S MARK COUNTERSIGNS A SEAL");
                if (_sfx != null) _sfx.Play(Sfx.Scour, 0.60f, 1.30f);
                if (_rig != null) _rig.Shake(0.04f);
            }

            if (sheet.TakeKeyed() && _gate <= 0f)
            {
                Announce("THE WIRE TAKES THE SEAL");
                if (_sfx != null) _sfx.Play(Sfx.Ward, 0.45f, 1.20f);
                _gate = ToastGate;
            }

            if (sheet.TakeTorn())
            {
                Announce("THE WIRE TEARS WHERE IT WAS KEYED");
                if (_sfx != null) _sfx.Play(Sfx.Denied, 0.55f, 0.70f);
                if (_rig != null) _rig.Shake(0.05f);
                _gate = ToastGate;
            }

            if (sheet.TakeRemembered())
            {
                Announce("THE MARK KEEPS WHAT STOOD THERE");
                if (_sfx != null) _sfx.Play(Sfx.Seal, 0.45f, 1.30f);
            }
        }

        void Announce(string line)
        {
            _toastLine = line != null ? line : string.Empty;
            _toastLife = ToastLife;
            LastAnnouncement = _toastLine;
        }

        // ------------------------------------------------------------------
        // Overlay
        // ------------------------------------------------------------------

        void BuildOverlay()
        {
            GameObject root = new GameObject("Watermark Overlay");
            root.transform.SetParent(transform, false);

            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 260;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRect = root.GetComponent<RectTransform>();
            Font serif = ResolveSerif();

            // A quiet line under the arena's own silt readout: the page says how much of
            // its wire is still free without ever making a fuss about it.
            _readout = MakeText(_canvasRect, "Wire", serif, 19, FontStyle.Normal,
                TextAnchor.UpperLeft, Palette.Watermark,
                new Vector2(0f, 1f), new Vector2(38f, -232f), new Vector2(820f, 28f));

            // Announcements, beneath the arena's, the colophon's and the lector's.
            _toast = MakeText(_canvasRect, "Watermark Toast", serif, 22, FontStyle.Normal,
                TextAnchor.UpperCenter, Palette.Wire,
                new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(1500f, 76f));

            // The mark's nameplate, tracked over its field while the sheet is lit.
            GameObject plateObject = new GameObject("Mark Plate");
            plateObject.transform.SetParent(_canvasRect, false);

            _plate = plateObject.AddComponent<RectTransform>();
            _plate.anchorMin = new Vector2(0.5f, 0.5f);
            _plate.anchorMax = new Vector2(0.5f, 0.5f);
            _plate.pivot = new Vector2(0.5f, 0.5f);
            _plate.sizeDelta = Vector2.zero;

            _plateName = MakeText(_plate, "Name", serif, 19, FontStyle.Bold,
                TextAnchor.MiddleCenter, Palette.Wire,
                new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(1200f, 26f));

            _plateWords = MakeText(_plate, "Words", serif, 15, FontStyle.Italic,
                TextAnchor.MiddleCenter, Palette.Muted,
                new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1200f, 22f));

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

        void UpdateOverlay(float dt)
        {
            if (_canvas == null || _arena == null) return;

            _toastLife = Mathf.Max(0f, _toastLife - dt);

            Watermark sheet = Sheet;
            bool show = !_arena.IsTitle && (sheet != null || _toastLife > 0f);
            if (_canvas.gameObject.activeSelf != show) _canvas.gameObject.SetActive(show);
            if (!show) return;

            _toast.text = _toastLine;
            _toast.color = Palette.Alpha(Palette.Wire, Mathf.Clamp01(_toastLife / 1.2f));

            if (sheet == null)
            {
                if (_plate != null && _plate.gameObject.activeSelf) _plate.gameObject.SetActive(false);
                _readout.text = string.Empty;
                return;
            }

            if (_arena.IsOver)
            {
                _readout.text = (_arena.IsVictory
                        ? "THE SHEET IS OLDER THAN THE FOLIO   -   "
                        : "THE SHEET OUTLASTS THE HAND   -   ")
                    + sheet.KeyedTotal
                    + (sheet.KeyedTotal == 1 ? " SEAL KEYED TO THE WIRE" : " SEALS KEYED TO THE WIRE");
                _readout.color = Palette.Alpha(Palette.Watermark, 0.9f);
            }
            else if (sheet.IsRevealed)
            {
                _readout.text = "HELD TO THE LIGHT   -   " + sheet.KeyedCount + " / " + sheet.Budget
                    + " SEALS KEYED   -   THE WIRE IS DRAWN IN LIGHT";
                _readout.color = Palette.Alpha(Palette.Wire, 0.95f);
            }
            else
            {
                _readout.text = "THE WIRE   " + sheet.KeyedCount + " / " + sheet.Budget + " SEALS KEYED"
                    + (sheet.IsCountersigned ? "   -   THE MARK IS COUNTERSIGNED" : string.Empty);
                _readout.color = Palette.Alpha(Palette.Watermark, 0.85f);
            }

            // The mark only names itself when the light gets through it.
            Camera camera = View;
            bool marked = sheet.IsRevealed && !_arena.IsOver && camera != null;

            Vector2 local = Vector2.zero;
            if (marked)
            {
                Vector3 screen = camera.WorldToScreenPoint(Watermark.MarkPosition + Vector3.up * 2.0f);
                if (screen.z <= 0f)
                {
                    marked = false;
                }
                else
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _canvasRect, screen, null, out local);
                }
            }

            if (_plate.gameObject.activeSelf != marked) _plate.gameObject.SetActive(marked);
            if (!marked) return;

            _plate.anchoredPosition = local;
            _plateName.text = "THE MAKER'S MARK";
            _plateWords.text = sheet.IsCountersigned
                ? "the sheet is keeping the seal written here"
                : "older than the first hand, and in no ledger";
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
