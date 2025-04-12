using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SplashEdit.RuntimeCode
{
    /// <summary>
    /// Represents a vertex formatted for the PSX (PlayStation) style rendering.
    /// </summary>
    public struct PSXVertex
    {
        // Position components in fixed-point format.
        public short vx, vy, vz;
        // Normal vector components in fixed-point format.
        public short nx, ny, nz;
        // Texture coordinates.
        public byte u, v;
        // Vertex color components.
        public byte r, g, b;
    }

    /// <summary>
    /// Represents a triangle defined by three PSX vertices.
    /// </summary>
    public struct Tri
    {
        public PSXVertex v0;
        public PSXVertex v1;
        public PSXVertex v2;

        public PSXTexture2D Texture;
        public readonly PSXVertex[] Vertexes => new PSXVertex[] { v0, v1, v2 };
    }

    /// <summary>
    /// A mesh structure that holds a list of triangles converted from a Unity mesh into the PSX format.
    /// </summary>
    [System.Serializable]
    public class PSXMesh
    {
        public List<Tri> Triangles;


        private static Vector3[] RecalculateSmoothNormals(Mesh mesh)
        {
            Vector3[] normals = new Vector3[mesh.vertexCount];
            Dictionary<Vector3, List<int>> vertexMap = new Dictionary<Vector3, List<int>>();

            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vector3 vertex = mesh.vertices[i];
                if (!vertexMap.ContainsKey(vertex))
                {
                    vertexMap[vertex] = new List<int>();
                }
                vertexMap[vertex].Add(i);
            }

            foreach (var kvp in vertexMap)
            {
                Vector3 smoothNormal = Vector3.zero;
                foreach (int index in kvp.Value)
                {
                    smoothNormal += mesh.normals[index];
                }
                smoothNormal.Normalize();

                foreach (int index in kvp.Value)
                {
                    normals[index] = smoothNormal;
                }
            }

            return normals;
        }


        /// <summary>
        /// Creates a PSXMesh from a Unity Mesh by converting its vertices, normals, UVs, and applying shading.
        /// </summary>
        /// <param name="mesh">The Unity mesh to convert.</param>
        /// <param name="textureWidth">Width of the texture (default is 256).</param>
        /// <param name="textureHeight">Height of the texture (default is 256).</param>
        /// <param name="transform">Optional transform to convert vertices to world space.</param>
        /// <returns>A new PSXMesh containing the converted triangles.</returns>
        public static PSXMesh CreateFromUnityRenderer(Renderer renderer, float GTEScaling, Transform transform, List<PSXTexture2D> textures)
        {
            PSXMesh psxMesh = new PSXMesh { Triangles = new List<Tri>() };

            // Get materials and mesh.
            Material[] materials = renderer.sharedMaterials;
            Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            
            bool uvWarning = false;

            // Iterate over each submesh.
            for (int submeshIndex = 0; submeshIndex < materials.Length; submeshIndex++)
            {
                // Get the triangles for this submesh.
                int[] submeshTriangles = mesh.GetTriangles(submeshIndex);

                // Get the material for this submesh.
                Material material = materials[submeshIndex];

                // Get the corresponding texture for this material (assume mainTexture).
                Texture2D texture = material.mainTexture as Texture2D;
                PSXTexture2D psxTexture = null;

                if (texture != null)
                {
                    // Find the corresponding PSX texture based on the Unity texture.
                    psxTexture = textures.FirstOrDefault(t => t.OriginalTexture == texture);
                }

                if (psxTexture == null)
                {
                    continue;
                }

                // Get mesh data arrays.
                mesh.RecalculateNormals();
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                Vector3[] smoothNormals = RecalculateSmoothNormals(mesh);
                Vector2[] uv = mesh.uv;
                PSXVertex convertData(int index)
                {
                    // Scale the vertex based on world scale.
                    Vector3 v = Vector3.Scale(vertices[index], transform.lossyScale);
                    // Transform the vertex to world space.
                    Vector3 wv = transform.TransformPoint(vertices[index]);
                    // Transform the normals to world space.
                    Vector3 wn = transform.TransformDirection(smoothNormals[index]).normalized;
                    // Compute lighting for each vertex.
                    Color c = PSXLightingBaker.ComputeLighting(wv, wn);
                    // Convert vertex to PSX format, including fixed-point conversion and shading.
                    return ConvertToPSXVertex(v, GTEScaling, normals[index], uv[index], psxTexture?.Width, psxTexture?.Height, c);
                }
                // Iterate through the triangles of the submesh.
                for (int i = 0; i < submeshTriangles.Length; i += 3)
                {
                    int vid0 = submeshTriangles[i];
                    int vid1 = submeshTriangles[i + 1];
                    int vid2 = submeshTriangles[i + 2];

                    Vector3 faceNormal = Vector3.Cross(vertices[vid1] - vertices[vid0], vertices[vid2] - vertices[vid0]).normalized;

                    if (Vector3.Dot(faceNormal, normals[vid0]) < 0)
                    {
                        (vid1, vid2) = (vid2, vid1);
                    }

                    // Set uvWarning to true if uv cooordinates are outside the range [0, 1].
                    if (uv[vid0].x < 0 || uv[vid0].y < 0 || uv[vid1].x < 0 || uv[vid1].y < 0 || uv[vid2].x < 0 || uv[vid2].y < 0)
                        uvWarning = true;
                    if (uv[vid0].x > 1 || uv[vid0].y > 1 || uv[vid1].x > 1 || uv[vid1].y > 1 || uv[vid2].x > 1 || uv[vid2].y > 1)
                        uvWarning = true;

                    // Add the constructed triangle to the mesh.
                    psxMesh.Triangles.Add(new Tri { v0 = convertData(vid0), v1 = convertData(vid1), v2 = convertData(vid2), Texture = psxTexture });
                }
            }

            if(uvWarning)
            {
                Debug.LogWarning($"UV coordinates on mesh {mesh.name} are outside the range [0, 1]. Texture repeat DOES NOT WORK right now. You may have broken textures.");
            }

            return psxMesh;
        }

        /// <summary>
        /// Converts a Unity vertex into a PSXVertex by applying fixed-point conversion, shading, and UV mapping.
        /// </summary>
        /// <param name="vertex">The position of the vertex.</param>
        /// <param name="normal">The normal vector at the vertex.</param>
        /// <param name="uv">Texture coordinates for the vertex.</param>
        /// <param name="lightDir">The light direction used for shading calculations.</param>
        /// <param name="lightColor">The color of the light affecting the vertex.</param>
        /// <param name="textureWidth">Width of the texture for UV scaling.</param>
        /// <param name="textureHeight">Height of the texture for UV scaling.</param>
        /// <returns>A PSXVertex with converted coordinates, normals, UVs, and color.</returns>
        private static PSXVertex ConvertToPSXVertex(Vector3 vertex, float GTEScaling, Vector3 normal, Vector2 uv, int? textureWidth, int? textureHeight, Color color)
        {
            int width = textureWidth ?? 0;
            int height = textureHeight ?? 0;
            PSXVertex psxVertex = new PSXVertex
            {
                // Convert position to fixed-point, clamping values to a defined range.
                vx = PSXTrig.ConvertCoordinateToPSX(vertex.x, GTEScaling),
                vy = PSXTrig.ConvertCoordinateToPSX(-vertex.y, GTEScaling),
                vz = PSXTrig.ConvertCoordinateToPSX(vertex.z, GTEScaling),

                // Convert normals to fixed-point.
                nx = PSXTrig.ConvertCoordinateToPSX(normal.x),
                ny = PSXTrig.ConvertCoordinateToPSX(-normal.y),
                nz = PSXTrig.ConvertCoordinateToPSX(normal.z),

                // Map UV coordinates to a byte range after scaling based on texture dimensions.



                u = (byte)Mathf.Clamp(uv.x * (width - 1), 0, 255),
                v = (byte)Mathf.Clamp((1.0f - uv.y) * (height - 1), 0, 255),

                // Apply lighting to the colors.
                r = Utils.ColorUnityToPSX(color.r),
                g = Utils.ColorUnityToPSX(color.g),
                b = Utils.ColorUnityToPSX(color.b),
            };

            return psxVertex;
        }
    }
}
