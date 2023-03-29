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

using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System.Collections.Generic;

namespace AnimatedPeople
{
    // Copied from DFU MobilePersonBillboard class and modified.
    [ImportedComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class AnimatedPeopleBillboard : Billboard
    {
        static Mod mod;

        static bool forceNoNudity;

        static Dictionary<string, List<Texture2D>> textureCache = new Dictionary<string, List<Texture2D>>();

        public float SecondsPerFrame = 0.2f;     // How much time between frames

        public float DelayMin = 0; // Minimum delay before anim runs
        public float DelayMax = 0; // Maximun delay before anim runs
        public int RepeatMin = 0; // Minimum animation loop before adding new delay
        public int RepeatMax = 0; // Maximum animation loop before adding new delay
        
        public int Archive = 182;
        public int Record = 0;

        #region Billboard
        public override int FramesPerSecond
        {
            get { return Mathf.RoundToInt(1 / SecondsPerFrame); }
            set { SecondsPerFrame = 1 / value; }
        }

        public override bool OneShot { get; set; }
        public override bool FaceY { get; set; }
        #endregion

        Camera mainCamera = null;
        MeshFilter meshFilter = null;
        MeshRenderer meshRenderer;

        float animationDelay = 0.0f;
        int repeatCount = 0;
        float frameBuffer = 0.0f;

        bool firstUpdate = true;
        bool aligned = false;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();
            mod.IsReady = true;
        }

        static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
        {
            forceNoNudity = modSettings.GetBool("Compatibility", "ForceNoNudity");
        }

        static bool HasNoNudity()
        {
            Mod noNudity = ModManager.Instance.GetMod("No nudity");
            return noNudity != null && noNudity.Enabled;
        }

        string GetSettingPrefix(int archive, int record)
        {
            if (!forceNoNudity && !HasNoNudity())
                return "";

            if(archive == 175)
            {
                if (record == 0)
                    return ".NN";
            }
            else if(archive == 176)
            {
                if (record == 2
                    || record == 3)
                    return ".NN";
            }
            else if(archive == 179)
            {
                if (record == 0
                    || record == 1
                    || record == 3)
                    return ".NN";
            }
            else if(archive == 181)
            {
                if (record == 6)
                    return ".NN";
            }
            else if(archive == 182)
            {
                if (record == 32
                    || record == 34
                    || record == 41
                    || record == 48)
                    return ".NN";
            }
            else if(archive == 184)
            {
                if (record == 6
                    || record == 11
                    || record == 12
                    || record == 13
                    || record == 14
                    || record == 31)
                    return ".NN";
            }

            return "";
        }

        void Start()
        {
            if (Application.isPlaying)
            {                
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
            }
        }

        private void OnDisable()
        {
            firstUpdate = true;
        }

        void SetupAnimatedPeople()
        {
            SetMaterial(Archive, Record);
            AlignToBase();

            summary.CurrentFrame = GetFrameCount() - 1;

            SetCurrentFrame();

            if (DelayMax != 0.0f)
            {
                animationDelay = UnityEngine.Random.Range(DelayMin, DelayMax);
            }

            if (RepeatMax != 0)
            {
                repeatCount = UnityEngine.Random.Range(RepeatMin, RepeatMax + 1);
            }
        }

        void Update()
        {
            if(firstUpdate)
            {
                SetupAnimatedPeople();
                firstUpdate = false;
            }

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
        public override Material SetMaterial(int archive, int record, int frame = 0)
        {
            // Get DaggerfallUnity
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return null;

            // Get references
            meshRenderer = GetComponent<MeshRenderer>();

            string prefix = GetSettingPrefix(archive, record);

            Vector2 size;
            Vector2 scale;
            Mesh mesh = null;
            Material material = null;
            if(!string.IsNullOrEmpty(prefix))
            {
                material = GetCustomBillboardMaterial(archive, record, prefix, ref summary, out scale);
                mesh = dfUnity.MeshReader.GetBillboardMesh(summary.Rect, archive, record, out size);
                size *= scale;
                summary.AtlasedMaterial = false;
                summary.AnimatedMaterial = summary.ImportedTextures.FrameCount > 1;
            }
            else if (material = TextureReplacement.GetStaticBillboardMaterial(gameObject, archive, record, ref summary, out scale))
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
                Collider col = gameObject.GetComponent<BoxCollider>();
                if(col == null)
                    col = gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
            }

            return material;
        }
                
        /// <summary>
        /// Aligns billboard to centre of base, rather than exact centre.
        /// Must have already set material using SetMaterial() for billboard dimensions to be known.
        /// </summary>
        public override void AlignToBase()
        {
            if (aligned)
                return;
                        
            // MeshReplace.AlignToBase lowers custom billboard prefabs in dungeons for some reason
            // Just put them back up
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                int height = ImageReader.GetImageData(TextureFile.IndexToFileName(Archive), Record, createTexture: false).height;

                Vector3 offset = Vector3.zero;
                offset.y = height / 2 * MeshReader.GlobalScale;
                transform.position += offset;
            }
            else
            {
                // Calcuate offset for correct positioning in scene
                Vector3 offset = Vector3.zero;
                offset.y = (summary.Size.y / 2);
                transform.position += offset;
            }

            aligned = true;
        }

        public override void SetRMBPeopleData(DFBlock.RmbBlockPeopleRecord person)
        {
            SetRMBPeopleData(person.FactionID, person.Flags, person.Position);
        }

        /// <summary>
        /// Sets people data directly.
        /// </summary>
        /// <param name="factionID">FactionID of person.</param>
        /// <param name="flags">Person flags.</param>
        public override void SetRMBPeopleData(int factionID, int flags, long position = 0)
        {
            // Add common data
            summary.FactionOrMobileID = factionID;
            summary.FixedEnemyType = MobileTypes.None;
            summary.Flags = flags;

            // TEMP: Add name seed
            summary.NameSeed = (int)position;
        }

        public override void SetRDBResourceData(DFBlock.RdbFlatResource resource)
        {
            // Add common data
            summary.Flags = resource.Flags;
            summary.FactionOrMobileID = (int)resource.FactionOrMobileId;
            summary.FixedEnemyType = MobileTypes.None;

            // TEMP: Add name seed
            summary.NameSeed = (int)resource.Position;

            // Set data of fixed mobile types (e.g. non-random enemy spawn)
            if (resource.TextureArchive == 199)
            {
                if (resource.TextureRecord == 16)
                {
                    summary.IsMobile = true;
                    summary.EditorFlatType = EditorFlatTypes.FixedMobile;
                    summary.FixedEnemyType = (MobileTypes)(summary.FactionOrMobileID & 0xff);
                }
                else if (resource.TextureRecord == 10) // Start marker. Holds data for dungeon block water level and castle block status.
                {
                    if (resource.SoundIndex != 0)
                        summary.WaterLevel = (short)(-8 * resource.SoundIndex);
                    else
                        summary.WaterLevel = 10000; // no water

                    summary.CastleBlock = (resource.Magnitude != 0);
                }
            }
        }

        public override Material SetMaterial(Texture2D texture, Vector2 size, bool isLightArchive = false)
        {
            // Get DaggerfallUnity
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return null;

            // Get references
            meshRenderer = GetComponent<MeshRenderer>();

            // Create material
            Material material = MaterialReader.CreateBillboardMaterial();
            material.mainTexture = texture;

            // Create mesh
            Mesh mesh = dfUnity.MeshReader.GetSimpleBillboardMesh(size);

            // Set summary
            summary.FlatType = FlatTypes.Decoration;
            summary.Size = size;

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
            meshRenderer.shadowCastingMode = (DaggerfallUnity.Settings.GeneralBillboardShadows && !isLightArchive) ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off;

            return material;
        }

        static Material GetCustomBillboardMaterial(int archive, int record, string prefix, ref BillboardSummary summary, out Vector2 scale)
        {
            scale = Vector2.one;

            string firstFrameName = $"{archive}{prefix}_{record}-0";

            if(!textureCache.TryGetValue(firstFrameName, out List<Texture2D> albedo))
            {
                albedo = new List<Texture2D>();

                string frameName = firstFrameName;
                int frame = 0;
                while(ModManager.Instance.TryGetAsset(frameName, clone: false, out Texture2D frameAsset))
                {
                    albedo.Add(frameAsset);
                    frameName = $"{archive}{prefix}_{record}-{++frame}";
                }

                if (frame == 0)
                    return null;

                textureCache.Add(firstFrameName, albedo);
            }

            summary.ImportedTextures.HasImportedTextures = true;
            summary.ImportedTextures.Albedo = albedo;
            summary.ImportedTextures.FrameCount = albedo.Count;
            

            // Make material
            Material material = MaterialReader.CreateBillboardMaterial();
            summary.Rect = new Rect(0, 0, 1, 1);

            // Set textures on material
            material.SetTexture(Uniforms.MainTex, albedo[0]);

            return material;
        }
    }
}
