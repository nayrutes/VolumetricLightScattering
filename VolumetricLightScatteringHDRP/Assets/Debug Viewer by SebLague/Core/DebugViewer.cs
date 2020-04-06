using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Visualisation.MeshGeneration;

namespace Visualisation {

    public enum Style { Standard, Unlit }

    public static partial class Vis {

        const string shaderPath_unlitAlpha = "HDRP/Unlit";
        const string shaderPath_standard = "HDRP/Lit";

        static Material[] materials;
        static MaterialPropertyBlock materialProperties;

        // Cached meshes:
        // These are meshes that don't change, in contrast to dynamic meshes (like an arc, where the angle can change)
        // As such, they only need to be generated once, and reused as needed.
        static Mesh sphereMesh;
        static Mesh cylinderMesh;

        static Queue<Mesh> inactiveMeshes;
        static List<DrawInfo> drawList;

        static int lastFrameInputReceived;

        static Vis () {
            //Camera.onPreCull -= Draw;
            //Camera.onPreCull += Draw;
            RenderPipelineManager.beginFrameRendering -= Draw;
            RenderPipelineManager.beginFrameRendering += Draw;
            
            Init ();
        }

        static void Init () {
            if (sphereMesh == null) {
                inactiveMeshes = new Queue<Mesh> ();
                materialProperties = new MaterialPropertyBlock ();
                drawList = new List<DrawInfo> ();

                // Generate and cache primitive meshes
                sphereMesh = new Mesh ();
                cylinderMesh = new Mesh ();
                SphereMesh.GenerateMesh (sphereMesh);
                CylinderMesh.GenerateMesh (cylinderMesh);

                // Create materials
                materials = new Material[2];
                materials[0] = new Material (Shader.Find (shaderPath_standard));
                materials[1] = new Material (Shader.Find (shaderPath_unlitAlpha));
            }

            // New frame index, so clear out last frame's draw list
            if (lastFrameInputReceived != Time.frameCount) {
                lastFrameInputReceived = Time.frameCount;

                // Store all unique meshes in inactive queue to be recycled
                var usedMeshes = new HashSet<Mesh> ();
                // Don't recycle cached meshes
                usedMeshes.Add (sphereMesh);
                usedMeshes.Add (cylinderMesh);

                for (int i = 0; i < drawList.Count; i++) {
                    if (!usedMeshes.Contains (drawList[i].mesh)) {
                        usedMeshes.Add (drawList[i].mesh);
                        inactiveMeshes.Enqueue (drawList[i].mesh);
                    }
                }

                // Clear old draw list
                drawList.Clear ();
            }
        }

        // Draw all items in the drawList on each game/scene camera
        static void Draw (ScriptableRenderContext context, Camera[] camera) {
            if (Time.frameCount == lastFrameInputReceived) {
                for (int i = 0; i < drawList.Count; i++) {
                    DrawInfo drawData = drawList[i];
                    Matrix4x4 matrix = Matrix4x4.TRS (drawData.position, drawData.rotation, drawData.scale);
                    if (drawData.style == Style.Unlit)
                    {
                        materialProperties.SetColor ("_UnlitColor", drawData.colour);
                    }
                    else
                    {
                        materialProperties.SetColor ("_BaseColor", drawData.colour);
                    }
                    Material activeMaterial = materials[(int) drawData.style];
                    Graphics.DrawMesh (drawData.mesh, matrix, activeMaterial, 0, null, 0, materialProperties);
                }
            }
        }

        static Mesh CreateOrRecycleMesh () {
            Mesh mesh = null;
            if (inactiveMeshes.Count > 0) {
                mesh = inactiveMeshes.Dequeue ();
                mesh.Clear ();
            } else {
                mesh = new Mesh ();
            }

            return mesh;
        }

        class DrawInfo {
            public Mesh mesh;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Color colour;
            public Style style;

            public DrawInfo (Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color colour, Style style) {
                this.mesh = mesh;
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
                this.colour = colour;
                this.style = style;
            }
        }
    }
}