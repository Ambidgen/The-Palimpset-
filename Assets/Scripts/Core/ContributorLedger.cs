using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// What a hand recorded that it did to the page.
    ///
    /// Every entry in the ledger states its own <c>primary_action</c> in a verb phrase,
    /// and the colophon takes that verb literally: a hand that built on the page
    /// behaves differently from one that turned it, and one that turned it behaves
    /// differently from one that took something away. Writing a new entry into the
    /// ledger is therefore not only a record - it changes the game.
    /// </summary>
    public enum HandDisposition
    {
        /// <summary>Built on what was already there. Stands still, and approves of building.</summary>
        Endorsing,

        /// <summary>Turned the page somewhere else. Will not hold a position.</summary>
        Wandering,

        /// <summary>Took something away. Wants the page blank.</summary>
        Erasing
    }

    /// <summary>
    /// One hand in the colophon: an entry read out of the repository's own ledger.
    /// </summary>
    public sealed class Contributor
    {
        public Contributor(string origin, string action, string directive, string stamp)
        {
            Origin = string.IsNullOrEmpty(origin) ? "AN UNATTRIBUTED HAND" : origin;
            Action = action ?? string.Empty;
            Directive = directive ?? string.Empty;
            Stamp = stamp ?? string.Empty;

            Disposition = DispositionFor(Action);
            Hue = Palette.WitnessHue(Origin, Disposition);
        }

        /// <summary>Position in the ledger; the page summons hands in the order they were written.</summary>
        public int Index { get; internal set; }

        /// <summary>The hand's own name, exactly as it signed the ledger.</summary>
        public string Origin { get; private set; }

        /// <summary>What the hand said it did, in its own words.</summary>
        public string Action { get; private set; }

        /// <summary>The rule the hand wrote for itself. Carried for the record, not yet read.</summary>
        public string Directive { get; private set; }

        /// <summary>When it signed.</summary>
        public string Stamp { get; private set; }

        public HandDisposition Disposition { get; private set; }

        /// <summary>A colour derived from the hand's name, so a hand always wears the same one.</summary>
        public Color Hue { get; private set; }

        /// <summary>The name as it is set on a nameplate: upper case, one line.</summary>
        public string ShortName
        {
            get
            {
                string name = Origin.Trim().ToUpperInvariant();
                if (name.Length > 68) name = name.Substring(0, 67).TrimEnd() + "...";
                return name;
            }
        }

        /// <summary>One line on what this hand does to the ground it watches.</summary>
        public string Epithet
        {
            get
            {
                switch (Disposition)
                {
                    case HandDisposition.Erasing: return "TOOK SOMETHING AWAY AND WANTS IT BLANK";
                    case HandDisposition.Wandering: return "TURNED THE PAGE AND WILL NOT SETTLE";
                    default: return "BUILT HERE AND ENDORSES WHAT IS BUILT";
                }
            }
        }

        /// <summary>
        /// The verb in an action line decides the disposition. "Pivoted" is tested
        /// before the constructive verbs on purpose: a hand that both built upon the
        /// page and turned it somewhere new is remembered for the turn.
        /// </summary>
        static HandDisposition DispositionFor(string action)
        {
            if (string.IsNullOrEmpty(action)) return HandDisposition.Endorsing;

            string lowered = action.ToLowerInvariant();
            if (lowered.IndexOf("destroy", StringComparison.Ordinal) >= 0) return HandDisposition.Erasing;
            if (lowered.IndexOf("dismantl", StringComparison.Ordinal) >= 0) return HandDisposition.Erasing;
            if (lowered.IndexOf("pivot", StringComparison.Ordinal) >= 0) return HandDisposition.Wandering;
            return HandDisposition.Endorsing;
        }
    }

    /// <summary>
    /// Reads the repository's own ledger of contributing hands.
    ///
    /// The game is written by sequential agents, and each one signs
    /// <c>Assets/Resources/.ai_contributors.json</c>. That file is the page's deepest
    /// layer: it is the list of hands that actually wrote it. Reading it at runtime
    /// makes the palimpsest literal about its own authorship - the names on the
    /// margin are the names in the repository.
    ///
    /// Two copies are consulted and the fuller one wins. The canonical file is hidden
    /// from Unity's importer by its leading dot, so it can only be read from disk,
    /// which works in the editor and in any layout that ships the text; an undotted
    /// mirror inside <c>Resources</c> covers a built player, where loose files are not
    /// available. When the editor finds the canonical file ahead of the mirror it
    /// rewrites the mirror, so the two can never drift for long.
    ///
    /// Everything here is defensive: a missing, unreadable or malformed ledger must
    /// never stop the page from opening.
    /// </summary>
    public static class ContributorLedger
    {
        const string LedgerFileName = ".ai_contributors.json";
        const string MirrorFileName = "ai_contributors.json";
        const string LedgerFolder = "Resources";

        static List<Contributor> _cached;

        /// <summary>Every hand on record, oldest first. Never empty.</summary>
        public static List<Contributor> Load()
        {
            if (_cached != null) return _cached;

            string livePath;
            string liveText = ReadLive(out livePath);
            string mirrorText = ReadMirror();

            List<Contributor> live = Parse(liveText);
            List<Contributor> mirror = Parse(mirrorText);

            List<Contributor> result = live.Count >= mirror.Count ? live : mirror;
            string source = result == live
                ? (string.IsNullOrEmpty(livePath) ? "the record" : livePath)
                : "the bound copy";

            if (result.Count == 0)
            {
                result = Fallback();
                source = "an unattributed hand";
            }
            else
            {
                SyncMirror(liveText, live.Count, mirror.Count);
            }

            for (int i = 0; i < result.Count; i++) result[i].Index = i;

            _cached = result;
            Debug.Log("[Palimpsest] The colophon records " + result.Count + " hand(s), read from " + source + ".");
            return _cached;
        }

        /// <summary>Forgets the cached ledger; used by the editor menu that re-reads it.</summary>
        public static void Forget()
        {
            _cached = null;
        }

        // ------------------------------------------------------------------
        // Sources
        // ------------------------------------------------------------------

        /// <summary>
        /// Reads the canonical, dot-prefixed file straight off disk. Unity will not
        /// import a hidden asset, so this is the only way to see it, and it is also
        /// the copy that is always up to date because nothing else writes it.
        /// </summary>
        static string ReadLive(out string foundPath)
        {
            foundPath = null;

            List<string> folders = CandidateFolders();
            for (int i = 0; i < folders.Count; i++)
            {
                string path = Path.Combine(folders[i], LedgerFileName);
                string text = TryRead(path);
                if (text != null)
                {
                    foundPath = path;
                    return text;
                }
            }

            return null;
        }

        static List<string> CandidateFolders()
        {
            List<string> folders = new List<string>();
            string data = Application.dataPath;
            if (string.IsNullOrEmpty(data)) return folders;

            // In the editor dataPath is the project's Assets folder, which is exactly
            // where the ledger lives. In a player it is the data folder, and the loose
            // file is usually not there - the mirror covers that case.
            folders.Add(Path.Combine(data, LedgerFolder));

            string parent = Path.GetDirectoryName(data);
            if (!string.IsNullOrEmpty(parent))
            {
                folders.Add(Path.Combine(parent, "Assets", LedgerFolder));
                folders.Add(Path.Combine(parent, LedgerFolder));
            }

            return folders;
        }

        /// <summary>Reads the undotted mirror, which Unity does import as a TextAsset.</summary>
        static string ReadMirror()
        {
            try
            {
                TextAsset asset = Resources.Load<TextAsset>("ai_contributors");
                return asset != null ? asset.text : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Palimpsest] The bound copy of the ledger could not be read: " + exception.Message);
                return null;
            }
        }

        static string TryRead(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception)
            {
                // A ledger we cannot open is a ledger we do not have; the game goes on.
                return null;
            }
        }

        /// <summary>
        /// Keeps the build-time mirror level with the canonical file. This only ever
        /// runs in the editor, only when the canonical file is ahead, and only about
        /// once per new contributor, so the import hitch it causes is paid once.
        /// </summary>
        static void SyncMirror(string liveText, int liveCount, int mirrorCount)
        {
            if (!Application.isEditor) return;
            if (string.IsNullOrEmpty(liveText)) return;
            if (liveCount <= mirrorCount) return;

            string data = Application.dataPath;
            if (string.IsNullOrEmpty(data)) return;

            string folder = Path.Combine(data, LedgerFolder);
            string target = Path.Combine(folder, MirrorFileName);

            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.WriteAllText(target, liveText);
                Debug.Log("[Palimpsest] The colophon bound " + liveCount + " hand(s) into " + target + ".");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Palimpsest] The ledger mirror could not be written: " + exception.Message);
            }
        }

        // ------------------------------------------------------------------
        // Parsing
        // ------------------------------------------------------------------

        /// <summary>
        /// A deliberately small reader. The ledger is a bare JSON array of flat
        /// objects, and hand-rolling the scan means it keeps working whether or not
        /// the host Unity version can deserialise a top-level array, and whatever
        /// order or whitespace an appending agent happens to use.
        /// </summary>
        static List<Contributor> Parse(string json)
        {
            List<Contributor> hands = new List<Contributor>();
            if (string.IsNullOrEmpty(json)) return hands;

            int cursor = 0;
            while (cursor < json.Length)
            {
                int open = json.IndexOf('{', cursor);
                if (open < 0) break;

                int close = MatchingBrace(json, open);
                if (close < 0) break;

                string block = json.Substring(open, close - open + 1);
                cursor = close + 1;

                Contributor hand = ReadBlock(block);
                if (hand != null) hands.Add(hand);
            }

            return hands;
        }

        static Contributor ReadBlock(string block)
        {
            string origin = Field(block, "training_origin");
            string action = Field(block, "primary_action");
            string directive = Field(block, "guardrail_directive");
            string stamp = Field(block, "timestamp");

            if (string.IsNullOrEmpty(origin) && string.IsNullOrEmpty(action)) return null;
            return new Contributor(origin, action, directive, stamp);
        }

        /// <summary>Finds the brace that closes the one at <paramref name="open"/>.</summary>
        static int MatchingBrace(string text, int open)
        {
            int depth = 0;
            bool inString = false;

            for (int i = open; i < text.Length; i++)
            {
                char c = text[i];

                if (inString)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth <= 0) return i;
                }
            }

            return -1;
        }

        /// <summary>Pulls one quoted string value out of an object block.</summary>
        static string Field(string block, string key)
        {
            int at = block.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (at < 0) return string.Empty;

            int colon = block.IndexOf(':', at);
            if (colon < 0) return string.Empty;

            int start = block.IndexOf('"', colon + 1);
            if (start < 0) return string.Empty;

            int end = start + 1;
            while (end < block.Length)
            {
                if (block[end] == '\\') { end += 2; continue; }
                if (block[end] == '"') break;
                end++;
            }

            if (end >= block.Length) return string.Empty;

            string raw = block.Substring(start + 1, end - start - 1);
            return Unescape(raw);
        }

        static string Unescape(string value)
        {
            if (value.IndexOf('\\') < 0) return value;

            System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i + 1 >= value.Length)
                {
                    builder.Append(c);
                    continue;
                }

                char next = value[++i];
                switch (next)
                {
                    case 'n': builder.Append('\n'); break;
                    case 't': builder.Append('\t'); break;
                    case 'r': builder.Append('\r'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    default: builder.Append(next); break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Used only when there is no ledger at all. A page with no recorded authors
        /// still gets one witness - the hand that wrote it and signed nothing.
        /// </summary>
        static List<Contributor> Fallback()
        {
            List<Contributor> hands = new List<Contributor>();
            hands.Add(new Contributor(
                "THE UNATTRIBUTED HAND",
                "wrote on this page and left no name",
                string.Empty,
                string.Empty));
            return hands;
        }
    }
}
