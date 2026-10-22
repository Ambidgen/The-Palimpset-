using UnityEngine;
using UnityEngine.UI;

namespace Palimpsest
{
    /// <summary>
    /// The entire interface, constructed at runtime.
    ///
    /// There is no prefab and no scene file, so every element here - canvas, text,
    /// bars, overlays - is created from a handful of helpers. The palette stays
    /// within the game's manuscript colours and the type is a serif when the host
    /// machine can supply one.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        Arena _arena;

        Text _layerLabel;
        Text _folioLabel;
        Text _flameLabel;
        Text _wardLabel;
        Text _scoreLabel;
        Text _toastLabel;
        Text _hintLabel;
        Text _verbLabel;
        Image _integrityFill;
        Text _integrityLabel;

        CanvasGroup _titleGroup;
        Text _titleHeadline;
        Text _titleSub;
        Text _titlePrompt;

        CanvasGroup _overGroup;
        Text _overHeadline;
        Text _overBody;
        Text _overPrompt;

        CanvasGroup _pauseGroup;

        float _toastLife;
        float _hintLife = 14f;
        float _titlePulse;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public static Hud Build(Transform parent, Arena arena)
        {
            GameObject root = new GameObject("Palimpsest HUD");
            root.transform.SetParent(parent, false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            Hud hud = root.AddComponent<Hud>();
            hud._arena = arena;
            hud.BuildElements(root.transform);
            return hud;
        }

        void BuildElements(Transform root)
        {
            Font serif = ResolveSerif();

            // ---------------- persistent readouts ----------------
            _layerLabel = Label(root, "Layer", "LAYER I", 40, FontStyle.Normal,
                TextAnchor.UpperLeft, Palette.Text, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(36f, -28f), new Vector2(760f, 52f), serif);

            _folioLabel = Label(root, "Folio", "THE FIRST HAND", 22, FontStyle.Normal,
                TextAnchor.UpperLeft, Palette.Muted, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(38f, -84f), new Vector2(760f, 32f), serif);

            _flameLabel = Label(root, "Flames", "FLAMES  0 / 0", 24, FontStyle.Normal,
                TextAnchor.UpperLeft, Palette.Ember, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(38f, -128f), new Vector2(500f, 32f), serif);

            _wardLabel = Label(root, "Wards", "SEALS  0 / 0", 24, FontStyle.Normal,
                TextAnchor.UpperLeft, Palette.InkGlow, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(38f, -162f), new Vector2(500f, 32f), serif);

            _scoreLabel = Label(root, "Score", "SCORE 0", 22, FontStyle.Normal,
                TextAnchor.UpperRight, Palette.Muted, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-38f, -68f), new Vector2(420f, 32f), serif);

            _integrityLabel = Label(root, "Integrity", "INK 100", 22, FontStyle.Normal,
                TextAnchor.UpperRight, Palette.Muted, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-38f, -104f), new Vector2(420f, 30f), serif);

            BuildIntegrityBar(root);

            _toastLabel = Label(root, "Toast", "", 30, FontStyle.Normal,
                TextAnchor.UpperCenter, Palette.Alert, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -60f), new Vector2(1100f, 44f), serif);

            _verbLabel = Label(root, "Verbs", "", 21, FontStyle.Normal,
                TextAnchor.LowerRight, Palette.Muted, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-38f, 66f), new Vector2(760f, 30f), serif);

            _hintLabel = Label(root, "Hint",
                "WASD / ARROWS  move      LEFT CLICK or SPACE  inscribe (within reach)      E or RIGHT CLICK  scrape      SHIFT or Q  illuminate      ESC  pause",
                20, FontStyle.Normal, TextAnchor.LowerCenter, Palette.Muted,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1500f, 30f), serif);

            // ---------------- title overlay ----------------
            GameObject titleRoot = Panel(root, "Title Overlay", Palette.Alpha(Palette.Night, 0.86f));
            _titleGroup = titleRoot.AddComponent<CanvasGroup>();

            Label(titleRoot.transform, "Title Colophon", "A  P A L I M P S E S T", 30, FontStyle.Normal,
                TextAnchor.MiddleCenter, Palette.Muted, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 176f), new Vector2(1200f, 40f), serif);

            _titleHeadline = Label(titleRoot.transform, "Title Headline", "THE PALIMPSEST", 116, FontStyle.Normal,
                TextAnchor.MiddleCenter, Palette.Text, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 76f), new Vector2(1600f, 150f), serif);

            _titleSub = Label(titleRoot.transform, "Title Sub",
                "you are the last scribe of a page that has been written over\nlay the seals, keep the flames, and do not let the undertext rise",
                26, FontStyle.Normal, TextAnchor.MiddleCenter, Palette.Muted,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(1300f, 90f), serif);

            _titlePrompt = Label(titleRoot.transform, "Title Prompt", "PRESS SPACE TO BEGIN", 34, FontStyle.Normal,
                TextAnchor.MiddleCenter, Palette.InkGlow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -168f), new Vector2(900f, 50f), serif);

            // ---------------- pause overlay ----------------
            GameObject pauseRoot = Panel(root, "Pause Overlay", Palette.Alpha(Palette.Night, 0.78f));
            _pauseGroup = pauseRoot.AddComponent<CanvasGroup>();
            _pauseGroup.alpha = 0f;
            _pauseGroup.blocksRaycasts = false;
            _pauseGroup.interactable = false;

            Label(pauseRoot.transform, "Pause Headline", "THE QUILL RESTS", 68, FontStyle.Normal,
                TextAnchor.MiddleCenter, Palette.Text, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(1200f, 90f), serif);
            Label(pauseRoot.transform, "Pause Sub", "ESC to return to the page", 26, FontStyle.Normal,
                TextAnchor.MiddleCenter, Palette.Muted, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -34f), new Vector2(1200f, 40f), serif);

            // ---------------- game over overlay ----------------
            GameObject overRoot = Panel(root, "Over Overlay", Palette.Alpha(Palette.Night, 0.9f));
            _overGroup = overRoot.AddComponent<CanvasGroup>();
            _overGroup.alpha = 0f;
            _overGroup.blocksRaycasts = false;
            _overGroup.interactable = false;

            _overHeadline = Label(overRoot.transform, "Over Headline", "THE PAGE IS SCRAPED", 84, FontStyle.Normal,
                TextAnchor.MiddleCenter, Palette.Alert, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 96f), new Vector2(1500f, 110f), serif);

            _overBody = Label(overRoot.transform, "Over Body", "", 30, FontStyle.Normal,
                TextAnchor.MiddleCenter, Palette.Text, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f), new Vector2(1200f, 160f), serif);

            _overPrompt = Label(overRoot.transform, "Over Prompt", "PRESS R TO BEGIN A NEW FOLIO", 32, FontStyle.Normal,
                TextAnchor.MiddleCenter, Palette.InkGlow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -160f), new Vector2(1200f, 50f), serif);
        }

        void BuildIntegrityBar(Transform root)
        {
            GameObject frame = new GameObject("Integrity Frame");
            frame.transform.SetParent(root, false);
            Image frameImage = frame.AddComponent<Image>();
            frameImage.color = Palette.Alpha(Palette.Undertext, 0.85f);

            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(1f, 1f);
            frameRect.anchorMax = new Vector2(1f, 1f);
            frameRect.pivot = new Vector2(1f, 1f);
            frameRect.anchoredPosition = new Vector2(-38f, -142f);
            frameRect.sizeDelta = new Vector2(360f, 14f);

            GameObject fill = new GameObject("Integrity Fill");
            fill.transform.SetParent(frame.transform, false);
            _integrityFill = fill.AddComponent<Image>();
            _integrityFill.color = Palette.InkGlow;

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(0f, 0f);
            fillRect.offsetMin = new Vector2(0f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillRect.sizeDelta = new Vector2(356f, 0f);
        }

        // ------------------------------------------------------------------
        // Layout helpers
        // ------------------------------------------------------------------

        static Font ResolveSerif()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { /* Older engines use Arial.ttf instead. */ }

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

        static Text Label(Transform parent, string name, string content, int size, FontStyle style,
            TextAnchor anchor, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta, Font font)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMax.y);
            if (Mathf.Approximately(anchorMin.x, 0.5f) && Mathf.Approximately(anchorMax.x, 0.5f)) rect.pivot = new Vector2(0.5f, anchorMax.y);
            if (Mathf.Approximately(anchorMin.x, 1f)) rect.pivot = new Vector2(1f, anchorMax.y);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            return text;
        }

        static GameObject Panel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go;
        }

        // ------------------------------------------------------------------
        // Runtime
        // ------------------------------------------------------------------

        void Update()
        {
            if (_arena == null) return;

            float dt = Time.unscaledDeltaTime;

            _toastLife = Mathf.Max(0f, _toastLife - dt);
            _hintLife = Mathf.Max(0f, _hintLife - dt);

            float toastAlpha = Mathf.Clamp01(_toastLife);
            _toastLabel.color = Palette.Alpha(Palette.Alert, toastAlpha);

            float hintAlpha = Mathf.Clamp01(_hintLife / 3f) * 0.85f;
            _hintLabel.color = Palette.Alpha(Palette.Muted, hintAlpha);

            bool showTitle = _arena.IsTitle;
            _titleGroup.alpha = Mathf.MoveTowards(_titleGroup.alpha, showTitle ? 1f : 0f, dt * 5f);
            _titleGroup.blocksRaycasts = showTitle;

            // A slow breath on the prompt so the title screen is never fully still.
            _titlePulse += dt;
            if (_titlePrompt != null)
            {
                float pulse = 0.55f + 0.45f * Mathf.Sin(_titlePulse * 2.1f);
                _titlePrompt.color = Palette.Alpha(Palette.InkGlow, 0.35f + pulse * 0.65f);
            }

            bool showPause = _arena.IsPaused;
            _pauseGroup.alpha = Mathf.MoveTowards(_pauseGroup.alpha, showPause ? 1f : 0f, dt * 8f);
            _pauseGroup.blocksRaycasts = showPause;

            bool showOver = _arena.IsOver;
            _overGroup.alpha = Mathf.MoveTowards(_overGroup.alpha, showOver ? 1f : 0f, dt * 3.5f);
            _overGroup.blocksRaycasts = showOver;

            if (!showTitle) RefreshReadouts();
        }

        void RefreshReadouts()
        {
            int layer = _arena.Layer;
            _layerLabel.text = "LAYER " + Roman(layer);
            _folioLabel.text = _arena.FolioTitle;

            int flames = _arena.LivingFlames;
            int flamesNeeded = _arena.FlamesRequired;
            _flameLabel.text = "FLAMES  " + flames + " / " + flamesNeeded;
            _flameLabel.color = flames < flamesNeeded ? Palette.Alert : Palette.Ember;

            int seals = _arena.WardsThisLayer;
            int sealsNeeded = _arena.WardsRequired;
            _wardLabel.text = "SEALS  " + Mathf.Min(seals, sealsNeeded) + " / " + sealsNeeded;
            _wardLabel.color = seals >= sealsNeeded ? Palette.InkGlow : Palette.Text;

            if (_arena.IsSiege)
            {
                _wardLabel.text += "      HOLD " + Mathf.CeilToInt(_arena.SiegeRemaining) + "s";
                _wardLabel.color = Palette.Alert;
            }

            _scoreLabel.text = "FOLIOS " + (_arena.Folio - 1) + "      SCORE " + _arena.Score;

            if (_verbLabel != null)
            {
                float illuminate = _arena.IlluminateCooldownRemaining;
                float scrape = _arena.ScrapeCooldownRemaining;

                string illuminateText = illuminate <= 0f
                    ? "SHIFT  ILLUMINATE"
                    : "SHIFT  ILLUMINATE " + Mathf.CeilToInt(illuminate) + "s";
                string scrapeText = scrape <= 0f
                    ? "E  SCRAPE"
                    : "E  SCRAPE " + Mathf.CeilToInt(scrape) + "s";

                _verbLabel.text = illuminateText + "      " + scrapeText;
                _verbLabel.color = Palette.Alpha(Palette.Muted, (illuminate <= 0f || scrape <= 0f) ? 0.95f : 0.55f);
            }

            float pct = Mathf.Clamp01(_arena.Integrity / 100f);
            _integrityLabel.text = "INTEGRITY  " + Mathf.CeilToInt(_arena.Integrity);
            _integrityLabel.color = pct < 0.34f ? Palette.Alert : Palette.Muted;

            if (_integrityFill != null)
            {
                RectTransform rect = _integrityFill.rectTransform;
                rect.sizeDelta = new Vector2(356f * pct, rect.sizeDelta.y);
                _integrityFill.color = Color.Lerp(Palette.Alert, Palette.InkGlow, Mathf.InverseLerp(0.15f, 0.75f, pct));
            }

            if (_arena.IsOver)
            {
                _overHeadline.text = _arena.IsVictory ? "THE PAGE ENDURES" : "THE PAGE IS SCRAPED";
                _overHeadline.color = _arena.IsVictory ? Palette.InkGlow : Palette.Alert;
                _overBody.text = _arena.OutcomeLine;
                _overPrompt.text = "PRESS R TO BEGIN A NEW FOLIO";
            }
        }

        public void Toast(string message)
        {
            if (_toastLabel == null) return;
            _toastLabel.text = message;
            _toastLife = 2.6f;
        }

        public static string Roman(int value)
        {
            if (value <= 0) return "-";
            int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] glyphs = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int remaining = value;
            for (int i = 0; i < values.Length; i++)
            {
                while (remaining >= values[i])
                {
                    builder.Append(glyphs[i]);
                    remaining -= values[i];
                }
            }
            return builder.ToString();
        }
    }
}
