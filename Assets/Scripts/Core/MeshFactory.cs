using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Palimpsest
{
    /// <summary>
    /// Procedural geometry and primitive plumbing.
    ///
    /// Unity's built-in primitives give us beds, candles and erasures, but the
    /// vellum page itself is a generated disc with generated binding rings. All
    /// meshes are cached statically so repeated world rebuilds do not leak.
    /// </summary>
    public static class MeshFactory
    {
        static Mesh _unitRing;
        static Mesh _unitDisc;
        static Mesh _unitBlot;
        static Mesh _floorDisc;

        // ------------------------------------------------------------------
        // Cached meshes
        // ------------------------------------------------------------------

        /// <summary>Unit radius (inner 0.88) ring, meant to be scaled on X/Z.</summary>
        public static Mesh UnitRing
        {
            get
            {
                if (_unitRing == null) _unitRing = BuildRing(0.88f, 1f, 64);
                return _unitRing;
            }
        }

        /// <summary>Unit radius disc intended to be scaled on X/Z.</summary>
        public static Mesh UnitDisc
        {
            get
            {
                if (_unitDisc == null) _unitDisc = BuildDisc(1f, 64);
                return _unitDisc;
            }
        }

        public static Mesh FloorDisc(float radius)
        {
            if (_floorDisc == null) _floorDisc = BuildDisc(radius, 96);
            return _floorDisc;
        }

        /// <summary>
        /// A one-off annulus. Deliberately not cached: the page uses this for several
        /// different radii, and a single shared mesh would paint them all identically.
        /// </summary>
        public static Mesh MarginBand(float inner, float outer)
        {
            return BuildRing(inner, outer, 96);
        }

        /// <summary>A one-off annulus used for the heart ruling.</summary>
        public static Mesh HeartBand(float inner, float outer)
        {
            return BuildRing(inner, outer, 64);
        }

        /// <summary>
        /// A cached, irregular disc used for the silt blots left by un-written
        /// erasures. The wobble is baked once and every instance is then rotated by
        /// its own seed, so a page carrying a hundred stains never reads as a field
        /// of identical circles.
        /// </summary>
        public static Mesh UnitBlot
        {
            get
            {
                if (_unitBlot == null) _unitBlot = BuildBlot(1f, 48, 0x5EED);
                return _unitBlot;
            }
        }

        // ------------------------------------------------------------------
        // Mesh construction
        // ------------------------------------------------------------------

        /// <summary>A flat, upward facing disc centred on the origin.</summary>
        public static Mesh BuildDisc(float radius, int segments)
        {
            if (segments < 3) segments = 3;

            Mesh mesh = new Mesh();
            mesh.name = "PalimpsestDisc";

            Vector3[] vertices = new Vector3[segments + 1];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            int[] triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[i * 3 + 0] = 0;
                triangles[i * 3 + 1] = next + 1;
                triangles[i * 3 + 2] = i + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A flat, upward facing annulus.</summary>
        public static Mesh BuildRing(float innerRadius, float outerRadius, int segments)
        {
            if (segments < 3) segments = 3;
            if (innerRadius >= outerRadius) innerRadius = outerRadius * 0.9f;

            Mesh mesh = new Mesh();
            mesh.name = "PalimpsestRing";

            Vector3[] vertices = new Vector3[segments * 2];
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertices[i * 2 + 0] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
                vertices[i * 2 + 1] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
            }

            int[] triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int a = i * 2 + 0;      // inner current
                int b = i * 2 + 1;      // outer current
                int c = next * 2 + 1;   // outer next
                int d = next * 2 + 0;   // inner next

                triangles[i * 6 + 0] = a;
                triangles[i * 6 + 1] = c;
                triangles[i * 6 + 2] = b;

                triangles[i * 6 + 3] = a;
                triangles[i * 6 + 4] = d;
                triangles[i * 6 + 5] = c;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A flat disc with a wobbling, double-rimmed silhouette. Used for silt
        /// stains: the shape must read as a spill rather than a plate.
        /// </summary>
        public static Mesh BuildBlot(float radius, int segments, int seed)
        {
            if (segments < 3) segments = 3;

            System.Random rng = new System.Random(seed);

            float[] innerJitter = new float[segments];
            float[] outerJitter = new float[segments];
            for (int i = 0; i < segments; i++)
            {
                innerJitter[i] = 0.44f + (float)rng.NextDouble() * 0.16f;
                outerJitter[i] = 0.76f + (float)rng.NextDouble() * 0.32f;
            }

            Vector3[] vertices = new Vector3[1 + segments * 2];
            vertices[0] = Vector3.zero;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertices[1 + i] = new Vector3(cos * radius * innerJitter[i], 0f, sin * radius * innerJitter[i]);
                vertices[1 + segments + i] = new Vector3(cos * radius * outerJitter[i], 0f, sin * radius * outerJitter[i]);
            }

            int[] triangles = new int[segments * 9];
            int innerStart = 1;
            int outerStart = 1 + segments;

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int a = innerStart + i;
                int b = outerStart + i;
                int c = outerStart + next;
                int d = innerStart + next;

                // Centre fan meeting the inner rim, wound like BuildDisc.
                triangles[i * 9 + 0] = 0;
                triangles[i * 9 + 1] = d;
                triangles[i * 9 + 2] = a;

                // The band between the two wobbling rims, wound like BuildRing.
                triangles[i * 9 + 3] = a;
                triangles[i * 9 + 4] = c;
                triangles[i * 9 + 5] = b;

                triangles[i * 9 + 6] = a;
                triangles[i * 9 + 7] = d;
                triangles[i * 9 + 8] = c;
            }

            Mesh mesh = new Mesh();
            mesh.name = "PalimpsestBlot";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------
        // Procedural page furniture
        // ------------------------------------------------------------------

        /// <summary>
        /// Builds the ghost of previous writing: rows of broken dashes that read as
        /// erased lines of text. Returned as one mesh so the whole scrawl costs a
        /// single draw call.
        /// </summary>
        public static Mesh BuildScrawl(float radius, int seed, int rows, int perRow)
        {
            List<Vector3> vertices = new List<Vector3>(rows * perRow * 4);
            List<int> triangles = new List<int>(rows * perRow * 6);
            System.Random rng = new System.Random(seed);

            float usable = radius * 0.80f;

            for (int r = 0; r < rows; r++)
            {
                float t = rows <= 1 ? 0.5f : r / (float)(rows - 1);
                float z = Mathf.Lerp(-usable, usable, t);
                float halfWidth = Mathf.Sqrt(Mathf.Max(0f, usable * usable - z * z)) * 0.86f;
                if (halfWidth < 0.8f) continue;

                float x = -halfWidth + (float)rng.NextDouble() * 0.5f;
                int guard = 0;
                while (x < halfWidth && guard++ < perRow * 3)
                {
                    float dash = 0.14f + (float)rng.NextDouble() * 0.62f;
                    float gap = 0.10f + (float)rng.NextDouble() * 0.42f;
                    float thickness = 0.030f + (float)rng.NextDouble() * 0.030f;
                    float jitter = ((float)rng.NextDouble() - 0.5f) * 0.045f;

                    AddFlatQuad(vertices, triangles, x + dash * 0.5f, z + jitter, dash, thickness);
                    x += dash + gap;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "PalimpsestScrawl";
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddFlatQuad(List<Vector3> vertices, List<int> triangles,
            float centerX, float centerZ, float width, float thickness)
        {
            int start = vertices.Count;
            float hx = width * 0.5f;
            float hz = thickness * 0.5f;
            const float y = 0.006f;

            vertices.Add(new Vector3(centerX - hx, y, centerZ - hz));
            vertices.Add(new Vector3(centerX - hx, y, centerZ + hz));
            vertices.Add(new Vector3(centerX + hx, y, centerZ + hz));
            vertices.Add(new Vector3(centerX + hx, y, centerZ - hz));

            triangles.Add(start + 0);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 0);
            triangles.Add(start + 3);
            triangles.Add(start + 2);
        }

        // ------------------------------------------------------------------
        // Object conveniences
        // ------------------------------------------------------------------

        /// <summary>Creates an unlit-geometry object from a generated mesh.</summary>
        public static GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return go;
        }

        /// <summary>Creates a primitive without its collider (this game is deliberately collisionless).</summary>
        public static GameObject Primitive(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            return Primitive(type, name, parent, localPosition, localScale, Quaternion.identity, material);
        }

        /// <summary>Creates a primitive without its collider, with an explicit local rotation.</summary>
        public static GameObject Primitive(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;

            StripCollider(go);

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
            return go;
        }

        /// <summary>Removes the automatically added collider from a primitive.</summary>
        public static void StripCollider(GameObject go)
        {
            if (go == null) return;
            Collider collider = go.GetComponent<Collider>();
            if (collider == null) return;
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }
    }
}
