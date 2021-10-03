// Project:         Villager Variety mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2021 kaboissonneault
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Authors:         kaboissonneault

using UnityEngine;
using UnityEngine.Rendering;

using System;
using System.Collections;

using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;

using static DaggerfallWorkshop.DaggerfallBillboard;

namespace AnimatedPeople
{
    // Copied from DFU MobilePersonBillboard class and modified.
    [ImportedComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class AnimatedPeopleBillboard : MonoBehaviour
    {
        public float SecondsPerFrame = 0.2f;     // How much time between frames

        public float DelayMin = 0; // Minimum delay before anim runs
        public float DelayMax = 0; // Maximun delay before anim runs
        public int RepeatMin = 0; // Minimum animation loop before adding new delay
        public int RepeatMax = 0; // Maximum animation loop before adding new delay
        
        public int Archive = 182;
        public int Record = 0;

        [SerializeField]
        BillboardSummary summary = new BillboardSummary();

        Camera mainCamera = null;
        MeshFilter meshFilter = null;
        MeshRenderer meshRenderer;
        public BillboardSummary Summary
        {
            get { return summary; }
        }

        float animationDelay = 0.0f;
        int repeatCount = 0;
        float frameBuffer = 0.0f;

        void Start()
        {
            if (Application.isPlaying)
            {
                SetMaterial(Archive, Record);
                AlignToBase();

                summary.CurrentFrame = GetFrameCount() - 1;

                // Get component references
                mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
                meshFilter = GetComponent<MeshFilter>();
                meshRenderer = GetComponent<MeshRenderer>();

                // Hide editor marker from live scene
                bool showEditorFlats = GameManager.Instance.StartGameBehaviour.ShowEditorFlats;
                if (summary.FlatType == FlatTypes.Editor && meshRenderer && !showEditorFlats)
                {
                    // Just disable mesh renderer as actual object can be part of action chain
                    // Example is the treasury in Daggerfall castle, some action records flow through the quest item marker
                    meshRenderer.enabled = false;
                }

                SetCurrentFrame();

                if(DelayMax != 0.0f)
                {
                    animationDelay = UnityEngine.Random.Range(DelayMin, DelayMax);
                }

                if(RepeatMax != 0)
                {
                    repeatCount = UnityEngine.Random.Range(RepeatMin, RepeatMax + 1);
                }
            }
        }

        void Update()
        {

            // Rotate to face camera in game
            // Do not rotate if MeshRenderer disabled. The player can't see it anyway and this could be a hidden editor marker with child objects.
            // In the case of hidden editor markers with child treasure objects, we don't want a 3D replacement spinning around like a billboard.
            // Treasure objects are parented to editor marker in this way as the moving action data for treasure is actually on editor marker parent.
            // Visible child of treasure objects have their own MeshRenderer and DaggerfallBillboard to apply rotations.
            if (mainCamera && Application.isPlaying && meshRenderer.enabled)
            {
                Vector3 viewDirection = -new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z);
                transform.LookAt(transform.position + viewDirection);
            }

            UpdateAnimation();
        }

        void UpdateAnimation()
        {
            if (meshFilter != null && summary.AnimatedMaterial)
            {
                if (animationDelay > 0.0f)
                {
                    animationDelay -= Time.deltaTime;
                    return;
                }

                frameBuffer += Time.deltaTime;

                if (frameBuffer >= SecondsPerFrame)
                {
                    SetCurrentFrame();

                    summary.CurrentFrame++;

                    if (summary.CurrentFrame >= GetFrameCount())
                    {
                        summary.CurrentFrame = 0;

                        if(repeatCount > 0)
                        {
                            repeatCount--;
                        }
                        else
                        {
                            animationDelay = UnityEngine.Random.Range(DelayMin, DelayMax);
                            if (RepeatMax != 0)
                            {
                                repeatCount = UnityEngine.Random.Range(RepeatMin, RepeatMax + 1);
                            }
                        }
                    }

                    frameBuffer -= SecondsPerFrame;
                }
            }
        }

        int GetFrameCount()
        {
            return !summary.ImportedTextures.HasImportedTextures
                    ? summary.AtlasIndices[summary.Record].frameCount
                    : summary.ImportedTextures.FrameCount;
        }

        void SetCurrentFrame()
        {
            // Original Daggerfall textures
            if (!summary.ImportedTextures.HasImportedTextures)
            {
                int index = summary.AtlasIndices[summary.Record].startIndex + summary.CurrentFrame;
                Rect rect = summary.AtlasRects[index];

                // Update UVs on mesh
                Vector2[] uvs = new Vector2[4];
                uvs[0] = new Vector2(rect.x, rect.yMax);
                uvs[1] = new Vector2(rect.xMax, rect.yMax);
                uvs[2] = new Vector2(rect.x, rect.y);
                uvs[3] = new Vector2(rect.xMax, rect.y);
                meshFilter.sharedMesh.uv = uvs;
            }
            // Custom textures
            else
            {
                // Set imported textures for current frame
                meshRenderer.material.SetTexture("_MainTex", summary.ImportedTextures.Albedo[summary.CurrentFrame]);
                if (summary.ImportedTextures.IsEmissive)
                    meshRenderer.material.SetTexture("_EmissionMap", summary.ImportedTextures.Emission[summary.CurrentFrame]);
            }
        }

        /// <summary>
        /// Sets new Daggerfall material and recreates mesh.
        /// Will use an atlas if specified in DaggerfallUnity singleton.
        /// </summary>
        /// <param name="dfUnity">DaggerfallUnity singleton. Required for content readers and settings.</param>
        /// <param name="archive">Texture archive index.</param>
        /// <param name="record">Texture record index.</param>
        /// <param name="frame">Frame index.</param>
        /// <returns>Material.</returns>
        Material SetMaterial(int archive, int record)
        {
            // Get DaggerfallUnity
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return null;

            // Get references
            meshRenderer = GetComponent<MeshRenderer>();

            Vector2 size;
            Vector2 scale;
            Mesh mesh = null;
            Material material = null;
            if (material = TextureReplacement.GetStaticBillboardMaterial(gameObject, archive, record, ref summary, out scale))
            {
                mesh = dfUnity.MeshReader.GetBillboardMesh(summary.Rect, archive, record, out size);
                size *= scale;
                summary.AtlasedMaterial = false;
                summary.AnimatedMaterial = summary.ImportedTextures.FrameCount > 1;
            }
            else if (dfUnity.MaterialReader.AtlasTextures)
            {
                material = dfUnity.MaterialReader.GetMaterialAtlas(
                    archive,
                    0,
                    4,
                    2048,
                    out summary.AtlasRects,
                    out summary.AtlasIndices,
                    4,
                    true,
                    0,
                    false,
                    true);
                mesh = dfUnity.MeshReader.GetBillboardMesh(
                    summary.AtlasRects[summary.AtlasIndices[record].startIndex],
                    archive,
                    record,
                    out size);
                summary.AtlasedMaterial = true;
                if (summary.AtlasIndices[record].frameCount > 1)
                    summary.AnimatedMaterial = true;
                else
                    summary.AnimatedMaterial = false;
            }
            else
            {
                material = dfUnity.MaterialReader.GetMaterial(
                    archive,
                    record,
                    0,
                    0,
                    out summary.Rect,
                    4,
                    true,
                    true);
                mesh = dfUnity.MeshReader.GetBillboardMesh(
                    summary.Rect,
                    archive,
                    record,
                    out size);
                summary.AtlasedMaterial = false;
                summary.AnimatedMaterial = false;
            }

            // Set summary
            summary.FlatType = MaterialReader.GetFlatType(archive);
            summary.Archive = archive;
            summary.Record = record;
            summary.Size = size;

            // Set editor flat types
            if (summary.FlatType == FlatTypes.Editor)
                summary.EditorFlatType = MaterialReader.GetEditorFlatType(summary.Record);

            // Set NPC flat type based on archive
            if (RDBLayout.IsNPCFlat(summary.Archive))
                summary.FlatType = FlatTypes.NPC;

            // Assign mesh and material
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            Mesh oldMesh = meshFilter.sharedMesh;
            if (mesh)
            {
                meshFilter.sharedMesh = mesh;
                meshRenderer.sharedMaterial = material;
            }
            if (oldMesh)
            {
                // The old mesh is no longer required
#if UNITY_EDITOR
                DestroyImmediate(oldMesh);
#else
                Destroy(oldMesh);
#endif
            }

            // General billboard shadows if enabled
            bool isLightArchive = (archive == TextureReader.LightsTextureArchive);
            meshRenderer.shadowCastingMode = (DaggerfallUnity.Settings.GeneralBillboardShadows && !isLightArchive) ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off;

            // Add NPC trigger collider
            if (summary.FlatType == FlatTypes.NPC)
            {
                Collider col = gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
            }

            return material;
        }
                
        /// <summary>
        /// Aligns billboard to centre of base, rather than exact centre.
        /// Must have already set material using SetMaterial() for billboard dimensions to be known.
        /// </summary>
        void AlignToBase()
        {
            // Calcuate offset for correct positioning in scene
            Vector3 offset = Vector3.zero;
            offset.y = (summary.Size.y / 2);
            transform.position += offset;
        }
    }
}
