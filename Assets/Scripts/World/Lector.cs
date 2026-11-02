using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Palimpsest
{
    /// <summary>
    /// THE LECTOR - the page is read aloud.
    ///
    /// <para>
    /// The sediment answered <i>what the page remembers</i>. The counter-hand answered
    /// <i>who wrote it</i>. The colophon answered <i>who is still watching</i>. The gloss
    /// answered <i>what the page says about itself</i>. Every one of those is a writer:
    /// marks, rivals, witnesses, commentary. A manuscript, in the end, is not finished by
    /// being written - it is finished by being read, and the reader is the only hand in the
    /// scriptorium that leaves no mark of its own.
    /// </para>
    ///
    /// <para>
    /// The lector walks the page in lines, the way a finger tracks a text: down one line,
    /// down the next, reversing at each margin. Everything in its sightline is being held
    /// up to be read, and the undertext cannot stand in the light of it. Erasures caught in
    /// the line burn, are shoved out of it, and take a correction at every stroke. A blot
    /// under the reader's regard never ripens, and a wick it reads by is steadied.
    /// </para>
    ///
    /// <para>
    /// It costs something, and the cost is the palimpsest's own problem. <b>A reader will
    /// not read over wet ink.</b> A seal set in front of the moving line startles it: it
    /// loses the place, recoils, and hurries the rest of the line. A page can therefore be
    /// written or read, and not both at once in the same place - which is why the scribe
    /// who wants the shelter has to stop sealing and go and fight in it, and the scribe who
    /// wants to build has to build where the line is not. Building is quietest where the
    /// reader has just been; killing is cheapest where it is about to be.
    /// </para>
    ///
    /// <para>
    /// Nerve is the second cost. Every erasure the reader has to hold up and read through
    /// wears it down; when the nerve is gone the reader <b>closes the book</b> and the line
    /// goes dark for a while. It cannot read everything, which is exactly the point: the
    /// shelter is at its weakest in the fights that need it most.
    /// </para>
    ///
    /// <para>
    /// And it reads <b>the record</b>. The lector takes its voice from this repository's own
    /// ledger of contributing hands - the same file the colophon summons witnesses out of -
    /// and it reads it <b>backwards</b>: the newest entry first, then the one before it, one
    /// hand per layer as the folio deepens. The page is written forwards and read backwards.
    /// What a hand put in <c>primary_action</c> decides how it reads: a hand that built on
    /// the page reads slowly and steadily and keeps its nerve; a hand that turned the page
    /// somewhere else will not hold a line and skips; a hand that destroyed something reads
    /// what is underneath instead - it burns the undertext hardest, ripens the blots in its
    /// sightline, and will not steady a flame. Signing the ledger is signing how you are
    /// read.
    /// </para>
    ///
    /// <para>
    /// The system is bolted onto the finished arena the way every other layer was: a runtime
    /// director attaches itself, hangs the reader off the page transform (so a page turn does
    /// not remove it), finds erasures and seals by reading the layer transform, reads candles
    /// through the arena's public surface and blots through <see cref="Sediment"/>.
    /// <c>Arena.cs</c> was not modified, so deleting <c>Lector.cs</c> removes the reader
    /// without removing the game.
    /// </para>
    /// </summary>
    public class Lector : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Tuning
        // ------------------------------------------------------------------

        /// <summary>
        /// Half the length of the sightline, along the line being read. It is deliberately
        /// long: this is a line of text, not a lamp, and it should span a third of the leaf.
        /// </summary>
        public const float LineHalfLength = 4.40f;

        /// <summary>Half the breadth of the sightline, across the line being read.</summary>
        public const float LineHalfWidth = 1.15f;

        /// <summary>How many lines one leaf of this page holds.</summary>
        public const int Rows = 7;

        /// <summary>Distance between two ruled lines.</summary>
        public const float RowSpacing = 2.6f;

        /// <summary>
        /// How far out along a line the reader walks before turning back. It stops short of
        /// the leaf's edge so the sightline never hangs off the vellum.
        /// </summary>
        public const float ReadWidth = 7.6f;

        /// <summary>Walking pace of a steady hand.</summary>
        const float SpeedBase = 3.05f;

        /// <summary>Walking pace of a hand that will not settle.</summary>
        const float SpeedRestless = 4.15f;

        /// <summary>Multiplier applied while hurrying away from fresh ink.</summary>
        const float HurryFactor = 1.9f;

        /// <summary>The pause at the end of a line.</summary>
        const float LineBreak = 0.5f;

        /// <summary>The pause at the foot of the leaf, when the reader turns back to the top.</summary>
        const float WrapTime = 1.5f;

        /// <summary>Seconds a hand takes to come up through the vellum.</summary>
        const float RiseTime = 1.15f;

        /// <summary>Seconds the reader stands still after being startled.</summary>
        const float StumbleHold = 0.8f;

        /// <summary>How far it recoils from the ink that startled it.</summary>
        const float Recoil = 0.42f;

        /// <summary>How long it hurries after a stumble, in seconds of walking.</summary>
        const float HurryTime = 5f;

        /// <summary>Damage per second an erasure takes at the centre of the line.</summary>
        const float BurnBase = 6.5f;
        const float BurnPerLayer = 1.3f;

        /// <summary>What each disposition does to that rate.</summary>
        const float EndorsingBurn = 0.92f;
        const float RestlessBurn = 1.12f;
        const float UndertextBurn = 1.70f;

        /// <summary>Seconds between the reader's corrections, and what one carries.</summary>
        const float StrokeInterval = 1.15f;
        const float StrokeBase = 6.0f;
        const float StrokePerLayer = 1.8f;

        /// <summary>How far a correction shoves the undertext out of the line.</summary>
        const float StrokePush = 1.35f;

        /// <summary>Wax per second returned to a wick the reader is reading by.</summary>
        const float CandleSteadyRate = 0.020f;

        /// <summary>Damage per second the reader's lamp does to the rival scribe's hand.</summary>
        const float HandBurnBase = 5.0f;
        const float HandBurnPerLayer = 1.2f;

        /// <summary>Blot strength the reader wears away per second, and charge an erasing hand adds.</summary>
        const float StainSettleRate = 0.006f;
        const float StainRipenRate = 0.012f;
        const float SedimentInterval = 0.25f;

        /// <summary>Nerve lost per erasure held in the line, per second.</summary>
        const float NerveDrainPerErasure = 0.030f;

        /// <summary>Nerve recovered per second while the line is clean.</summary>
        const float NerveRecover = 0.055f;

        /// <summary>Nerve lost when ink is set in the reader's path.</summary>
        const float StumbleCost = 0.070f;

        /// <summary>
        /// Nerve lost every time the line reads an erasure clean out of the page. A reader can
        /// bear a certain amount of violence and no more, which is the whole limit on the
        /// shelter: the line is not a wall, it is an attention, and attention runs out.
        /// </summary>
        const float NerveKillCost = 0.12f;

        /// <summary>
        /// Seconds between two startles. Without this the reader stutters constantly while a
        /// scribe builds anywhere near it, which reads as noise rather than as a cost.
        /// </summary>
        const float StumbleGate = 4.0f;

        /// <summary>Ink younger than this still reads as wet.</summary>
        const float WetInkWindow = 6f;

        /// <summary>Seconds the book stays closed once the nerve is spent.</summary>
        const float ClosedTime = 13f;

        /// <summary>How often the page is re-read for ink set under the reader's eye.</summary>
        const float ScanInterval = 0.16f;

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        /// <summary>The hand whose words this reader is reading.</summary>
        public Contributor Hand { get; private set; }

        /// <summary>0 when the reader has given up, 1 when its attention is whole.</summary>
        public float Nerve { get; private set; }

        /// <summary>True while the sightline is on the page.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>How the words it is reading were written, which decides how it reads.</summary>
        public HandDisposition Disposition
        {
            get { return Hand != null ? Hand.Disposition : HandDisposition.Endorsing; }
        }

        /// <summary>The line of the leaf the reader is currently on.</summary>
        public int RowIndex { get { return _row; } }

        /// <summary>Seconds until the book opens again; zero while it is open.</summary>
        public float ClosedRemaining { get { return _closed; } }

        /// <summary>The colour the reader wears, taken from the hand it is reading.</summary>
        public Color Hue { get; private set; }

        public string Name { get { return Hand != null ? Hand.ShortName : "A READER"; } }

        /// <summary>The colour of the sightline itself.</summary>
        public Color Tint { get { return Color.Lerp(Palette.Reading, Hue, 0.42f); } }

        // ------------------------------------------------------------------
        // Visual
        // ------------------------------------------------------------------

        Transform _view;
        Transform _lineRoot;
        Transform _place;
        Transform _foot;
        Transform _lantern;
        Light _light;

        Material _robeMaterial;
        Material _lineMaterial;
        Material _bandMaterial;
        Material _sightMaterial;

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------

        readonly Dictionary<int, float> _wardBorn = new Dictionary<int, float>();
        readonly HashSet<int> _stumbledOnce = new HashSet<int>();
        readonly HashSet<int> _liveWards = new HashSet<int>();

        int _row;
        float _x;
        float _dir = 1f;
        float _hold;
        float _rise;
        float _bow;
        float _hurry;
        float _stumble;
        float _stroke;
        float _scan;
        float _stumbleGate;
        float _sedimentTimer;
        float _closed;
        float _seed;

        bool _stumbledFlag;
        bool _closedFlag;
        bool _openedFlag;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        /// <summary>
        /// Summons a reader onto the page. It hangs off the page transform (not the layer),
        /// because a page turn does not remove the fact that the page is being read.
        /// </summary>
        public static Lector Create(Transform parent, Contributor hand)
        {
            GameObject root = new GameObject("Lector");
            if (parent != null) root.transform.SetParent(parent, false);

            Lector lector = root.AddComponent<Lector>();
            lector._seed = Random.Range(0f, 100f);
            lector.BuildView();
            lector.SetHand(hand, true);
            return lector;
        }

        void Awake()
        {
            /*
             * A note from the hand that reads.
             * Syllables: 5 - 7 - 5.
             */
            Debug.Log("The page is read twice / a voice that leaves no mark on / anything it reads");
        }

        void BuildView()
        {
            Color linen = Color.Lerp(Palette.Night, Palette.Lector, 0.40f);

            _robeMaterial = MaterialFactory.DynamicEmissive(linen, 0.35f);
            _lineMaterial = MaterialFactory.DynamicEmissive(Tint, 0.14f);
            _bandMaterial = MaterialFactory.DynamicEmissive(Tint, 0.55f);
            _sightMaterial = MaterialFactory.DynamicEmissive(Tint, 1.10f);

            // Everything that comes up out of the page hangs off one transform so the
            // arrival can be animated as a single motion.
            GameObject view = new GameObject("Figure");
            view.transform.SetParent(transform, false);
            _view = view.transform;

            MeshFactory.Primitive(PrimitiveType.Cylinder, "Reader", _view,
                new Vector3(0f, 0.62f, 0f), new Vector3(0.25f, 0.62f, 0.25f), _robeMaterial);

            MeshFactory.Primitive(PrimitiveType.Sphere, "Head", _view,
                new Vector3(0f, 1.30f, 0f), new Vector3(0.20f, 0.23f, 0.20f), _robeMaterial);

            // No eye. Where the scribe has a mask and the witness has a regard, the reader
            // has a band across its face: it is not looking at you, or at the hazard, it is
            // looking at the words.
            MeshFactory.Primitive(PrimitiveType.Cube, "Blindfold", _view,
                new Vector3(0f, 1.32f, 0.12f), new Vector3(0.30f, 0.07f, 0.07f), _bandMaterial);

            // The one thing it carries, and it is not a quill: an open book, and the lamp
            // it reads by. The lantern hangs forward of the figure, in the direction the
            // line is being read.
            MeshFactory.Primitive(PrimitiveType.Cube, "Book Left", _view,
                new Vector3(-0.10f, 1.02f, 0.20f), new Vector3(0.18f, 0.02f, 0.26f),
                Quaternion.Euler(0f, 0f, 14f), _bandMaterial);

            MeshFactory.Primitive(PrimitiveType.Cube, "Book Right", _view,
                new Vector3(0.10f, 1.02f, 0.20f), new Vector3(0.18f, 0.02f, 0.26f),
                Quaternion.Euler(0f, 0f, -14f), _bandMaterial);

            _lantern = MeshFactory.Primitive(PrimitiveType.Cube, "Lantern", _view,
                new Vector3(0f, 0.96f, 0.40f), new Vector3(0.13f, 0.17f, 0.11f), _sightMaterial).transform;

            GameObject lightObject = new GameObject("Reading Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.05f, 0.25f);

            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = Tint;
            _light.range = 4.5f;
            _light.intensity = 1.1f;
            _light.shadows = LightShadows.None;

            // The sightline: two faint rules marking the breadth of the regard, a brighter
            // rule down its middle, and a soft band of lamplight between them. It is the
            // same shape as the scribe's ruling already on the page, which is the point -
            // the reader's line is one of the ruled guides brought to life.
            GameObject lineRoot = new GameObject("Sightline");
            lineRoot.transform.SetParent(transform, false);
            _lineRoot = lineRoot.transform;

            // A disc is round, so this is not the band itself but the bloom of lamplight
            // under the line: kept very dim, and flattened to the shape of the regard.
            Transform bloom = MeshFactory.MeshObject("Sight Bloom", MeshFactory.UnitDisc,
                _lineMaterial, _lineRoot).transform;
            bloom.localPosition = new Vector3(0f, 0.031f, 0f);
            bloom.localScale = new Vector3(LineHalfLength * 0.85f, 1f, LineHalfWidth * 0.80f);

            Rule("Rule Far", LineHalfWidth);
            Rule("Rule Near", -LineHalfWidth);

            MeshFactory.Primitive(PrimitiveType.Cube, "Sight Rule", _lineRoot,
                new Vector3(0f, 0.038f, 0f), new Vector3(LineHalfLength * 2f, 0.004f, 0.055f), _sightMaterial);

            // The reader's place: a small ring at the leading edge of the line, where the
            // finger would be. It is the only part of the line that moves on its own.
            _place = MeshFactory.MeshObject("Place", MeshFactory.UnitRing, _sightMaterial, _lineRoot).transform;
            _place.localPosition = new Vector3(LineHalfLength, 0.048f, 0f);
            _place.localScale = new Vector3(0.34f, 1f, 0.34f);

            // A ring under the figure itself, so the reader has a readable station.
            _foot = MeshFactory.MeshObject("Station", MeshFactory.UnitRing, _bandMaterial, transform).transform;
            _foot.localPosition = new Vector3(0f, 0.052f, 0f);
            _foot.localScale = new Vector3(0.72f, 1f, 0.72f);
        }

        void Rule(string name, float z)
        {
            // Sit above the candle halos and the sediment's rings: the reader's line is an
            // annotation on top of the page's own furniture, not part of it.
            MeshFactory.Primitive(PrimitiveType.Cube, name, _lineRoot,
                new Vector3(0f, 0.034f, z), new Vector3(LineHalfLength * 2f, 0.003f, 0.035f), _bandMaterial);
        }

        // ------------------------------------------------------------------
        // Commission
        // ------------------------------------------------------------------

        /// <summary>
        /// Puts a hand's words in front of the reader. On the first summon it rises through
        /// the vellum; on a page turn it simply turns to another hand and starts again at
        /// the top of the leaf.
        /// </summary>
        public void SetHand(Contributor hand, bool fromTheMargin)
        {
            Hand = hand;
            Hue = hand != null ? hand.Hue : Palette.Reading;

            Nerve = 1f;
            IsOpen = true;
            _closed = 0f;

            _stumbledFlag = false;
            _closedFlag = false;
            _openedFlag = false;

            _row = 0;
            _x = -ReadWidth;
            _dir = 1f;
            _hold = fromTheMargin ? RiseTime : LineBreak;
            _hurry = 0f;
            _stumble = 0f;
            _stroke = StrokeInterval;
            _scan = ScanInterval;
            _stumbleGate = 0f;
            _sedimentTimer = 0f;
            _rise = fromTheMargin ? 0f : Mathf.Min(_rise, 0.55f);

            _wardBorn.Clear();
            _stumbledOnce.Clear();
            _liveWards.Clear();

            if (_lineRoot != null) _lineRoot.gameObject.SetActive(true);
            if (_light != null)
            {
                _light.enabled = true;
                _light.color = Tint;
            }

            transform.position = new Vector3(_x, 0f, RowZ(_row));
        }

        /// <summary>The z of one ruled line on the page.</summary>
        public static float RowZ(int row)
        {
            return -((Rows - 1) * RowSpacing) * 0.5f + Mathf.Clamp(row, 0, Rows - 1) * RowSpacing;
        }

        // ------------------------------------------------------------------
        // Geometry
        // ------------------------------------------------------------------

        /// <summary>
        /// True when a point on the vellum is inside the reader's sightline, with a falloff
        /// that is strongest on the ruled line itself and weakest at the edges of the regard.
        /// </summary>
        public bool Contains(Vector3 world, out float falloff)
        {
            falloff = 0f;

            Vector3 delta = world - transform.position;
            delta.y = 0f;

            float along = Mathf.Abs(delta.x);
            float across = Mathf.Abs(delta.z);
            if (along > LineHalfLength || across > LineHalfWidth) return false;

            falloff = (1f - across / LineHalfWidth) * (1f - 0.45f * (along / LineHalfLength));
            return true;
        }

        // ------------------------------------------------------------------
        // Simulation - driven by LectorDirector
        // ------------------------------------------------------------------

        public void Tick(float dt, Arena arena, Transform layerRoot)
        {
            _rise = Mathf.Min(1f, _rise + dt / RiseTime);
            _stumble = Mathf.MoveTowards(_stumble, 0f, dt * 1.7f);
            _stumbleGate = Mathf.Max(0f, _stumbleGate - dt);

            if (!IsOpen)
            {
                _closed = Mathf.Max(0f, _closed - dt);
                Nerve = Mathf.Min(1f, Nerve + (dt / ClosedTime));
                Animate(dt);
                if (_closed <= 0f) Reopen();
                return;
            }

            Walk(dt);

            int inside = Read(dt, arena, layerRoot);

            if (inside > 0)
            {
                float drain = NerveDrainPerErasure * inside * dt * DrainFactor;
                // A page thick with silt is heavier to hold up than a clean one.
                if (arena != null) drain *= 1f + arena.SiltPressure * 0.6f;
                Nerve = Mathf.Max(0f, Nerve - drain);
            }
            else
            {
                Nerve = Mathf.Min(1f, Nerve + NerveRecover * RecoverFactor * dt);
            }

            if (Nerve <= 0f) Close();

            Animate(dt);
        }

        float Speed
        {
            get
            {
                float speed = Disposition == HandDisposition.Wandering ? SpeedRestless : SpeedBase;
                if (_hurry > 0f) speed *= HurryFactor;
                return speed;
            }
        }

        /// <summary>A hand that built on the page can bear more of it than one that did not.</summary>
        float DrainFactor
        {
            get
            {
                if (Disposition == HandDisposition.Endorsing) return 0.75f;
                if (Disposition == HandDisposition.Wandering) return 0.85f;
                return 1f;
            }
        }

        /// <summary>A hand that will not settle loses its nerve quickly and regains it quickly.</summary>
        float RecoverFactor
        {
            get
            {
                if (Disposition == HandDisposition.Wandering) return 1.6f;
                if (Disposition == HandDisposition.Erasing) return 0.8f;
                return 1f;
            }
        }

        // ------------------------------------------------------------------
        // Walking the page
        // ------------------------------------------------------------------

        void Walk(float dt)
        {
            _hurry = Mathf.Max(0f, _hurry - dt);

            if (_hold > 0f)
            {
                _hold = Mathf.Max(0f, _hold - dt);
                return;
            }

            _x += _dir * Speed * dt;

            if (_dir > 0f && _x >= ReadWidth)
            {
                _x = ReadWidth;
                BreakLine();
            }
            else if (_dir < 0f && _x <= -ReadWidth)
            {
                _x = -ReadWidth;
                BreakLine();
            }

            transform.position = new Vector3(_x, 0f, RowZ(_row));
        }

        /// <summary>
        /// The end of a line. Boustrophedon: the reader turns back on the next line and keeps
        /// its place across the page, exactly as the eye does. At the foot of the leaf it goes
        /// back to the top, a little wearier for having read the whole page.
        /// </summary>
        void BreakLine()
        {
            _dir = -_dir;
            _row++;

            if (_row >= Rows)
            {
                _row = 0;
                _hold = WrapTime;
                _rise = 0.12f;
                Nerve = Mathf.Max(0f, Nerve - 0.05f);
                return;
            }

            _hold = LineBreak;

            // A hand that would not settle does not read every line: it skips ahead, which
            // makes its shelter hard to plan around - the same temperament the colophon
            // gives it, read as a reading habit instead of a standing one.
            if (Disposition == HandDisposition.Wandering && Random.value > 0.55f)
            {
                _row = Mathf.Min(Rows - 1, _row + 1);
                _hold = LineBreak * 1.4f;
            }
        }

        // ------------------------------------------------------------------
        // Reading
        // ------------------------------------------------------------------

        /// <summary>
        /// Holds the line up to be read: burns the undertext in it, corrects it on a pulse,
        /// steadies the wick the reader is reading by, and reads the blots in its sightline.
        /// Returns how many erasures are standing in the line.
        /// </summary>
        int Read(float dt, Arena arena, Transform layerRoot)
        {
            if (arena == null) return 0;

            float burn = BurnBase + arena.Layer * BurnPerLayer;
            float strokeDamage = StrokeBase + arena.Layer * StrokePerLayer;

            switch (Disposition)
            {
                case HandDisposition.Erasing:
                    burn *= UndertextBurn;
                    break;
                case HandDisposition.Wandering:
                    burn *= RestlessBurn;
                    break;
                default:
                    burn *= EndorsingBurn;
                    break;
            }

            _stroke -= dt;
            bool stroke = _stroke <= 0f;
            if (stroke)
            {
                _stroke = StrokeInterval;
                Transform parent = layerRoot != null ? layerRoot : transform;
                TransientRing.Spawn(parent,
                    transform.position + new Vector3(_dir * LineHalfLength * 0.45f, 0f, 0f),
                    0.15f, LineHalfWidth * 1.9f, Tint, 0.55f, 1.7f);
            }

            int inside = 0;

            if (layerRoot != null)
            {
                for (int i = layerRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = layerRoot.GetChild(i);
                    if (child == null) continue;

                    Erasure erasure = child.GetComponent<Erasure>();
                    if (erasure == null || erasure.IsDead) continue;

                    float falloff;
                    if (!Contains(erasure.transform.position, out falloff)) continue;

                    inside++;

                    float amount = burn * falloff * dt;
                    if (stroke) amount += strokeDamage * falloff;

                    bool dead = erasure.TakeDamage(amount);
                    if (dead) Nerve = Mathf.Max(0f, Nerve - NerveKillCost * DrainFactor);
                    if (dead || !stroke) continue;

                    // A correction does not only wound the undertext, it puts it back out
                    // of the line: the reader pushes what it is reading past the margin of
                    // the ruled text, and the undertext has to walk back in.
                    Push(erasure, falloff);
                }
            }

            SteadyCandles(dt, arena);
            ReadTheOtherHand(dt, arena);
            ReadSediment(dt);

            _scan -= dt;
            if (_scan <= 0f && layerRoot != null)
            {
                _scan = ScanInterval;
                ScanInk(layerRoot);
            }

            return inside;
        }

        /// <summary>
        /// A wick the reader is reading by is a flame being attended to, and it lasts a little
        /// longer for it. A hand that destroyed something does not spare the light.
        /// </summary>
        void SteadyCandles(float dt, Arena arena)
        {
            if (Disposition == HandDisposition.Erasing) return;

            for (int i = 0; i < arena.CandleCount; i++)
            {
                Candle candle = arena.CandleAt(i);
                if (candle == null || candle.IsOut) continue;

                float falloff;
                if (!Contains(candle.transform.position, out falloff)) continue;

                candle.Kindle(CandleSteadyRate * dt * falloff);
            }
        }

        /// <summary>
        /// The rival scribe is undertext as much as anything else on this page, and the only
        /// thing that reaches it is light. A reader carries a lamp. Stand the earlier hand in
        /// the sightline and it is read, and being read burns it - slowly, the way candlelight
        /// burns it, unless the hand being read is one that destroyed something, in which case
        /// the reader is reading the undertext on purpose and it burns hardest.
        /// </summary>
        void ReadTheOtherHand(float dt, Arena arena)
        {
            CounterHandDirector director = CounterHandDirector.Instance;
            if (director == null || !director.IsHandAtWork) return;

            CounterHand hand = director.Hand;
            if (hand == null) return;

            float falloff;
            if (!Contains(hand.transform.position, out falloff)) return;

            float factor = Disposition == HandDisposition.Erasing ? 1.35f : 1f;
            float amount = (HandBurnBase + arena.Layer * HandBurnPerLayer) * falloff * factor * dt;

            // No knockback: the reader does not push the rival around, it only reads it.
            hand.TakeDamage(amount, transform.position, 0f);
        }

        /// <summary>
        /// Read steadily, a blot never ripens: the charge is turned over as fast as it gathers,
        /// so nothing blooms in the reader's sightline. The blot itself is not destroyed -
        /// the reader removes no mark from the page, it only declines to let the mark grow.
        /// A hand that destroyed something reads the reverse way round.
        /// </summary>
        void ReadSediment(float dt)
        {
            _sedimentTimer -= dt;
            if (_sedimentTimer > 0f) return;

            _sedimentTimer = SedimentInterval;

            Sediment sediment = Sediment.Instance;
            if (sediment == null) return;

            List<Stratum> strata = sediment.All;
            for (int i = 0; i < strata.Count; i++)
            {
                Stratum stratum = strata[i];
                if (stratum == null || stratum.Kind != StratumKind.Stain || stratum.IsSpent) continue;

                float falloff;
                if (!Contains(stratum.transform.position, out falloff)) continue;

                if (Disposition == HandDisposition.Erasing)
                {
                    stratum.Ferment(StainRipenRate * SedimentInterval * falloff);
                }
                else
                {
                    stratum.Sour(StainSettleRate * SedimentInterval * falloff);
                }
            }
        }

        /// <summary>
        /// Shoves an erasure out of the line, never off the page.
        /// </summary>
        void Push(Erasure erasure, float falloff)
        {
            Vector3 position = erasure.transform.position;
            float side = position.z - transform.position.z;
            float sign = side >= 0f ? 1f : -1f;

            Vector3 pushed = position + new Vector3(0f, 0f, sign * StrokePush * Mathf.Max(0.35f, falloff));

            Vector2 flat = new Vector2(pushed.x, pushed.z);
            float limit = Arena.PageRadius - 0.6f;
            if (flat.magnitude > limit) flat = flat.normalized * limit;

            erasure.transform.position = new Vector3(flat.x, 0f, flat.y);
        }

        /// <summary>
        /// A reader will not read over wet ink. Any seal set under the moving line in the last
        /// few seconds is still wet: the hand writing here is louder than the voice reading
        /// here, and the reader loses the place.
        /// </summary>
        void ScanInk(Transform layerRoot)
        {
            _liveWards.Clear();

            for (int i = 0; i < layerRoot.childCount; i++)
            {
                Ward ward = layerRoot.GetChild(i).GetComponent<Ward>();
                if (ward == null || ward.IsBroken) continue;

                int id = ward.GetInstanceID();
                _liveWards.Add(id);

                float born;
                if (!_wardBorn.TryGetValue(id, out born))
                {
                    born = Time.time;
                    _wardBorn[id] = born;
                }

                float falloff;
                if (!Contains(ward.transform.position, out falloff)) continue;
                if (Time.time - born > WetInkWindow) continue;
                if (_stumbledOnce.Contains(id)) continue;
                if (_stumbleGate > 0f) continue;

                _stumbledOnce.Add(id);
                _stumbleGate = StumbleGate;
                Stumble(falloff);
            }

            // Forget the seals that have been eaten or scraped away; the record stays bounded
            // no matter how long a folio runs.
            if (_wardBorn.Count > _liveWards.Count)
            {
                List<int> gone = new List<int>();
                foreach (KeyValuePair<int, float> entry in _wardBorn)
                {
                    if (!_liveWards.Contains(entry.Key)) gone.Add(entry.Key);
                }

                for (int i = 0; i < gone.Count; i++)
                {
                    _wardBorn.Remove(gone[i]);
                    _stumbledOnce.Remove(gone[i]);
                }
            }
        }

        // ------------------------------------------------------------------
        // Nerve
        // ------------------------------------------------------------------

        void Stumble(float severity)
        {
            if (!IsOpen) return;

            Nerve = Mathf.Max(0f, Nerve - StumbleCost * (0.6f + Mathf.Clamp01(severity) * 0.6f));
            _hold = StumbleHold;
            _hurry = HurryTime;
            _stumble = 1f;
            _x = Mathf.Clamp(_x - _dir * Recoil, -ReadWidth, ReadWidth);
            _stumbledFlag = true;

            transform.position = new Vector3(_x, 0f, RowZ(_row));

            if (Nerve <= 0f) Close();
        }

        void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;
            Nerve = 0f;
            _closed = ClosedTime;
            _hold = 0f;
            _closedFlag = true;

            if (_lineRoot != null) _lineRoot.gameObject.SetActive(false);
            if (_light != null) _light.enabled = false;
        }

        void Reopen()
        {
            IsOpen = true;
            Nerve = 1f;
            _closed = 0f;
            _rise = 0.35f;
            _openedFlag = true;

            if (_lineRoot != null) _lineRoot.gameObject.SetActive(true);
            if (_light != null)
            {
                _light.enabled = true;
                _light.color = Tint;
            }
        }

        /// <summary>True once, on the frame the reader lost its place.</summary>
        public bool TakeStumbled()
        {
            bool value = _stumbledFlag;
            _stumbledFlag = false;
            return value;
        }

        /// <summary>True once, on the frame the book closed.</summary>
        public bool TakeClosed()
        {
            bool value = _closedFlag;
            _closedFlag = false;
            return value;
        }

        /// <summary>True once, on the frame the book opened again.</summary>
        public bool TakeOpened()
        {
            bool value = _openedFlag;
            _openedFlag = false;
            return value;
        }

        // ------------------------------------------------------------------
        // Presentation
        // ------------------------------------------------------------------

        /// <summary>How much of the reader's light is on the page: arrival, nerve and startle.</summary>
        public float Visibility()
        {
            if (!IsOpen) return 0f;
            float nerve = Mathf.Clamp01(Nerve);
            return Mathf.Clamp01(_rise) * (0.35f + nerve * 0.65f) * (1f - _stumble * 0.45f);
        }

        void Animate(float dt)
        {
            float t = Time.time + _seed;
            float vis = Visibility();
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.2f);

            if (_view != null)
            {
                float facing = _dir > 0f ? 90f : -90f;
                float bow = IsOpen ? 0f : 24f;
                _bow = Mathf.Lerp(_bow, bow, Mathf.Clamp01(dt * 2.5f));

                _view.localRotation = Quaternion.Euler(_bow, facing + Mathf.Sin(t * 1.2f) * 4f, 0f);

                // The arrival, and the return to the top of the leaf: both are the same motion,
                // a hand coming up through the vellum, because that is where the reader lives.
                float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_rise));
                _view.localPosition = new Vector3(0f,
                    Mathf.Lerp(-1.6f, 0f, rise) + Mathf.Sin(t * 1.9f) * 0.02f, 0f);
            }

            if (_lineMaterial != null)
            {
                MaterialFactory.ApplyEmission(_lineMaterial, Tint, (0.10f + 0.09f * pulse) * vis);
            }

            if (_bandMaterial != null)
            {
                MaterialFactory.ApplyEmission(_bandMaterial, Tint, (0.42f + 0.16f * pulse) * vis);
            }

            if (_sightMaterial != null)
            {
                MaterialFactory.ApplyEmission(_sightMaterial, Tint, (0.70f + 0.55f * pulse) * vis);
            }

            if (_robeMaterial != null)
            {
                Color linen = Color.Lerp(Palette.Night, Palette.Lector, IsOpen ? 0.40f : 0.10f);
                MaterialFactory.ApplyEmission(_robeMaterial, linen, (IsOpen ? 0.35f : 0.10f) * (0.45f + vis * 0.55f));
            }

            if (_light != null)
            {
                _light.enabled = IsOpen;
                _light.intensity = (0.75f + 0.45f * pulse) * vis;
            }

            if (_lantern != null)
            {
                float scale = IsOpen ? 1f : 0.72f;
                _lantern.localScale = new Vector3(0.13f * scale, 0.17f * scale, 0.11f * scale);
            }

            if (_foot != null)
            {
                float girth = 0.70f + pulse * 0.05f;
                _foot.localScale = new Vector3(girth, 1f, girth);
            }

            if (_place != null)
            {
                // The leading edge of the line: where the eye is. It does not quite hold
                // still, because a reader's attention never does.
                float lead = _dir >= 0f ? LineHalfLength : -LineHalfLength;
                _place.localPosition = new Vector3(lead, 0.048f, 0f);

                float girth = 0.30f + Mathf.Sin(t * 4.4f) * 0.04f;
                _place.localScale = new Vector3(girth, 1f, girth);
            }
        }

        void OnDestroy()
        {
            if (_lineMaterial != null) Destroy(_lineMaterial);
            if (_bandMaterial != null) Destroy(_bandMaterial);
            if (_sightMaterial != null) Destroy(_sightMaterial);
            if (_robeMaterial != null) Destroy(_robeMaterial);
        }
    }

    /// <summary>
    /// Runs the lector without the arena having to know it exists.
    ///
    /// <para>
    /// The director is additive in the same way the counter-hand, the colophon and the gloss
    /// are: it attaches itself at runtime, hangs the reader off the page transform (so a page
    /// turn does not remove it), reads erasures and seals out of the layer transform, candles
    /// through the arena's public surface, blots through <see cref="Sediment"/>, and the
    /// record of hands through <see cref="ContributorLedger"/>. <c>Arena.cs</c> was not
    /// modified to add any of this.
    /// </para>
    ///
    /// <para>
    /// The reading order is the point. The colophon summons the page's authors <i>forwards</i>,
    /// oldest hand first, one per layer: the page being written over again. The lector reads
    /// them <i>backwards</i>, newest hand first: the page being read. Between them, the ledger
    /// runs in both directions at once, the way a palimpsest actually does.
    /// </para>
    /// </summary>
    public class LectorDirector : MonoBehaviour
    {
        /// <summary>
        /// The reader comes on the fourth layer. A page has to have been written, revised and
        /// argued over before anyone sits down to read it aloud.
        /// </summary>
        public const int FirstLayer = 4;

        /// <summary>Seconds after a page turn before the reader's small dramas are announced.</summary>
        const float SettleTime = 1.4f;

        /// <summary>How long an announcement stays on the screen.</summary>
        const float ToastLife = 3.6f;

        public static LectorDirector Instance { get; private set; }

        /// <summary>The reader currently on the page, if any.</summary>
        public Lector Reader { get; private set; }

        /// <summary>The hand whose words are being read.</summary>
        public Contributor Hand { get { return Reader != null ? Reader.Hand : _ledgerHand; } }

        /// <summary>How many times a reader has been summoned onto this page.</summary>
        public int Summons { get; private set; }

        /// <summary>The last line the page announced about its reading.</summary>
        public string LastAnnouncement { get; private set; }

        Arena _arena;
        SfxBank _sfx;
        CameraRig _rig;

        List<Contributor> _ledger;
        Contributor _ledgerHand;

        Transform _pageRoot;
        Transform _layerRoot;

        int _seenLayer;
        float _gate;
        bool _armed;
        bool _stale;

        Canvas _canvas;
        RectTransform _canvasRect;
        RectTransform _plate;
        Text _plateName;
        Text _plateWords;
        Image _nerveFill;
        Text _toast;
        string _toastLine = string.Empty;
        float _toastLife;
        Camera _view;

        const float NerveWidth = 210f;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            if (Instance != null) return;
            Build();
        }

        public static LectorDirector Build()
        {
            GameObject go = new GameObject("Lector Director");
            return go.AddComponent<LectorDirector>();
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
                if (_armed || Reader != null) Dismiss();
                UpdateOverlay(dt);
                return;
            }

            if (_arena.IsOver)
            {
                _stale = true;
            }
            else if (_stale)
            {
                // A new folio is a new reading: the book is opened again at the record's end.
                _stale = false;
                Dismiss();
            }

            if (!_armed) Arm();
            if (!_armed) return;

            if (_layerRoot == null) _layerRoot = LayerRoot();

            if (_arena.Layer != _seenLayer) OnLayerTurned();

            bool live = !_arena.IsOver && !_arena.IsPaused && !_arena.IsTitle;

            if (Reader != null)
            {
                Reader.Tick(live ? dt : 0f, _arena, _layerRoot);
                if (live) HandleEvents();
            }
            else if (live && _arena.Layer >= FirstLayer && _gate <= 0f)
            {
                Summon();
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
            _ledger = ContributorLedger.Load();

            Transform page = PageRoot();
            if (page == null) return;

            _pageRoot = page;
            _layerRoot = LayerRoot();
            _seenLayer = _arena.Layer;
            _gate = SettleTime;
            _armed = true;

            Debug.Log("[Palimpsest] The lector is bound. " + (_ledger != null ? _ledger.Count : 0)
                + " hand(s) on record; it will read them backwards from layer " + Hud.Roman(FirstLayer) + ".");
        }

        void Dismiss()
        {
            if (Reader != null) Destroy(Reader.gameObject);
            Reader = null;
            _ledgerHand = null;

            if (_plate != null) _plate.gameObject.SetActive(false);

            _armed = false;
            _seenLayer = 0;
            _toastLife = 0f;
            _toastLine = string.Empty;
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
        // The record, read backwards
        // ------------------------------------------------------------------

        /// <summary>
        /// Which hand the reader holds on a given layer. The newest entry on the record is read
        /// first and the reading walks back towards the oldest writing, one hand per layer, so
        /// descending the folio is descending the archive. Once the record runs out it starts
        /// again at the newest hand: the page does not invent a reader it never had.
        /// </summary>
        public Contributor HandForLayer(int layer)
        {
            if (_ledger == null || _ledger.Count == 0)
            {
                _ledger = ContributorLedger.Load();
                if (_ledger == null || _ledger.Count == 0) return null;
            }

            int offset = Mathf.Max(0, layer - FirstLayer);
            int index = (int)(((long)_ledger.Count - 1 - offset) % _ledger.Count);
            if (index < 0) index += _ledger.Count;

            return _ledger[index];
        }

        void Summon()
        {
            if (_pageRoot == null) return;

            Contributor hand = HandForLayer(_arena.Layer);
            if (hand == null) return;

            Reader = Lector.Create(_pageRoot, hand);
            _ledgerHand = hand;
            Summons++;

            Announce("A READER COMES TO THE PAGE");
            if (_sfx != null) _sfx.Play(Sfx.Start, 0.55f, 1.35f);
            if (_rig != null) _rig.Shake(0.05f);

            Debug.Log("[Palimpsest] The lector opens the record at the newest hand and reads backwards: "
                + hand.ShortName + ".");
        }

        void OnLayerTurned()
        {
            _seenLayer = _arena.Layer;
            _gate = SettleTime;

            if (_arena.Layer < FirstLayer)
            {
                if (Reader != null) Destroy(Reader.gameObject);
                Reader = null;
                _ledgerHand = null;
                return;
            }

            if (Reader == null)
            {
                Summon();
                return;
            }

            Contributor hand = HandForLayer(_arena.Layer);
            if (hand == null) return;

            Reader.SetHand(hand, false);
            _ledgerHand = hand;

            Announce("THE READER TURNS TO ANOTHER HAND");
        }

        // ------------------------------------------------------------------
        // Reader events
        // ------------------------------------------------------------------

        void HandleEvents()
        {
            Lector reader = Reader;
            if (reader == null) return;

            if (reader.TakeStumbled() && _gate <= 0f)
            {
                Announce("THE READER LOSES THE PLACE");
                if (_sfx != null) _sfx.Play(Sfx.Denied, 0.45f, 1.25f);
                if (_rig != null) _rig.Shake(0.03f);
            }

            if (reader.TakeClosed())
            {
                string fragment = FragmentOf(reader.Hand != null ? reader.Hand.Action : null, 7);
                Announce(string.IsNullOrEmpty(fragment)
                    ? "THE READER CLOSES THE BOOK"
                    : "THE READER CLOSES THE BOOK\n... " + fragment);
                if (_sfx != null) _sfx.Play(Sfx.Seal, 0.5f, 0.78f);
                if (_rig != null) _rig.Shake(0.04f);
            }

            if (reader.TakeOpened())
            {
                Announce("THE READER OPENS THE BOOK AGAIN");
                if (_sfx != null) _sfx.Play(Sfx.Start, 0.4f, 1.55f);
            }
        }

        void Announce(string line)
        {
            _toastLine = line != null ? line : string.Empty;
            _toastLife = ToastLife;
            LastAnnouncement = _toastLine;
        }

        /// <summary>
        /// A few words out of a hand's action line, so the page is heard reading an actual
        /// sentence off the record rather than only naming its author.
        /// </summary>
        public static string FragmentOf(string action, int words)
        {
            if (string.IsNullOrEmpty(action) || words <= 0) return string.Empty;

            string[] parts = action.Split(
                new char[] { ' ', '\t', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0) return string.Empty;
            if (parts.Length <= words) return action.Trim();

            int start = Random.Range(0, parts.Length - words + 1);

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = start; i < start + words && i < parts.Length; i++)
            {
                if (builder.Length > 0) builder.Append(' ');
                builder.Append(parts[i]);
            }

            return builder.ToString().TrimEnd(',', ';', ':', '.') + "...";
        }

        // ------------------------------------------------------------------
        // Overlay
        // ------------------------------------------------------------------

        void BuildOverlay()
        {
            GameObject root = new GameObject("Lector Overlay");
            root.transform.SetParent(transform, false);

            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 300;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRect = root.GetComponent<RectTransform>();
            Font serif = ResolveSerif();

            // Announcements, under the arena's own toast and the colophon's.
            _toast = MakeText(_canvasRect, "Lector Toast", serif, 24, FontStyle.Normal,
                TextAnchor.UpperCenter, Palette.Reading,
                new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(1500f, 86f));

            // The nameplate, tracked over the reader's head.
            GameObject plateObject = new GameObject("Lector Plate");
            plateObject.transform.SetParent(_canvasRect, false);

            _plate = plateObject.AddComponent<RectTransform>();
            _plate.anchorMin = new Vector2(0.5f, 0.5f);
            _plate.anchorMax = new Vector2(0.5f, 0.5f);
            _plate.pivot = new Vector2(0.5f, 0.5f);
            _plate.sizeDelta = Vector2.zero;

            _plateName = MakeText(_plate, "Name", serif, 18, FontStyle.Bold, TextAnchor.MiddleCenter,
                Palette.Reading, new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(1300f, 26f));

            _plateWords = MakeText(_plate, "Words", serif, 14, FontStyle.Italic, TextAnchor.MiddleCenter,
                Palette.Muted, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1300f, 22f));

            // The nerve: a thin bar, because a reader's attention is a resource like any other.
            GameObject bar = new GameObject("Nerve");
            bar.transform.SetParent(_plate, false);

            Image frame = bar.AddComponent<Image>();
            frame.color = Palette.Alpha(Palette.Undertext, 0.85f);
            frame.raycastTarget = false;

            RectTransform frameRect = bar.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.5f, 0f);
            frameRect.anchorMax = new Vector2(0.5f, 0f);
            frameRect.pivot = new Vector2(0.5f, 0f);
            frameRect.anchoredPosition = new Vector2(0f, 44f);
            frameRect.sizeDelta = new Vector2(NerveWidth + 4f, 6f);

            GameObject fill = new GameObject("Nerve Fill");
            fill.transform.SetParent(bar.transform, false);

            _nerveFill = fill.AddComponent<Image>();
            _nerveFill.color = Palette.Reading;
            _nerveFill.raycastTarget = false;

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(2f, 0f);
            fillRect.sizeDelta = new Vector2(NerveWidth, 0f);
            fillRect.offsetMin = new Vector2(2f, 1f);
            fillRect.offsetMax = new Vector2(-2f, -1f);

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

            Lector reader = Reader;
            bool show = !_arena.IsTitle && (reader != null || _toastLife > 0f);
            if (_canvas.gameObject.activeSelf != show) _canvas.gameObject.SetActive(show);
            if (!show) return;

            _toast.text = _toastLine;
            _toast.color = Palette.Alpha(Palette.Reading, Mathf.Clamp01(_toastLife / 1.2f));

            if (_plate == null || reader == null)
            {
                if (_plate != null && _plate.gameObject.activeSelf) _plate.gameObject.SetActive(false);
                return;
            }

            Camera camera = View;
            bool visible = !_arena.IsOver && camera != null;

            Vector2 local = Vector2.zero;
            if (visible)
            {
                Vector3 screen = camera.WorldToScreenPoint(reader.transform.position + Vector3.up * 1.9f);
                if (screen.z <= 0f)
                {
                    visible = false;
                }
                else
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _canvasRect, screen, null, out local);
                }
            }

            if (_plate.gameObject.activeSelf != visible) _plate.gameObject.SetActive(visible);
            if (!visible) return;

            _plate.anchoredPosition = local;

            if (reader.IsOpen)
            {
                _plateName.text = "LECTOR   READING  " + reader.Name;
                _plateName.color = reader.Hue;
                _plateWords.text = WordsOf(reader);
            }
            else
            {
                _plateName.text = "THE BOOK IS CLOSED   " + Mathf.CeilToInt(reader.ClosedRemaining);
                _plateName.color = Palette.Alpha(Palette.Muted, 0.85f);
                _plateWords.text = "the reader has nothing left to read by";
            }

            if (_nerveFill != null)
            {
                RectTransform rect = _nerveFill.rectTransform;
                rect.sizeDelta = new Vector2(NerveWidth * Mathf.Clamp01(reader.Nerve), rect.sizeDelta.y);

                _nerveFill.color = Color.Lerp(Palette.Alert, reader.Hue,
                    Mathf.InverseLerp(0.10f, 0.75f, reader.Nerve));
            }
        }

        /// <summary>The words being read, quoted out of the hand's own action line.</summary>
        static string WordsOf(Lector reader)
        {
            if (reader.Hand == null) return string.Empty;

            string action = reader.Hand.Action;
            if (string.IsNullOrEmpty(action)) return string.Empty;

            string line = action.Trim();
            if (line.Length > 92) line = line.Substring(0, 91).TrimEnd() + "...";

            return "\"" + line + "\"";
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
