using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Palimpsest
{
    /// <summary>
    /// Creates every material in the game at runtime.
    ///
    /// No .mat assets exist in this repository by design, so the factory resolves
    /// the correct shader for whichever render pipeline the project is opened in
    /// (Built-in "Standard" or "Universal Render Pipeline/Lit") and caches one
    /// material per colour/intensity pair. Cached materials are shared, which keeps
    /// draw calls sane; anything that needs to animate its colour asks for a
    /// "Dynamic" variant that the owner is responsible for destroying.
    /// </summary>
    public static class MaterialFactory
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static Shader _lit;
        static Shader _unlit;

        // ------------------------------------------------------------------
        // Shader resolution
        // ------------------------------------------------------------------

        /// <summary>True when a Scriptable Render Pipeline (URP/HDRP) is active.</summary>
        public static bool UsingScriptablePipeline
        {
            get { return GraphicsSettings.currentRenderPipeline != null; }
        }

        static Shader LitShader
        {
            get
            {
                if (_lit == null)
                {
                    _lit = UsingScriptablePipeline
                        ? Resolve("Universal Render Pipeline/Lit", "Standard",
                                  "Legacy Shaders/Diffuse", "Diffuse", "Sprites/Default")
                        : Resolve("Standard", "Universal Render Pipeline/Lit",
                                  "Legacy Shaders/Diffuse", "Diffuse", "Sprites/Default");
                }
                return _lit;
            }
        }

        static Shader UnlitShader
        {
            get
            {
                if (_unlit == null)
                {
                    _unlit = UsingScriptablePipeline
                        ? Resolve("Universal Render Pipeline/Unlit", "Unlit/Color",
                                  "Sprites/Default", "Standard")
                        : Resolve("Unlit/Color", "Sprites/Default", "Standard",
                                  "Universal Render Pipeline/Unlit");
                }
                return _unlit;
            }
        }

        static Shader Resolve(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null) return shader;
            }
            Debug.LogWarning("[Palimpsest] No suitable shader found; using the render pipeline default.");
            return Shader.Find("Sprites/Default");
        }

        // ------------------------------------------------------------------
        // Public creation API
        // ------------------------------------------------------------------

        /// <summary>Shared, opaque, physically lit surface material.</summary>
        public static Material Lit(Color color, float smoothness = 0.12f)
        {
            string key = "lit|" + ColorKey(color) + "|" + smoothness.ToString("F2");
            Material cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            Material material = new Material(LitShader);
            material.name = "PalLit_" + key;
            ApplyColor(material, color);
            ApplySmoothness(material, smoothness);
            Cache[key] = material;
            return material;
        }

        /// <summary>Shared surface material that emits light of its own colour.</summary>
        public static Material Emissive(Color color, float intensity = 2f)
        {
            string key = "emit|" + ColorKey(color) + "|" + intensity.ToString("F2");
            Material cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            Material material = new Material(LitShader);
            material.name = "PalEmit_" + key;
            ApplyColor(material, color);
            ApplySmoothness(material, 0.35f);
            ApplyEmission(material, color, intensity);
            Cache[key] = material;
            return material;
        }

        /// <summary>Shared material that ignores scene lighting (used for glyphs and glyph rings).</summary>
        public static Material Unlit(Color color)
        {
            string key = "unlit|" + ColorKey(color);
            Material cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

            Material material = new Material(UnlitShader);
            material.name = "PalUnlit_" + key;
            ApplyColor(material, color);
            Cache[key] = material;
            return material;
        }

        /// <summary>
        /// A one-off lit material. The caller owns it and must destroy it when done
        /// (used by wards and erasures, which animate their own emission).
        /// </summary>
        public static Material DynamicLit(Color color, float smoothness = 0.2f)
        {
            Material material = new Material(LitShader);
            material.name = "PalDynamicLit";
            ApplyColor(material, color);
            ApplySmoothness(material, smoothness);
            return material;
        }

        /// <summary>A one-off emissive material owned by the caller.</summary>
        public static Material DynamicEmissive(Color color, float intensity = 2f)
        {
            Material material = new Material(LitShader);
            material.name = "PalDynamicEmissive";
            ApplyColor(material, color);
            ApplySmoothness(material, 0.3f);
            ApplyEmission(material, color, intensity);
            return material;
        }

        // ------------------------------------------------------------------
        // Property helpers - these work across both supported pipelines
        // ------------------------------------------------------------------

        public static void ApplyColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", color);
        }

        public static void ApplyEmission(Material material, Color color, float intensity)
        {
            if (material == null) return;
            material.EnableKeyword("_EMISSION");
            Color emission = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
        }

        public static void ApplySmoothness(Material material, float smoothness)
        {
            if (material == null) return;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        }

        static string ColorKey(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
