using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// The single source of truth for the manuscript's palette.
    /// Every object in the game is built from primitives at runtime, so colour is
    /// the only "art direction" channel available. Keeping it centralised means the
    /// whole mood of a folio can be re-tuned from one file.
    /// </summary>
    public static class Palette
    {
        // The room the scribe works in: near-black violet, as if the scriptorium
        // has burned down and only the vellum is still catching the candlelight.
        public static readonly Color Night = new Color(0.031f, 0.027f, 0.043f);
        public static readonly Color Vellum = new Color(0.470f, 0.410f, 0.310f);
        public static readonly Color VellumDeep = new Color(0.310f, 0.270f, 0.210f);
        public static readonly Color Rule = new Color(0.240f, 0.210f, 0.160f);

        // Ink: the player's only weapon and currency.
        public static readonly Color Ink = new Color(0.055f, 0.062f, 0.085f);
        public static readonly Color InkGlow = new Color(0.360f, 0.940f, 1.000f);
        public static readonly Color InkDeep = new Color(0.100f, 0.470f, 0.620f);

        // Erasure: the things that eat the page.
        public static readonly Color Erasure = new Color(0.100f, 0.055f, 0.140f);
        public static readonly Color ErasureGlow = new Color(0.620f, 0.220f, 0.820f);

        // The candle that must not go out.
        public static readonly Color Ember = new Color(1.000f, 0.620f, 0.220f);
        public static readonly Color EmberDeep = new Color(0.720f, 0.240f, 0.070f);
        public static readonly Color Wax = new Color(0.880f, 0.820f, 0.680f);

        // State that has already been written and scraped away.
        public static readonly Color Undertext = new Color(0.270f, 0.245f, 0.290f);

        // Interface.
        public static readonly Color Text = new Color(0.940f, 0.910f, 0.820f);
        public static readonly Color Muted = new Color(0.630f, 0.600f, 0.530f);
        public static readonly Color Alert = new Color(1.000f, 0.380f, 0.320f);

        // The colophon: the older hands still standing in the margin. A witness is
        // tinted from its own name, so this is only the colour of a hand with none.
        public static readonly Color Witness = new Color(0.640f, 0.800f, 0.950f);

        /// <summary>Returns the same colour with a replaced alpha channel.</summary>
        public static Color Alpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        /// <summary>Linear blend that reads a little better than a raw lerp for UI text.</summary>
        public static Color Blend(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, Mathf.Clamp01(t));
        }

        /// <summary>
        /// The deeper the palimpsest, the more the light shifts from clean ink
        /// towards the bruised violet of old erasures.
        /// </summary>
        public static Color LayerTint(int layer)
        {
            float t = Mathf.Clamp01((layer - 1) / 10f);
            return Color.Lerp(InkGlow, ErasureGlow, t);
        }

        /// <summary>
        /// The colour one hand in the colophon wears, hashed out of the name it signed
        /// so that a hand is always the same colour on any page and in any session.
        ///
        /// The hue is confined to the band the rest of the manuscript already lives in
        /// - clean ink through to the violet of old erasures - and pushed further
        /// along it by what the hand recorded doing: a hand that built sits toward the
        /// ink, one that turned the page somewhere new sits paler and further along,
        /// and one that took something away is pulled all the way to the alert red.
        /// </summary>
        public static Color WitnessHue(string name, HandDisposition disposition)
        {
            string key = string.IsNullOrEmpty(name) ? "a hand" : name;

            uint hash = 2166136261u;
            for (int i = 0; i < key.Length; i++)
            {
                hash = (hash ^ (uint)key[i]) * 16777619u;
            }

            float t = (hash % 1024u) / 1024f;

            switch (disposition)
            {
                case HandDisposition.Erasing:
                    // Around the red, where nothing on this page is ever safe.
                    return Color.HSVToRGB(Mathf.Repeat(0.930f + t * 0.085f, 1f), 0.66f, 1f);
                case HandDisposition.Wandering:
                    // Teal into clean ink: the colour of a page being turned.
                    return Color.HSVToRGB(0.430f + t * 0.115f, 0.50f, 1f);
                default:
                    // Ink blue through to the violet of the undertext.
                    return Color.HSVToRGB(0.525f + t * 0.180f, 0.56f, 1f);
            }
        }
    }
}
