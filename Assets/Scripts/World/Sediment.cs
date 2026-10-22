using System.Collections.Generic;
using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// The sediment: every mark the page has accumulated, and the arithmetic that
    /// turns those marks into pressure.
    ///
    /// <para>
    /// This is the piece the earlier layers of the repository were missing. The page
    /// used to be cleared at the end of every layer, which meant the palimpsest was
    /// a name rather than a mechanic. Sediment is built under the page instead of
    /// under the layer, so it survives layer turns, deaths and - through
    /// <see cref="PageArchive"/> - whole play sessions.
    /// </para>
    ///
    /// <para>
    /// It owns five responsibilities: placing marks, ageing them when a page turns,
    /// fermenting stains into blooms, answering queries about what lies under a point
    /// (wards, ink, light and scrape all consult it), and keeping the whole record
    /// inside a fixed budget so a long-lived save cannot grow without bound.
    /// </para>
    /// </summary>
    public class Sediment : MonoBehaviour
    {
        /// <summary>Upper bound on marks. Older, weaker marks are evicted past this.</summary>
        public const int Capacity = 190;

        /// <summary>Minimum seconds between two blooms erupting anywhere on the page.</summary>
        const float EruptionGateLength = 3.0f;

        /// <summary>Stain count at which silt alone accounts for most of the pressure.</summary>
        const float StainSaturation = 80f;

        /// <summary>Bloom count that saturates the pressure contribution of blooms.</summary>
        const float BloomSaturation = 5f;

        /// <summary>Two blots closer than this are merged rather than stacked.</summary>
        const float MergeDistance = 1.1f;

        public static Sediment Instance { get; private set; }

        readonly List<Stratum> _strata = new List<Stratum>();
        Transform _root;

        /// <summary>
        /// The page's total load, 0 (clean vellum) to 1 (nothing left but silt).
        ///
        /// "Cumulative" is the exact word, not a decoration: this number is not a
        /// per-layer difficulty setting that some designer chose, it is the running
        /// sum of everything that has already happened to the page - every erasure
        /// un-written, every seal abandoned, every generation of overwriting - folded
        /// into a single scalar that the spawner, the lighting and the interface all
        /// read from. It is deliberately smoothed rather than recomputed, because the
        /// page should gain weight when a bloom opens, not flicker.
        /// </summary>
        float _cumulativeSilt;

        float _eruptionGate;
        int _layer = 1;

        // ------------------------------------------------------------------
        // Read-only surface
        // ------------------------------------------------------------------

        public float Pressure { get { return _cumulativeSilt; } }
        public int Count { get { return _strata.Count; } }

        public int StainCount { get { return CountKind(StratumKind.Stain); } }
        public int GhostCount { get { return CountKind(StratumKind.Ghost); } }

        public int BloomCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _strata.Count; i++)
                {
                    if (_strata[i] != null && _strata[i].IsBloom) count++;
                }
                return count;
            }
        }

        /// <summary>The raw record, used by the archive when a page is saved.</summary>
        public List<Stratum> All { get { return _strata; } }

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public static Sediment Build(Transform pageRoot)
        {
            // Idempotent: a second arena in the same session must never create a second
            // sediment, or the page would have two conflicting memories.
            if (Instance != null) return Instance;

            GameObject root = new GameObject("Sediment");
            if (pageRoot != null) root.transform.SetParent(pageRoot, false);
            return root.AddComponent<Sediment>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _root = transform;

            /*
             * A note from the hand that laid this sediment down.
             * Syllables: 5 - 7 - 5.
             */
            Debug.Log("Old drafts fall behind / I keep only what still serves / the rest becomes silt");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        int CountKind(StratumKind kind)
        {
            int count = 0;
            for (int i = 0; i < _strata.Count; i++)
            {
                if (_strata[i] != null && _strata[i].Kind == kind) count++;
            }
            return count;
        }

        // ------------------------------------------------------------------
        // Placing marks
        // ------------------------------------------------------------------

        public Stratum Place(StratumKind kind, Vector3 position, float strength,
            int age, int gushes, float charge)
        {
            Stratum stratum = Stratum.Create(_root, kind, position, strength, age, gushes, charge);
            _strata.Add(stratum);
            Trim();
            return stratum;
        }

        /// <summary>Leaves a blot where an erasure was un-written.</summary>
        public Stratum Stain(Vector3 position, float strength)
        {
            Stratum existing = NearestKind(position, MergeDistance, StratumKind.Stain);
            if (existing != null)
            {
                // Two deaths in almost the same place make one darker grave.
                existing.Strengthen(0.24f);
                return existing;
            }

            return Place(StratumKind.Stain, position, strength, 0, 0, 0f);
        }

        /// <summary>Leaves the ring of a seal that outlived its layer.</summary>
        public Stratum Ghost(Vector3 position, float strength)
        {
            Stratum existing = NearestKind(position, MergeDistance * 0.7f, StratumKind.Ghost);
            if (existing != null)
            {
                existing.AdvanceGeneration();
                return existing;
            }

            return Place(StratumKind.Ghost, position, strength, 1, 0, 0f);
        }

        // ------------------------------------------------------------------
        // Queries
        // ------------------------------------------------------------------

        /// <summary>Nearest mark of any kind; used by the quill before it sets a seal.</summary>
        public Stratum Nearest(Vector3 from, float maxDistance)
        {
            Stratum best = null;
            float bestSqr = maxDistance * maxDistance;

            for (int i = 0; i < _strata.Count; i++)
            {
                Stratum stratum = _strata[i];
                if (stratum == null) continue;

                float sqr = (stratum.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = stratum;
                }
            }
            return best;
        }

        Stratum NearestKind(Vector3 from, float maxDistance, StratumKind kind)
        {
            Stratum best = null;
            float bestSqr = maxDistance * maxDistance;

            for (int i = 0; i < _strata.Count; i++)
            {
                Stratum stratum = _strata[i];
                if (stratum == null || stratum.Kind != kind) continue;

                float sqr = (stratum.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = stratum;
                }
            }
            return best;
        }

        // ------------------------------------------------------------------
        // Interactions
        // ------------------------------------------------------------------

        /// <summary>
        /// Called when the quill sets a ward onto a mark. Returns the resilience the new
        /// ward inherits. A stain is cauterised and kept as a ghost; a ghost is consumed
        /// outright, because the ward now occupies that ring.
        /// </summary>
        public float Settle(Stratum stratum)
        {
            if (stratum == null) return 0f;

            float bonus = stratum.ResilienceBonus;

            if (stratum.Kind == StratumKind.Stain)
            {
                stratum.Cauterise();
            }
            else
            {
                _strata.Remove(stratum);
                Destroy(stratum.gameObject);
            }

            return bonus;
        }

        /// <summary>
        /// Illuminate: scours stains inside the radius back toward clean vellum. Marks
        /// reduced past their faint threshold become ghosts, so light never destroys
        /// the page's memory, it only calms it. Returns the number of stains touched.
        /// </summary>
        public int Scour(Vector3 center, float radius, float amount)
        {
            int calmed = 0;
            float radiusSqr = radius * radius;

            for (int i = 0; i < _strata.Count; i++)
            {
                Stratum stratum = _strata[i];
                if (stratum == null || stratum.Kind != StratumKind.Stain) continue;

                Vector3 delta = stratum.transform.position - center;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr > radiusSqr) continue;

                float falloff = 1f - Mathf.Clamp01(Mathf.Sqrt(sqr) / radius) * 0.55f;
                stratum.Sour(amount * falloff);
                calmed++;
            }

            return calmed;
        }

        /// <summary>
        /// Scrape: takes residue off the page entirely, ghosts included. The verb is
        /// named for it - scraping is how you buy the page back, and it is always paid
        /// for with seals. Returns the number of marks removed.
        /// </summary>
        public int Scrub(Vector3 center, float radius)
        {
            int removed = 0;
            float radiusSqr = radius * radius;

            for (int i = _strata.Count - 1; i >= 0; i--)
            {
                Stratum stratum = _strata[i];
                if (stratum == null)
                {
                    _strata.RemoveAt(i);
                    continue;
                }

                Vector3 delta = stratum.transform.position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > radiusSqr) continue;

                _strata.RemoveAt(i);
                Destroy(stratum.gameObject);
                removed++;
            }

            return removed;
        }

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------

        public void Tick(float dt, int layer)
        {
            _layer = layer;
            _eruptionGate = Mathf.Max(0f, _eruptionGate - dt);

            int stains = 0;
            int blooms = 0;

            for (int i = _strata.Count - 1; i >= 0; i--)
            {
                Stratum stratum = _strata[i];
                if (stratum == null)
                {
                    _strata.RemoveAt(i);
                    continue;
                }

                stratum.Tick(dt, _layer);

                if (stratum.Kind == StratumKind.Stain)
                {
                    stains++;
                    if (stratum.IsBloom) blooms++;
                }
            }

            float raw = Mathf.Clamp01(stains / StainSaturation) * 0.55f
                      + Mathf.Clamp01(blooms / BloomSaturation) * 0.45f;
            _cumulativeSilt = Mathf.Lerp(_cumulativeSilt, Mathf.Clamp01(raw),
                1f - Mathf.Exp(-0.9f * dt));

            Trim();
        }

        /// <summary>
        /// Picks the ripest bloom that is ready and vents it. The arena turns the
        /// resulting position into a real erasure, so the spawner is not inventing
        /// pressure out of nowhere - the page's own history supplies it.
        /// </summary>
        public bool TryConsumeEruption(out Vector3 position)
        {
            position = Vector3.zero;
            if (_eruptionGate > 0f) return false;

            Stratum best = null;
            float bestScore = -1f;

            for (int i = 0; i < _strata.Count; i++)
            {
                Stratum stratum = _strata[i];
                if (stratum == null || !stratum.CanErupt) continue;

                float score = stratum.Strength * 2f + stratum.Charge + stratum.Age * 0.05f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = stratum;
                }
            }

            if (best == null) return false;

            position = best.transform.position;
            best.ConsumeEruption();
            _eruptionGate = EruptionGateLength;
            return true;
        }

        // ------------------------------------------------------------------
        // Page turns, deaths and washing
        // ------------------------------------------------------------------

        /// <summary>
        /// Ages every mark by one generation and lays the seal-rings of the layer that
        /// was just completed into the sediment.
        /// </summary>
        public void OnLayerTurned(List<Vector3> foundations)
        {
            for (int i = _strata.Count - 1; i >= 0; i--)
            {
                Stratum stratum = _strata[i];
                if (stratum == null)
                {
                    _strata.RemoveAt(i);
                    continue;
                }

                stratum.AdvanceGeneration();
            }

            if (foundations != null)
            {
                for (int i = 0; i < foundations.Count; i++)
                {
                    Ghost(foundations[i], 0.5f);
                }
            }

            Trim();
        }

        /// <summary>
        /// A new run lays a new folio over the old one. The scrape takes the weakest
        /// marks outright and thins the rest; only what the page genuinely remembers
        /// is still legible after a death.
        /// </summary>
        public void Wash(float severity)
        {
            for (int i = _strata.Count - 1; i >= 0; i--)
            {
                Stratum stratum = _strata[i];
                if (stratum == null)
                {
                    _strata.RemoveAt(i);
                    continue;
                }

                stratum.AdvanceGeneration();
                stratum.Thin(severity);

                bool washedAway = (stratum.Kind == StratumKind.Stain && stratum.Strength <= 0.18f)
                               || (stratum.Kind == StratumKind.Ghost && stratum.Age > 5);

                if (washedAway)
                {
                    _strata.RemoveAt(i);
                    Destroy(stratum.gameObject);
                }
            }

            _cumulativeSilt = 0f;
            Trim();
        }

        /// <summary>Burns everything back to blank vellum. Only the archive menu does this.</summary>
        public void Reset()
        {
            for (int i = 0; i < _strata.Count; i++)
            {
                if (_strata[i] != null) Destroy(_strata[i].gameObject);
            }

            _strata.Clear();
            _cumulativeSilt = 0f;
            _eruptionGate = 0f;
        }

        /// <summary>Rehydrates a saved page.</summary>
        public void Restore(ArchiveData data)
        {
            if (data == null || data.strata == null) return;

            float limit = Arena.PageRadius - 0.4f;

            for (int i = 0; i < data.strata.Count; i++)
            {
                StratumRecord record = data.strata[i];
                if (record == null) continue;

                Vector2 flat = new Vector2(record.x, record.z);
                if (flat.sqrMagnitude > limit * limit) continue;

                StratumKind kind = record.kind == (int)StratumKind.Stain
                    ? StratumKind.Stain
                    : StratumKind.Ghost;

                Place(kind, new Vector3(record.x, 0f, record.z),
                    record.strength, record.age, record.gushes, record.charge);
            }

            // A restored page must not arrive already seething.
            _cumulativeSilt = Mathf.Clamp01(_strata.Count / (float)Capacity) * 0.5f;
        }

        // ------------------------------------------------------------------
        // Capacity
        // ------------------------------------------------------------------

        /// <summary>
        /// Keeps the record inside its budget by dropping the least significant mark
        /// first: spent and faint stains go before blooms, and blooms before ghosts.
        /// The page has to forget something eventually, and it should forget what it
        /// can most easily spare.
        /// </summary>
        void Trim()
        {
            while (_strata.Count > Capacity)
            {
                int victim = -1;
                float worstScore = float.MaxValue;

                for (int i = 0; i < _strata.Count; i++)
                {
                    Stratum stratum = _strata[i];

                    if (stratum == null)
                    {
                        victim = i;
                        break;
                    }

                    if (stratum.IsBloom) continue;

                    float score = stratum.Strength
                                + (stratum.Kind == StratumKind.Ghost ? 0.4f : 0f)
                                + (stratum.IsSpent ? -0.6f : 0f)
                                + stratum.Age * 0.02f;

                    if (score < worstScore)
                    {
                        worstScore = score;
                        victim = i;
                    }
                }

                if (victim < 0) victim = 0;

                Stratum removed = _strata[victim];
                _strata.RemoveAt(victim);
                if (removed != null) Destroy(removed.gameObject);
            }
        }
    }
}
