using System.Collections.Generic;
using UnityEngine;

namespace Palimpsest
{
    /// <summary>How the quill currently reads the cursor; used only for the aiming tracer.</summary>
    public enum InputFrame
    {
        Ready,
        Cooldown,
        Inscribing
    }

    /// <summary>
    /// THE PALIMPSEST - one page, written over and over.
    ///
    /// The arena owns the whole game: the page, the scribe, the candles, the
    /// erasures, the layer progression and the scoring. Everything is composed at
    /// runtime from primitives, so there is no scene asset in this repository.
    ///
    /// Loop: seal the page (place wards) while keeping enough flames alive. Both
    /// conditions must hold at the same moment, which forces the player to spend
    /// ink on defence and on maintenance at the same time. Finishing a layer turns
    /// the page and adds another layer of undertext beneath it.
    /// </summary>
    public class Arena : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Tuning
        // ------------------------------------------------------------------

        public const float PageRadius = 12.5f;
        public const int FinalLayer = 8;
        public const float InkDamage = 30f;
        public const float InkDamageRadius = 0.95f;

        /// <summary>
        /// How far a stroke of the quill carries. The page is wider than the reach,
        /// so the scribe has to walk to the candles instead of editing the whole
        /// folio from the centre.
        /// </summary>
        public const float Reach = 8.5f;

        static readonly string[] FolioNames =
        {
            "THE FIRST HAND",
            "THE UNDERWRITING",
            "THE SCRAPED MARGIN",
            "THE CORRECTION",
            "THE FORGED COLOPHON",
            "THE DEEP TEXT",
            "THE ILLEGIBLE",
            "THE PALIMPSEST"
        };

        // ------------------------------------------------------------------
        // Public state
        // ------------------------------------------------------------------

        public static Arena Instance { get; private set; }

        public int Layer { get; private set; }
        public int Folio { get; private set; }
        public int Score { get; private set; }
        public float Integrity { get; private set; }
        public int WardsThisLayer { get; private set; }
        public int WardsRequired { get; private set; }
        /// <summary>
        /// The salient flame quota: the number of candles that must be visibly alight
        /// before the page will accept another layer. "Salient" is the precise word here
        /// because these are not merely surviving flames - they are the ones that stand
        /// out from the undertext, anchor the scribe's attention, and define the shape
        /// of the safe ground on an otherwise hostile page.
        /// </summary>
        int _salientFlameQuota;

        public int FlamesRequired { get { return _salientFlameQuota; } }
        public bool IsTitle { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsOver { get; private set; }
        public bool IsVictory { get; private set; }
        public InputFrame PointerState { get; private set; }

        public string FolioTitle
        {
            get { return FolioNames[Mathf.Clamp(Layer - 1, 0, FolioNames.Length - 1)]; }
        }

        public Vector3 ScribePosition
        {
            get { return _scribe != null ? _scribe.transform.position : Vector3.zero; }
        }

        public float ScribeRadius
        {
            get { return _scribe != null ? _scribe.Radius : 0.46f; }
        }

        public Vector3 AimPoint { get; private set; }

        /// <summary>How fast candlelight eats an erasure (units of health per second).</summary>
        public float ErasureBurnRate { get { return 26f; } }

        public int CandleCount { get { return _candles.Count; } }

        public int LivingFlames
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _candles.Count; i++)
                {
                    if (_candles[i] != null && !_candles[i].IsOut) count++;
                }
                return count;
            }
        }

        /// <summary>Seconds left in the siege; zero when the page is not turning.</summary>
        public float SiegeRemaining { get { return _advanceTimer > 0f ? _advanceTimer : 0f; } }
        public bool IsSiege { get { return _advanceTimer > 0f; } }

        public float IlluminateCooldownRemaining { get { return _illuminateCooldown; } }
        public float ScrapeCooldownRemaining { get { return _scrapeCooldown; } }
        public const float IlluminateCooldownLength = 11f;
        public const float ScrapeCooldownLength = 8f;

        public string OutcomeLine
        {
            get
            {
                if (IsVictory) return "Eight hands deep, the text holds." +
                    "\nSeals: " + _totalWards + "     Score: " + Score;

                return "The undertext rose through layer " + Hud.Roman(Layer) + "." +
                    "\nSeals: " + _totalWards + "     Score: " + Score;
            }
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        readonly List<Candle> _candles = new List<Candle>();
        readonly List<Ward> _wards = new List<Ward>();
        readonly List<Erasure> _erasures = new List<Erasure>();

        Transform _pageRoot;
        Transform _layerRoot;
        Transform _actorRoot;

        PalimpsestPlayer _scribe;
        CameraRig _rig;
        SfxBank _sfx;
        Hud _hud;

        Mesh _scrawl;
        Material _underglow;
        Material _heartMaterial;

        float _spawnTimer;
        float _erasureSpawnInterval;
        int _erasureHealthBase;
        float _erasureSpeed;
        int _erasureTarget;
        float _advanceTimer = -1f;
        float _hurtCooldown;
        float _reachWarnCooldown;
        float _illuminateCooldown;
        float _scrapeCooldown;
        float _runTime;
        int _totalWards;
        int _layerSeed;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            /*
             * A small note from the machine that assembled this page.
             * Syllables: 5 - 7 - 5.
             */
            Debug.Log("Layers of old ink fade / one page holds every answer / I too am rewritten");

            Application.targetFrameRate = 120;

            Layer = 1;
            Folio = 1;
            Integrity = 100f;
            IsTitle = true;
            IsOver = false;
            IsVictory = false;

            BuildLighting();
            BuildPage();
            BuildActors();
            BuildInterface();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.115f, 0.105f, 0.150f);
            RenderSettings.fog = false;

            // The default Unity scene ships its own directional light; adopt it rather
            // than stacking a second sun on the page.
            Light[] lights = FindObjectsOfType<Light>();
            Light key = null;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    key = lights[i];
                    break;
                }
            }

            if (key == null)
            {
                GameObject keyObject = new GameObject("Key Light");
                keyObject.transform.SetParent(transform, false);
                keyObject.transform.rotation = Quaternion.Euler(58f, -28f, 0f);

                key = keyObject.AddComponent<Light>();
                key.type = LightType.Directional;
            }

            key.color = new Color(0.760f, 0.790f, 0.900f);
            key.intensity = 0.72f;
            key.shadows = LightShadows.None;

            GameObject fillObject = new GameObject("Page Fill");
            fillObject.transform.SetParent(transform, false);
            fillObject.transform.position = new Vector3(0f, 9f, 0f);

            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = Palette.Vellum;
            fill.range = 26f;
            fill.intensity = 0.85f;
            fill.shadows = LightShadows.None;
        }

        void BuildPage()
        {
            GameObject pageGo = new GameObject("The Page");
            pageGo.transform.SetParent(transform, false);
            _pageRoot = pageGo.transform;

            // Vellum.
            MeshFactory.MeshObject("Folio", MeshFactory.FloorDisc(PageRadius),
                MaterialFactory.Lit(Palette.Vellum, 0.06f), _pageRoot);

            // Slightly brighter margin so the playfield has a visible edge.
            MeshFactory.MeshObject("Margin", MeshFactory.MarginBand(PageRadius - 0.42f, PageRadius),
                MaterialFactory.Lit(Palette.Alpha(Palette.Vellum, 1f), 0.10f), _pageRoot)
                .transform.localPosition = new Vector3(0f, 0.004f, 0f);

            // The scribe's ruling: concentric guides and a heart ring at the centre.
            for (int i = 1; i <= 3; i++)
            {
                float r = i * 3.4f;
                MeshFactory.MeshObject("Rule " + i, MeshFactory.MarginBand(r, r + 0.055f),
                    MaterialFactory.Lit(Palette.Rule, 0.05f), _pageRoot)
                    .transform.localPosition = new Vector3(0f, 0.005f, 0f);
            }

            _heartMaterial = MaterialFactory.DynamicLit(Palette.Rule, 0.15f);
            MeshFactory.MeshObject("Heart Ring", MeshFactory.HeartBand(2.42f, 2.52f), _heartMaterial, _pageRoot)
                .transform.localPosition = new Vector3(0f, 0.007f, 0f);

            // Ghost writing from earlier hands.
            _scrawl = MeshFactory.BuildScrawl(PageRadius - 1.1f, 8817, 15, 20);
            MeshFactory.MeshObject("Undertext Scrawl", _scrawl,
                MaterialFactory.Lit(Palette.Undertext, 0.02f), _pageRoot)
                .transform.localPosition = new Vector3(0f, 0.008f, 0f);

            // A violet bleed under the page, as though the vellum were lit from beneath.
            _underglow = MaterialFactory.DynamicEmissive(Palette.ErasureGlow, 0.55f);
            GameObject glow = MeshFactory.MeshObject("Underglow", MeshFactory.MarginBand(PageRadius, PageRadius + 2.2f),
                _underglow, _pageRoot);
            glow.transform.localPosition = new Vector3(0f, -0.012f, 0f);

            GameObject actorGo = new GameObject("Actors");
            actorGo.transform.SetParent(_pageRoot, false);
            _actorRoot = actorGo.transform;

            GameObject layerGo = new GameObject("Layer");
            layerGo.transform.SetParent(_pageRoot, false);
            _layerRoot = layerGo.transform;
        }

        void BuildActors()
        {
            _rig = CameraRig.Build(transform);
            _sfx = SfxBank.Build(transform);

            _scribe = PalimpsestPlayer.Create(_actorRoot, Vector3.zero);
            _rig.Follow(_scribe.transform);
        }

        void BuildInterface()
        {
            _hud = Hud.Build(transform, this);
        }

        // ------------------------------------------------------------------
        // Main loop
        // ------------------------------------------------------------------

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt > 0.08f) dt = 0.08f;

            if (_rig != null) _rig.Tick(dt);
            _runTime += dt;

            _illuminateCooldown = Mathf.Max(0f, _illuminateCooldown - dt);
            _scrapeCooldown = Mathf.Max(0f, _scrapeCooldown - dt);
            _hurtCooldown = Mathf.Max(0f, _hurtCooldown - dt);
            _reachWarnCooldown = Mathf.Max(0f, _reachWarnCooldown - dt);

            if (InputStreamGate(dt)) return;

            PointerState = InputRouter.InscribeHeld ? InputFrame.Inscribing
                : (_scribe != null && _scribe.CanInscribe ? InputFrame.Ready : InputFrame.Cooldown);

            UpdateAim();

            if (_scribe != null) _scribe.Tick(dt, this);

            if (InputRouter.IlluminatePressed) TryIlluminate();
            if (InputRouter.ScrapePressed) TryScrape();
            if (InputRouter.InscribeHeld && _scribe != null && _scribe.CanInscribe) TryInscribe();

            TickCandles(dt);
            TickErasures(dt);
            PruneLists();
            UpdateSpawner(dt);
            UpdateAdvance(dt);
            UpdateAtmosphere(dt);
        }

        /// <summary>
        /// Handles title / pause / game over input. Returns true when the simulation
        /// should stay frozen this frame.
        /// </summary>
        bool InputStreamGate(float dt)
        {
            if (IsTitle)
            {
                if (InputRouter.ConfirmPressed)
                {
                    IsTitle = false;
                    BeginRun();
                }
                return true;
            }

            if (IsOver)
            {
                if (InputRouter.RestartPressed)
                {
                    RestartRun();
                }
                return true;
            }

            if (InputRouter.PausePressed)
            {
                IsPaused = !IsPaused;
                if (_sfx != null) _sfx.Play(Sfx.Denied, 0.5f, 0.8f);
            }

            if (IsPaused) return true;

            return false;
        }

        // ------------------------------------------------------------------
        // Aiming and the quill
        // ------------------------------------------------------------------

        void UpdateAim()
        {
            Vector3 fallback = ScribePosition + transform.forward * 3f;

            if (_rig == null || _rig.View == null)
            {
                AimPoint = fallback;
                return;
            }

            Ray ray = _rig.View.ScreenPointToRay(InputRouter.PointerScreen);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float enter;

            if (!plane.Raycast(ray, out enter))
            {
                AimPoint = fallback;
                return;
            }

            Vector3 point = ray.GetPoint(enter);
            Vector2 flat = new Vector2(point.x, point.z);
            float limit = PageRadius - 0.3f;
            if (flat.magnitude > limit) flat = flat.normalized * limit;

            AimPoint = new Vector3(flat.x, 0f, flat.y);
        }

        void TryInscribe()
        {
            // The quill only carries so far.
            float reachDistance = (AimPoint - ScribePosition).magnitude;
            if (reachDistance > Reach)
            {
                if (_reachWarnCooldown <= 0f)
                {
                    _reachWarnCooldown = 0.9f;
                    if (_sfx != null) _sfx.Play(Sfx.Denied, 0.45f, 1.0f);
                    if (_hud != null) _hud.Toast("OUT OF REACH");
                }
                return;
            }

            // 1. An erasure under the nib is un-written.
            Erasure target = NearestErasure(AimPoint, InkDamageRadius);
            if (target != null)
            {
                bool killed = target.TakeDamage(InkDamage);
                _scribe.SpendInk();
                _scribe.Lash();

                TransientRing.Spawn(_layerRoot, AimPoint, 0.15f, 0.85f, Palette.InkGlow, 0.30f, 3.4f);
                if (_rig != null) _rig.Shake(killed ? 0.10f : 0.04f);
                if (_sfx != null) _sfx.Play(Sfx.Ink, 0.85f, Random.Range(0.92f, 1.10f));

                if (killed) KillErasure(target);
                return;
            }

            // 2. A dark wick is relit.
            Candle dark = NearestCandle(AimPoint, 1.25f, true);
            if (dark != null)
            {
                dark.Relight(0.55f);
                _scribe.SpendInk();
                _scribe.Lash();
                TransientRing.Spawn(_layerRoot, dark.transform.position, 0.2f, 1.6f, Palette.Ember, 0.5f, 2.6f);
                if (_sfx != null) _sfx.Play(Sfx.Start, 0.7f, 1.25f);
                if (_hud != null) _hud.Toast("A FLAME RETURNS");
                return;
            }

            // 3. A burning candle is fed.
            Candle lit = NearestCandle(AimPoint, 1.25f, false);
            if (lit != null)
            {
                lit.Kindle(0.15f);
                _scribe.SpendInk();
                _scribe.Lash();
                TransientRing.Spawn(_layerRoot, lit.transform.position, 0.2f, 1.2f, Palette.Ember, 0.4f, 2.0f);
                if (_sfx != null) _sfx.Play(Sfx.Ink, 0.6f, 1.35f);
                return;
            }

            // 4. A ward is reinforced.
            Ward ward = NearestWard(AimPoint, 1.05f);
            if (ward != null)
            {
                ward.Reinforce(22f);
                _scribe.SpendInk();
                _scribe.Lash();
                TransientRing.Spawn(_layerRoot, ward.transform.position, 0.2f, 1.0f, Palette.InkGlow, 0.35f, 2.0f);
                if (_sfx != null) _sfx.Play(Sfx.Ink, 0.7f, 1.20f);
                return;
            }

            // 5. Nothing there: a new seal is set.
            Ward placed = Ward.Create(_layerRoot, AimPoint);
            _wards.Add(placed);
            WardsThisLayer++;
            _totalWards++;
            Score += 12;

            _scribe.SpendInk();
            _scribe.Lash();

            if (_sfx != null) _sfx.Play(Sfx.Ward, 0.9f, Random.Range(0.94f, 1.12f));
            if (_rig != null) _rig.Shake(0.035f);

            if (WardsThisLayer == WardsRequired && _hud != null)
            {
                _hud.Toast("THE PAGE IS SEALED - KEEP THE FLAMES");
            }

        }

        void TryIlluminate()
        {
            if (_illuminateCooldown > 0f || _scribe == null) return;

            _illuminateCooldown = IlluminateCooldownLength;

            Vector3 center = ScribePosition;
            TransientRing.Spawn(_layerRoot, center, 0.4f, 7.4f, Color.white, 0.55f, 4.0f);
            TransientRing.Spawn(_layerRoot, center, 0.2f, 4.2f, Palette.InkGlow, 0.70f, 3.0f);
            if (_rig != null) _rig.Shake(0.14f);
            if (_sfx != null) _sfx.Play(Sfx.Illuminate, 1.0f, 1.0f);

            for (int i = _erasures.Count - 1; i >= 0; i--)
            {
                Erasure erasure = _erasures[i];
                if (erasure == null) continue;

                float distance = Vector3.Distance(erasure.transform.position, center);
                if (distance > 7.5f) continue;

                float falloff = 1f - Mathf.Clamp01(distance / 7.5f);
                bool killed = erasure.TakeDamage(26f + 58f * falloff);
                if (killed) KillErasure(erasure);
            }

            for (int i = 0; i < _candles.Count; i++)
            {
                Candle candle = _candles[i];
                if (candle == null) continue;
                if (Vector3.Distance(candle.transform.position, center) < 7.5f) candle.Kindle(0.12f);
            }

            if (_hud != null) _hud.Toast("ILLUMINATION");
        }

        void TryScrape()
        {
            if (_scrapeCooldown > 0f || _scribe == null) return;

            Vector3 center = ScribePosition;
            int broken = 0;

            for (int i = _wards.Count - 1; i >= 0; i--)
            {
                Ward ward = _wards[i];
                if (ward == null) continue;
                if (Vector3.Distance(ward.transform.position, center) > 4.4f) continue;

                broken++;
                Destroy(ward.gameObject);
                _wards.RemoveAt(i);
            }

            if (broken == 0)
            {
                if (_sfx != null) _sfx.Play(Sfx.Denied, 0.6f, 0.85f);
                if (_hud != null) _hud.Toast("NOTHING TO SCRAPE");
                return;
            }

            _scrapeCooldown = ScrapeCooldownLength;
            WardsThisLayer = Mathf.Max(0, WardsThisLayer - broken);
            HealIntegrity(7f + broken * 1.5f);

            TransientRing.Spawn(_layerRoot, center, 0.3f, 4.4f, Palette.ErasureGlow, 0.55f, 3.2f);
            if (_rig != null) _rig.Shake(0.16f);
            if (_sfx != null) _sfx.Play(Sfx.Scrape, 1.0f, 0.9f);
            if (_hud != null) _hud.Toast("THE PAGE IS SCRAPED - " + broken + " SEALS LOST");

            for (int i = _erasures.Count - 1; i >= 0; i--)
            {
                Erasure erasure = _erasures[i];
                if (erasure == null) continue;

                float distance = Vector3.Distance(erasure.transform.position, center);
                if (distance > 4.4f) continue;

                float falloff = 1f - Mathf.Clamp01(distance / 4.4f);
                bool killed = erasure.TakeDamage(30f + 40f * falloff);
                if (killed) KillErasure(erasure);
            }
        }

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------

        void TickCandles(float dt)
        {
            for (int i = 0; i < _candles.Count; i++)
            {
                if (_candles[i] != null) _candles[i].Tick(dt);
                if (IsOver) return;
            }
        }

        void TickErasures(float dt)
        {
            // Reverse iteration: a kill removes an entry from the list mid-loop.
            for (int i = _erasures.Count - 1; i >= 0; i--)
            {
                if (i >= _erasures.Count) continue;

                Erasure erasure = _erasures[i];
                if (erasure == null) continue;

                erasure.Tick(dt, this);
                if (erasure.IsDead) KillErasure(erasure);
                if (IsOver) return;
            }
        }

        void PruneLists()
        {
            for (int i = _wards.Count - 1; i >= 0; i--)
            {
                if (_wards[i] == null) _wards.RemoveAt(i);
            }
            for (int i = _candles.Count - 1; i >= 0; i--)
            {
                if (_candles[i] == null) _candles.RemoveAt(i);
            }
            for (int i = _erasures.Count - 1; i >= 0; i--)
            {
                if (_erasures[i] == null) _erasures.RemoveAt(i);
            }
        }

        void UpdateSpawner(float dt)
        {
            if (IsOver || IsPaused || IsTitle || _advanceTimer >= 0f) return;

            int alive = 0;
            for (int i = 0; i < _erasures.Count; i++)
            {
                if (_erasures[i] != null) alive++;
            }

            if (alive >= _erasureTarget) return;

            _spawnTimer -= dt;
            if (_spawnTimer > 0f) return;

            _spawnTimer = _erasureSpawnInterval;
            SpawnErasure();
        }

        void UpdateAdvance(float dt)
        {
            if (IsOver) return;

            bool sealed_ = WardsThisLayer >= WardsRequired;
            bool lit = LivingFlames >= _salientFlameQuota;

            if (!sealed_ || !lit)
            {
                // Seals can be eaten and flames can gutter; the turn is abandoned
                // and the siege with it.
                if (_advanceTimer >= 0f)
                {
                    _advanceTimer = -1f;
                    EndSiege();
                    if (_hud != null) _hud.Toast("THE TURN IS BROKEN");
                }
                return;
            }

            if (_advanceTimer < 0f)
            {
                _advanceTimer = SiegeLength;
                BeginSiege();
            }

            _advanceTimer -= dt;
            if (_advanceTimer <= 0f)
            {
                _advanceTimer = -1f;
                CompleteLayer();
            }
        }

        void UpdateAtmosphere(float dt)
        {
            if (_underglow != null)
            {
                float t = Mathf.Clamp01((Layer - 1) / (float)FinalLayer);
                float breathe = 0.55f + Mathf.Sin(_runTime * 0.7f) * 0.18f;
                MaterialFactory.ApplyEmission(_underglow, Palette.ErasureGlow, (0.35f + t * 1.5f) * breathe);
            }

            if (_heartMaterial != null)
            {
                float inv = Mathf.Clamp01(Integrity / 100f);
                MaterialFactory.ApplyColor(_heartMaterial, Color.Lerp(Palette.Alert, Palette.Rule, inv * 0.8f));
            }

            if (_sfx != null)
            {
                float tension = Mathf.Clamp01((Layer - 1) / (float)FinalLayer) * 0.7f
                              + (1f - Mathf.Clamp01(Integrity / 100f)) * 0.3f;
                _sfx.SetTension(tension);
            }
        }

        // ------------------------------------------------------------------
        // Siege: the page refuses to turn
        // ------------------------------------------------------------------

        const float SiegeLength = 4.5f;

        void BeginSiege()
        {
            if (_hud != null) _hud.Toast("THE UNDERTEXT RISES");
            if (_sfx != null)
            {
                _sfx.Play(Sfx.Surge, 1.0f, 0.9f);
                _sfx.Play(Sfx.Layer, 0.7f, 1.15f);
            }
            if (_rig != null) _rig.Shake(0.22f);

            for (int i = 0; i < _erasures.Count; i++)
            {
                if (_erasures[i] != null) _erasures[i].SetProvoked(true);
            }

            int surge = Mathf.Min(6, 2 + Layer / 2 + Layer % 2);
            for (int i = 0; i < surge; i++) SpawnErasure(true);

            TransientRing.Spawn(_layerRoot, Vector3.zero, 1.0f, PageRadius, Palette.ErasureGlow, 0.9f, 2.4f);
        }

        void EndSiege()
        {
            for (int i = 0; i < _erasures.Count; i++)
            {
                if (_erasures[i] != null) _erasures[i].SetProvoked(false);
            }
        }

        // ------------------------------------------------------------------
        // Spawning
        // ------------------------------------------------------------------

        void SpawnErasure(bool provoked = false)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = PageRadius + Random.Range(1.5f, 4.0f);
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            float size = Random.Range(0.46f, 0.66f) + Mathf.Min(0.18f, Layer * 0.02f);
            float health = _erasureHealthBase + Random.Range(-4f, 8f);
            float damage = 12f + Layer * 1.4f;

            Erasure erasure = Erasure.Create(_layerRoot, position, size, _erasureSpeed, health, damage);
            if (provoked) erasure.SetProvoked(true);
            _erasures.Add(erasure);

            TransientRing.Spawn(_layerRoot, position, 0.2f, 1.4f, Palette.ErasureGlow, 0.5f, 2.6f);
        }

        void KillErasure(Erasure erasure)
        {
            if (erasure == null) return;

            _erasures.Remove(erasure);
            Score += 25;

            TransientRing.Spawn(_layerRoot, erasure.transform.position, 0.25f, 1.8f, Palette.InkGlow, 0.45f, 3.0f);
            TransientRing.Spawn(_layerRoot, erasure.transform.position, 0.15f, 1.0f, Palette.ErasureGlow, 0.6f, 2.0f);

            if (_sfx != null) _sfx.Play(Sfx.Seal, 0.55f, Random.Range(1.0f, 1.25f));
            Destroy(erasure.gameObject);
        }

        // ------------------------------------------------------------------
        // Queries used by actors
        // ------------------------------------------------------------------

        public Candle CandleAt(int index)
        {
            if (index < 0 || index >= _candles.Count) return null;
            return _candles[index];
        }

        public Candle NearestLivingCandle(Vector3 from)
        {
            Candle best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < _candles.Count; i++)
            {
                Candle candle = _candles[i];
                if (candle == null || candle.IsOut) continue;

                float sqr = (candle.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candle;
                }
            }
            return best;
        }

        Candle NearestCandle(Vector3 from, float maxDistance, bool wantOut)
        {
            Candle best = null;
            float bestSqr = maxDistance * maxDistance;

            for (int i = 0; i < _candles.Count; i++)
            {
                Candle candle = _candles[i];
                if (candle == null) continue;
                if (candle.IsOut != wantOut) continue;

                float sqr = (candle.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candle;
                }
            }
            return best;
        }

        public Ward NearestWard(Vector3 from, float maxDistance)
        {
            Ward best = null;
            float bestSqr = maxDistance * maxDistance;

            for (int i = 0; i < _wards.Count; i++)
            {
                Ward ward = _wards[i];
                if (ward == null || ward.IsBroken) continue;

                float sqr = (ward.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = ward;
                }
            }
            return best;
        }

        Erasure NearestErasure(Vector3 from, float maxDistance)
        {
            Erasure best = null;
            float bestSqr = maxDistance * maxDistance;

            for (int i = 0; i < _erasures.Count; i++)
            {
                Erasure erasure = _erasures[i];
                if (erasure == null || erasure.IsDead) continue;

                float sqr = (erasure.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = erasure;
                }
            }
            return best;
        }

        public Vector3 SeparationFor(Erasure self)
        {
            Vector3 push = Vector3.zero;
            int neighbours = 0;
            Vector3 position = self.transform.position;
            float personalSpace = 1.25f;
            float sqrSpace = personalSpace * personalSpace;

            for (int i = 0; i < _erasures.Count; i++)
            {
                Erasure other = _erasures[i];
                if (other == null || other == self) continue;

                Vector3 delta = position - other.transform.position;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr > sqrSpace || sqr < 0.0001f) continue;

                push += delta.normalized * (1f - Mathf.Sqrt(sqr) / personalSpace);
                neighbours++;
            }

            if (neighbours == 0) return Vector3.zero;
            return push / neighbours;
        }

        // ------------------------------------------------------------------
        // Damage and progression
        // ------------------------------------------------------------------

        public void WoundScribe(float amount)
        {
            if (IsOver || amount <= 0f) return;

            Integrity -= amount;
            if (_rig != null && _hurtCooldown <= 0f)
            {
                _rig.Shake(0.12f);
                _hurtCooldown = 0.35f;
                if (_sfx != null) _sfx.Play(Sfx.Surge, 0.35f, 1.4f);
            }

            if (Integrity <= 0f)
            {
                Integrity = 0f;
                EndRun(false);
            }
        }

        void HealIntegrity(float amount)
        {
            Integrity = Mathf.Min(100f, Integrity + amount);
        }

        void OnCandleSnuffed(Candle candle)
        {
            if (IsOver) return;

            Score -= 5;
            HealIntegrity(-7f);
            if (_rig != null) _rig.Shake(0.10f);
            if (_sfx != null) _sfx.Play(Sfx.Flame, 1.0f, 0.85f);
            if (_hud != null) _hud.Toast("A FLAME GOES OUT");

            if (Integrity <= 0f) EndRun(false);
        }

        void OnWardBroken(Ward ward)
        {
            // A broken ward was already sealed; it no longer counts toward this layer.
            WardsThisLayer = Mathf.Max(0, WardsThisLayer - 1);
            Score -= 4;
            if (_sfx != null) _sfx.Play(Sfx.Seal, 0.45f, 0.7f);
        }

        void CompleteLayer()
        {
            Score += 120 * Layer;
            HealIntegrity(16f);

            if (Layer >= FinalLayer)
            {
                EndRun(true);
                return;
            }

            Layer++;
            BeginLayer();
        }

        void EndRun(bool victory)
        {
            if (IsOver) return;

            IsOver = true;
            IsVictory = victory;
            IsPaused = false;

            if (_sfx != null)
            {
                _sfx.Play(victory ? Sfx.Layer : Sfx.Over, 1.0f, victory ? 1.15f : 0.9f);
                _sfx.Silence();
            }
            if (_rig != null) _rig.Shake(victory ? 0.2f : 0.45f);
        }

        // ------------------------------------------------------------------
        // Run and layer setup
        // ------------------------------------------------------------------

        void BeginRun()
        {
            Layer = 1;
            Folio = 1;
            Integrity = 100f;
            Score = 0;
            _totalWards = 0;
            IsOver = false;
            IsVictory = false;
            _illuminateCooldown = 0f;
            _scrapeCooldown = 0f;

            if (_scribe != null) _scribe.transform.position = Vector3.zero;

            BeginLayer();
            if (_sfx != null)
            {
                _sfx.Resume();
                _sfx.Play(Sfx.Start, 0.9f, 1.0f);
            }
            if (_hud != null) _hud.Toast("LAYER I - " + FolioTitle);
        }

        void RestartRun()
        {
            IsOver = false;
            BeginRun();
        }

        void BeginLayer()
        {
            ClearLayer();

            Folio = Layer;
            WardsThisLayer = 0;
            WardsRequired = 11 + Layer * 2;
            _advanceTimer = -1f;
            _hurtCooldown = 0f;
            _layerSeed = Random.Range(1000, 999999);

            int candleCount = Mathf.Min(4 + (Layer - 1) / 2, 7);
            _salientFlameQuota = Mathf.Max(2, Mathf.CeilToInt(candleCount * 0.65f));

            _erasureTarget = Mathf.Min(2 + Layer, 12);
            _erasureHealthBase = 34 + Layer * 7;
            _erasureSpeed = Mathf.Min(6.20f, 3.10f + Layer * 0.36f);
            _erasureSpawnInterval = Mathf.Max(1.10f, 3.4f - Layer * 0.24f);
            _spawnTimer = 1.2f;

            SpawnCandles(candleCount);

            if (_scribe != null) _scribe.transform.position = Vector3.zero;
            if (_rig != null) _rig.Follow(_scribe != null ? _scribe.transform : null);

            // Two erasures arrive immediately so the page is never idle.
            SpawnErasure();
            SpawnErasure();

            if (_hud != null) _hud.Toast("LAYER " + Hud.Roman(Layer) + " - " + FolioTitle);
            if (_sfx != null) _sfx.Play(Sfx.Layer, 0.55f, 0.85f);

            TransientRing.Spawn(_layerRoot, Vector3.zero, 0.5f, PageRadius * 0.9f, Palette.InkGlow, 1.1f, 2.0f);
        }

        void ClearLayer()
        {
            for (int i = _erasures.Count - 1; i >= 0; i--)
            {
                if (_erasures[i] != null) Destroy(_erasures[i].gameObject);
            }
            for (int i = _wards.Count - 1; i >= 0; i--)
            {
                if (_wards[i] != null) Destroy(_wards[i].gameObject);
            }
            for (int i = _candles.Count - 1; i >= 0; i--)
            {
                if (_candles[i] != null) Destroy(_candles[i].gameObject);
            }

            _erasures.Clear();
            _wards.Clear();
            _candles.Clear();

            if (_layerRoot == null) return;
            for (int i = _layerRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_layerRoot.GetChild(i).gameObject);
            }
        }

        void SpawnCandles(int count)
        {
            // Candles begin part-burnt and burn faster each layer: the flame budget is
            // the clock the player is racing, not a permanent safety net.
            float fuelBurn = 0.0235f + Layer * 0.0012f;
            float startFuel = 0.58f;

            System.Random rng = new System.Random(_layerSeed);

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + (float)rng.NextDouble() * 0.55f;
                float radius = 8.6f + ((float)rng.NextDouble() - 0.5f) * 1.4f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                Candle candle = Candle.Create(_layerRoot, position, startFuel, fuelBurn);
                _candles.Add(candle);

                // A lit candle early in a layer should read clearly from above.
                TransientRing.Spawn(_layerRoot, position, 0.2f, 1.1f, Palette.Ember, 0.6f, 1.8f);
            }
        }
    }
}
