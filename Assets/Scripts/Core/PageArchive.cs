using System.Collections.Generic;
using UnityEngine;

namespace Palimpsest
{
    /// <summary>One mark, flattened for storage. Field names are kept terse because
    /// they are the on-disk schema, not prose.</summary>
    [System.Serializable]
    public class StratumRecord
    {
        public float x;
        public float z;
        public float strength;
        public float charge;
        public int kind;
        public int age;
        public int gushes;
    }

    /// <summary>
    /// Everything the page remembers between sessions: its marks, how deep anyone has
    /// ever reached, and the best score left on it.
    /// </summary>
    [System.Serializable]
    public class ArchiveData
    {
        public int version = 1;
        public int runs;
        public int bestLayer;
        public int bestScore;
        public List<StratumRecord> strata = new List<StratumRecord>();
    }

    /// <summary>
    /// The archive is the part of the palimpsest that outlives a single run.
    ///
    /// A real palimpsest is not a fresh page each morning: it is one sheet scraped and
    /// reused until the older writing shows through the new. Persisting the sediment
    /// through <see cref="PlayerPrefs"/> makes that literal. Die on layer five and the
    /// next folio still has your graves on it - unless you hold Backspace on the title
    /// card and burn the archive.
    ///
    /// The format is versioned JSON so a future hand can migrate or discard it without
    /// guessing, and every read is defensive: a corrupt archive must never stop the
    /// game from opening.
    /// </summary>
    public static class PageArchive
    {
        const string Key = "palimpsest.archive.v1";

        public static bool Exists { get { return PlayerPrefs.HasKey(Key); } }

        /// <summary>Loads the archive, returning an empty one if there is nothing usable.</summary>
        public static ArchiveData Load()
        {
            ArchiveData data = null;

            if (PlayerPrefs.HasKey(Key))
            {
                string json = PlayerPrefs.GetString(Key, string.Empty);

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        data = JsonUtility.FromJson<ArchiveData>(json);
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogWarning("[Palimpsest] The archive could not be read and was ignored: "
                            + exception.Message);
                        data = null;
                    }
                }
            }

            if (data == null) data = new ArchiveData();
            if (data.strata == null) data.strata = new List<StratumRecord>();
            if (data.version <= 0) data.version = 1;

            return data;
        }

        public static void Save(ArchiveData data)
        {
            if (data == null) return;

            try
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
                PlayerPrefs.Save();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[Palimpsest] The archive could not be written: " + exception.Message);
            }
        }

        /// <summary>Scrapes the whole page clean, across runs. Irreversible.</summary>
        public static void Burn()
        {
            if (!PlayerPrefs.HasKey(Key)) return;

            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            Debug.Log("[Palimpsest] The archive is burned. The page is blank again.");
        }

        /// <summary>
        /// Builds the next archive state from a live sediment, carrying forward the
        /// records that only get better (deepest layer, highest score, run count).
        /// </summary>
        public static ArchiveData Capture(Sediment sediment, int layer, int score, ArchiveData previous)
        {
            ArchiveData data = previous != null ? previous : new ArchiveData();

            data.version = 1;
            data.bestLayer = Mathf.Max(data.bestLayer, layer);
            data.bestScore = Mathf.Max(data.bestScore, score);

            data.strata = new List<StratumRecord>();

            if (sediment == null) return data;

            List<Stratum> all = sediment.All;
            int limit = Mathf.Min(all.Count, Sediment.Capacity);

            for (int i = 0; i < limit; i++)
            {
                Stratum stratum = all[i];
                if (stratum == null) continue;

                Vector3 position = stratum.transform.position;

                data.strata.Add(new StratumRecord
                {
                    x = position.x,
                    z = position.z,
                    strength = stratum.Strength,
                    charge = stratum.Charge,
                    kind = (int)stratum.Kind,
                    age = stratum.Age,
                    gushes = stratum.Gushes
                });
            }

            return data;
        }
    }
}
