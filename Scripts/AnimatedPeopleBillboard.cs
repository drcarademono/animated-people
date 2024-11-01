// Project:         Villager Variety mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2021 kaboissonneault
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Authors:         kaboissonneault

using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using DaggerfallConnect;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System.Collections.Generic;
using System.Linq;
using FullSerializer;
using System.IO;

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
        static bool verboseLogs;

        public bool HasCustomPortrait { get; set; }
        public int CustomPortraitRecord { get; set; }

        public Dictionary<uint, List<FlatReplacement>> flatReplacements;

        static Dictionary<string, List<Texture2D>> textureCache = new Dictionary<string, List<Texture2D>>();

        public float SecondsPerFrame = 0.2f;     // How much time between frames

        public float DelayMin = 0; // Minimum delay before anim runs
        public float DelayMax = 0; // Maximun delay before anim runs
        public int RepeatMin = 0; // Minimum animation loop before adding new delay
        public int RepeatMax = 0; // Maximum animation loop before adding new delay

        public int Archive = 182;
        public int Record = 0;

        private int originalArchive;
        private int originalRecord;

        public int RnRArchive = 0;
        public int RnRRecord = 0;

        public float originalScale = 1.0f;

        private bool useExactDimensions = true;

        private XMLManager xml;

        #region Billboard
        public override int FramesPerSecond
        {
            get { return Mathf.RoundToInt(1 / SecondsPerFrame); }
            set
            {
                // Don't let mods change our FramesPerSecond
            }
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
        bool materialSet = false;
        bool aligned = false;
    
        bool RnRFlag = false;

	    private static Mod FlatReplacerMod;

	    private static bool FlatReplacerModEnabled;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();

		    FlatReplacerMod = ModManager.Instance.GetModFromGUID("8f05f3ed-bc08-4eb9-b856-05a58b0b63da");
		    if (FlatReplacerMod != null && FlatReplacerMod.Enabled)
		    {
			    FlatReplacerModEnabled = true;
			    if(verboseLogs) Debug.Log("VE-AP: Flat Replacer Mod is active");
		    }

            mod.IsReady = true;
        }

        static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
        {
            verboseLogs = modSettings.GetBool("Core", "VerboseLog");
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

        void Awake()
        {
            if(verboseLogs) Debug.Log($"[VE-AP] Awake on {Archive}-{Record}");

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

            originalScale = GetOriginalScale(Archive, Record);
        }

        private void OnEnable()
        {
            if(verboseLogs) Debug.Log($"[VE-AP] OnEnable on {Archive}-{Record}");

            if (!materialSet)
            {
                SetupAnimatedPeople();
                materialSet = true;
            }

            if (FlatReplacerModEnabled) {
                // Store the original values
                originalArchive = summary.Archive;
                originalRecord = summary.Record;

                // Set to dummy values to avoid FlatReplacer detection
                summary.Archive = 0;
                summary.Record = 0;

                if (verboseLogs) Debug.Log($"[VE-AP] Overriding FlatReplacer for archive {originalArchive} and record {originalRecord}");
            }
        }

        void Start()
        {
            if(verboseLogs) Debug.Log("[VE-AP] Start method called.");


            if (FlatReplacerModEnabled) {
                // Initialize flatReplacements
                flatReplacements = new Dictionary<uint, List<FlatReplacement>>();

                // Check and update archive/record values
                CheckAndUpdateArchiveRecord();

                // Handle Talk Window change
                DaggerfallUI.UIManager.OnWindowChange += OnWindowChange;
            }

            if (DelayMax != 0.0f)
            {
                animationDelay = UnityEngine.Random.Range(DelayMin, DelayMax);
            }

            if (RepeatMax != 0)
            {
                repeatCount = UnityEngine.Random.Range(RepeatMin, RepeatMax + 1);
            }
        }

        public float GetOriginalScale(int archive, int record)
        {
            // Debug message indicating the start of the scale calculation process
            if (verboseLogs) Debug.Log($"[GetOriginalScale] Starting for archive {archive}, record {record}.");

            // Get the size of the original billboard mesh
            Vector2 meshSize;
            Mesh billboardMesh = DaggerfallUnity.Instance.MeshReader.GetBillboardMesh(new Rect(0, 0, 1, 1), archive, record, out meshSize);

            if (billboardMesh == null)
            {
                if (verboseLogs) Debug.LogError($"[GetOriginalScale] Failed to get billboard mesh for archive {archive}, record {record}.");
                return 1.0f; // Default to 1.0f scale if unable to get mesh
            }

            // Debugging the original mesh dimensions
            if (verboseLogs) Debug.Log($"[GetOriginalScale] Original Billboard Mesh size (X, Y): {meshSize.x}, {meshSize.y}");

            // Retrieve the original texture using the correct GetTexture2D method
            var settings = new GetTextureSettings
            {
                archive = archive,
                record = record,
                frame = 0,
                alphaIndex = 0
            };

            GetTextureResults textureResults = DaggerfallUnity.Instance.MaterialReader.TextureReader.GetTexture2D(settings);
            Texture2D originalTexture = textureResults.albedoMap;

            if (originalTexture == null)
            {
                if (verboseLogs) Debug.LogError($"[GetOriginalScale] Failed to get original texture for archive {archive}, record {record}.");
                return 1.0f; // Default to 1.0f scale if unable to get texture
            }

            // Retrieve texture dimensions
            int textureWidth = originalTexture.width;
            int textureHeight = originalTexture.height;

            // Debugging the original texture dimensions
            if (verboseLogs) Debug.Log($"[GetOriginalScale] Original Texture size (Width, Height): {textureWidth}, {textureHeight}");

            // Calculate the scale based on MeshX/TextureWidth or MeshY/TextureHeight
            float scaleX = (meshSize.x * 40.0f) / textureWidth;
            float scaleY = (meshSize.y * 40.0f) / textureHeight;

            // For simplicity, return the average of X and Y scales
            float originalScale = (scaleX + scaleY) / 2.0f;

            // Debugging the final scale
            if (verboseLogs) Debug.Log($"[GetOriginalScale] Original scale calculated: {originalScale}");

            return originalScale;
        }

        void LoadSettingsFromCSV()
        {

            if (verboseLogs) Debug.Log($"[LoadSettingsFromCSV] LoadSettingsFromCSV called on Archive {Archive}, Record {Record}");

            // Set default values
            SecondsPerFrame = 0.2f;
            DelayMin = 0;
            DelayMax = 0;
            RepeatMin = 0;
            RepeatMax = 0;

            string filePath = "AnimatedPeople.csv"; // Path to your CSV file
            try
            {
                if (ModManager.Instance.TryGetAsset<TextAsset>(filePath, false, out TextAsset csvAsset))
                {
                    using (StringReader reader = new StringReader(csvAsset.text))
                    {
                        string line = reader.ReadLine(); // Read header line
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] values = line.Split(',');

                            // Assuming columns are Archive, Record, SecondsPerFrame, DelayMin, DelayMax, RepeatMin, RepeatMax
                            if (values.Length < 7)
                            {
                                Debug.LogWarning($"[VE-AP] Incorrect number of columns in CSV: {line}");
                                continue;
                            }

                            int archive = int.Parse(values[0]);
                            int record = int.Parse(values[1]);

                            // Check if the current archive and record match the row's archive and record
                            if (archive == this.Archive && record == this.Record)
                            {
                                // Update the settings
                                SecondsPerFrame = float.TryParse(values[2], out float spf) ? spf : SecondsPerFrame;
                                DelayMin = float.TryParse(values[3], out float dmin) ? dmin : DelayMin;
                                DelayMax = float.TryParse(values[4], out float dmax) ? dmax : DelayMax;
                                RepeatMin = int.TryParse(values[5], out int rmin) ? rmin : RepeatMin;
                                RepeatMax = int.TryParse(values[6], out int rmax) ? rmax : RepeatMax;

                                if (verboseLogs)
                                {
                                    Debug.Log($"[VE-AP] Loaded settings from CSV for {Archive}-{Record}: SecondsPerFrame={SecondsPerFrame}, DelayMin={DelayMin}, DelayMax={DelayMax}, RepeatMin={RepeatMin}, RepeatMax={RepeatMax}");
                                }
                                break; // Exit loop once settings are found and updated
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("[VE-AP] CSV asset not found.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VE-AP] Error loading settings from CSV: {ex.Message}");
            }
        }

        void CheckAndUpdateArchiveRecord()
        {
            if (verboseLogs) Debug.Log("[VE-AP] Checking and updating archive/record.");

            var key = ((uint)Archive << 16) + (uint)Record;
            if (!AnimatedPeople.flatReplacements.ContainsKey(key))
            {
                if (verboseLogs) Debug.Log($"[VE-AP] No replacement available for archive {Archive}, record {Record}.");
                return; // No replacement available for this archive/record.
            }

            var candidates = new List<byte>();
            var playerGps = GameManager.Instance.PlayerGPS;
            var buildingData = GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData;

            for (var i = 0; i < AnimatedPeople.flatReplacements[key].Count; i++)
            {
                var replacementRecord = AnimatedPeople.flatReplacements[key][i].Record;
                var regionFound = replacementRecord.Regions[0] == -1 || replacementRecord.Regions.Contains(playerGps.CurrentRegionIndex);

                if (!regionFound)
                    continue;

                if ((replacementRecord.FactionId != -1 && replacementRecord.FactionId != buildingData.factionID) ||
                    (replacementRecord.BuildingType != -1 && replacementRecord.BuildingType != (int)buildingData.buildingType) ||
                    (buildingData.quality < replacementRecord.QualityMin || buildingData.quality > replacementRecord.QualityMax))
                    continue;

                candidates.Add((byte)i);
            }

            if (candidates.Count == 0)
            {
                if (verboseLogs) Debug.Log("[VE-AP] No valid candidates found for replacement.");
                return; // No valid candidates found.
            }

            var chosenIndex = candidates.Count > 1 ? new System.Random().Next(candidates.Count) : 0;
            var chosenReplacement = AnimatedPeople.flatReplacements[key][candidates[chosenIndex]];

            if (verboseLogs) Debug.Log($"[VE-AP] Replacing archive {Archive}, record {Record} with archive {chosenReplacement.Record.ReplaceTextureArchive}, record {chosenReplacement.Record.ReplaceTextureRecord}. UseExactDimension: {chosenReplacement.Record.UseExactDimensions}.");

            Archive = chosenReplacement.Record.ReplaceTextureArchive;
            Record = chosenReplacement.Record.ReplaceTextureRecord;
            useExactDimensions = chosenReplacement.Record.UseExactDimensions;

            // Update material and mesh
            SetMaterial(Archive, Record);

            // Set custom portrait if applicable
            if (chosenReplacement.Record.FlatPortrait > -1)
            {
                HasCustomPortrait = true;
                CustomPortraitRecord = chosenReplacement.Record.FlatPortrait;
            }
        }

        private void OnWindowChange(object sender, EventArgs e)
        {
            if (DaggerfallUI.UIManager.TopWindow != DaggerfallUI.Instance.TalkWindow || !TalkManager.Instance.StaticNPC)
                return;

            var replacementBillboard = TalkManager.Instance.StaticNPC.gameObject.GetComponent<AnimatedPeopleBillboard>();
            var facePortraitArchive = DaggerfallWorkshop.Game.UserInterface.DaggerfallTalkWindow.FacePortraitArchive.CommonFaces;
            GameManager.Instance.PlayerEntity.FactionData.GetFactionData(TalkManager.Instance.StaticNPC.Data.factionID, out var factionData);

            if (RnRFlag && RnRArchive == 197 && RnRRecord >= 0 && RnRRecord <= 6)
            {
                int portraitId = 197000 + RnRRecord;
                DaggerfallUI.Instance.TalkWindow.SetNPCPortrait(facePortraitArchive, portraitId);
                return;
            }

            if (factionData.type == 4 && factionData.face <= 60)
                facePortraitArchive = DaggerfallWorkshop.Game.UserInterface.DaggerfallTalkWindow.FacePortraitArchive.SpecialFaces;

            if (replacementBillboard && replacementBillboard.HasCustomPortrait)
            {
                if(verboseLogs) Debug.Log($"[VE-AP] Setting custom portrait: {replacementBillboard.CustomPortraitRecord}");
                DaggerfallUI.Instance.TalkWindow.SetNPCPortrait(facePortraitArchive, replacementBillboard.CustomPortraitRecord);
            }
        }

        public class FlatReplacementRecord
        {
            public int[] Regions;
            public int FactionId;
            public int BuildingType;
            public int QualityMin;
            public int QualityMax;
            public int TextureArchive;
            public int TextureRecord;
            public int ReplaceTextureArchive;
            public int ReplaceTextureRecord;
            public string FlatTextureName;
            public bool UseExactDimensions;
            public int FlatPortrait;
        }

        public class FlatReplacement
        {
            public FlatReplacementRecord Record;
        }

        private void OnDisable()
        {
            if(verboseLogs) Debug.Log($"[VE-AP] OnDisable on {Archive}-{Record}");

            firstUpdate = true;
        }

        void SetupAnimatedPeople()
        {
            if(verboseLogs) Debug.Log($"[VE-AP] SetupAnimatedPeople on {Archive}-{Record}");

            if (meshFilter == null)
            {
                Debug.LogWarning($"[VE-AP] Mesh filter null on record '{Archive}_{Record}'");
            }

            if (meshRenderer == null)
            {
                Debug.LogWarning($"[VE-AP] Mesh renderer null on record '{Archive}_{Record}'");
            }

            if (mainCamera == null)
            {
                Debug.LogWarning($"[VE-AP] Main camera null on record '{Archive}_{Record}'");
            }

            SetMaterial(Archive, Record);
        }

        void Update()
        {
            if(firstUpdate)
            {
                if(verboseLogs) Debug.Log($"[VE-AP] First Update on {Archive}-{Record}");

                summary.Archive = Archive;
                summary.Record = Record;
                if (!RnRFlag) AlignToBase();
                //AlignToBase();

                int frameCount = GetFrameCount();

                if(frameCount == 0)
                {
                    if(verboseLogs) Debug.LogError($"[VE-AP] Could not setup AP, frame count is zero on record '{Archive}_{Record}'");
                    return;
                }

                summary.CurrentFrame = frameCount - 1;

                SetCurrentFrame();

                var col = gameObject.GetComponent<BoxCollider>();

                // Calculate adjusted collider size based on mesh size and scaling
                Vector3 adjustedColliderSize = new Vector3(
                    summary.Size.x / transform.localScale.x,
                    summary.Size.y / transform.localScale.y,
                    0.1f // Small depth for 2D object
                );
                col.size = adjustedColliderSize;

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
                    ? summary.AtlasIndices[Record].frameCount
                    : summary.ImportedTextures.FrameCount;
        }

        void SetCurrentFrame()
        {
            // Original Daggerfall textures
            if (!summary.ImportedTextures.HasImportedTextures)
            {
                int index = summary.AtlasIndices[Record].startIndex + summary.CurrentFrame;
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
                var albedo = summary.ImportedTextures.Albedo[summary.CurrentFrame];
                if(albedo == null)
                {
                    Debug.LogError("[VE-AP] Imported textures albedo went null");
                    return;
                }

                // Set imported textures for current frame
                meshRenderer.material.SetTexture("_MainTex", albedo);
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
            if (verboseLogs) Debug.Log($"[VE-AP] SetMaterial on {Archive}-{Record} with archive={archive} and record={record}");

            // Load settings from CSV and update them
            LoadSettingsFromCSV();

            // AP is not setup to handle mods that change our billboard to another
            // archive-record. Ignore their calls to SetMaterial
            if (archive != Archive || record != Record)
            {
                //return null;
                RnRArchive = archive;
                RnRRecord = record;
                RnRFlag = true;
            }

            if (archive == 0 && record == 0) // Fixes exterior NPCs, which must be called as 0-0 somehow in RMBLayout.
            {
                archive = Archive;
                record = Record;
                RnRFlag = false;
            }

            // Get DaggerfallUnity
            DaggerfallUnity dfUnity = DaggerfallUnity.Instance;
            if (!dfUnity.IsReady)
                return null;

            // Get references
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            string prefix = GetSettingPrefix(archive, record);

            Vector2 size = Vector2.zero;
            Vector2 scale = Vector2.one;
            Mesh mesh = null;
            Material material = null;

            if (!string.IsNullOrEmpty(prefix) || archive == 197 || archive > 511)
            {
                material = GetCustomBillboardMaterial(archive, record, prefix, ref summary, out scale);
                if (material == null)
                {
                    Debug.LogError("[VE-AP] Failed to get custom billboard material.");
                    return null;
                }
                mesh = dfUnity.MeshReader.GetBillboardMesh(summary.Rect, archive, record, out size);
                size *= scale;
                summary.AtlasedMaterial = false;
                summary.AnimatedMaterial = summary.ImportedTextures.FrameCount > 1;
            }
            else if (TextureReplacement.GetStaticBillboardMaterial(gameObject, archive, record, ref summary, out scale))
            {
                material = TextureReplacement.GetStaticBillboardMaterial(gameObject, archive, record, ref summary, out scale);
                if (material == null)
                {
                    Debug.LogError("[VE-AP] Failed to get static billboard material.");
                    return null;
                }
                mesh = dfUnity.MeshReader.GetBillboardMesh(summary.Rect, archive, record, out size);
                size *= scale;
                summary.AtlasedMaterial = false;
                summary.AnimatedMaterial = summary.ImportedTextures.FrameCount > 1;
            }
            else if (dfUnity.MaterialReader.AtlasTextures)
            {
                try
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

                    if (material == null)
                    {
                        Debug.LogError("[VE-AP] Failed to get atlas material.");
                        return null;
                    }

                    // Ensure the record index is within bounds
                    if (record < 0 || record >= summary.AtlasIndices.Length)
                    {
                        if (verboseLogs) Debug.Log($"[VE-AP] Record index {record} is out of bounds for atlas indices.");
                        return null;
                    }

                    mesh = dfUnity.MeshReader.GetBillboardMesh(
                        summary.AtlasRects[summary.AtlasIndices[record].startIndex],
                        archive,
                        record,
                        out size);

                    summary.AtlasedMaterial = true;
                    summary.AnimatedMaterial = summary.AtlasIndices[record].frameCount > 1;
                }
                catch (IndexOutOfRangeException)
                {
                    if (verboseLogs) Debug.Log($"[VE-AP] Caught IndexOutOfRangeException: Record index {record} is out of bounds for atlas indices.");
                    return null;
                }
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
                if (material == null)
                {
                    Debug.LogError("[VE-AP] Failed to get default material.");
                    return null;
                }
                mesh = dfUnity.MeshReader.GetBillboardMesh(
                    summary.Rect,
                    archive,
                    record,
                    out size);
                summary.AtlasedMaterial = false;
                summary.AnimatedMaterial = false;
            }

                // Debug logs for size and scale
                if(verboseLogs) Debug.Log($"[VE-AP] Mesh Size: {size}, Scale: {scale}, originalScale: {originalScale}");

                if (!useExactDimensions)
                {
                    if (verboseLogs) Debug.Log($"VE-AP: Rescaling Archive {Archive}, Record {Record} based on originalScale {originalScale}");
                    Transform transform = GetComponent<Transform>();

                    // Set scale to originalScale for both x and y
                    transform.localScale = new Vector3(originalScale, originalScale, transform.localScale.z); // Placeholder value tk

                    // Optional: You can leave UV adjustment if necessary
                    Vector2 uv = Vector2.zero;  // Set default UVs
                    summary.Rect = new Rect(uv.x, uv.y, 1 - 2 * uv.x, 1 - 2 * uv.y);
                    AlignToBase();
                }

                if (xml != null)
                {
                    if(verboseLogs) Debug.Log("VE-AP: Rescaling based on XML from GetCustomBillboardMaterial");
                    Transform transform = GetComponent<Transform>();
                    scale = xml.GetVector2("scaleX", "scaleY", Vector2.one);
                    transform.localScale = new Vector3(scale.x, scale.y, transform.localScale.z);
                    Vector2 uv = xml.GetVector2("uvX", "uvY", Vector2.zero);
                    summary.Rect = new Rect(uv.x, uv.y, 1 - 2 * uv.x, 1 - 2 * uv.y);
                }
                else
                {
                    if(verboseLogs) Debug.Log($"VE-AP: XML data was not provided or found in GetCustomBillboardMaterial");
                }

            // Set summary
            summary.FlatType = MaterialReader.GetFlatType(archive);
            summary.Size = size;

            // Don't set summary archive and record yet
            // We don't want mods like R&R acting on our billboard

            // Set editor flat types
            if (summary.FlatType == FlatTypes.Editor)
                summary.EditorFlatType = MaterialReader.GetEditorFlatType(record);

            // Set NPC flat type based on archive
            if (RDBLayout.IsNPCFlat(archive))
                summary.FlatType = FlatTypes.NPC;

            // Assign mesh and material
            Mesh oldMesh = meshFilter.sharedMesh;
            if (mesh)
            {
                meshFilter.sharedMesh = mesh;
                meshRenderer.sharedMaterial = material;
            }
            if (oldMesh)
            {
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
                if (col == null)
                    col = gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
            }

            if(verboseLogs) Debug.Log($"[VE-AP] Successfully set material for archive={archive}, record={record}, size={summary.Size}, scale={scale}");
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

            if(verboseLogs) Debug.Log($"[VE-AP] AlignToBase on {Archive}-{Record}");

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

        static bool IsBadTextures(List<Texture2D> textures)
        {
            return textures == null || textures.Any(texture => texture == null);
        }

        Material GetCustomBillboardMaterial(int archive, int record, string prefix, ref BillboardSummary summary, out Vector2 scale)
        {
            scale = Vector2.one;

            string firstFrameName = $"{archive}{prefix}_{record}-0";
            string xmlFileName = $"{archive}_{record}-0";

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
                {
                    Debug.LogError($"[VE-AP] Could not load frames for '{firstFrameName}'");
                    return null;
                }

                textureCache.Add(firstFrameName, albedo);
            }
            else if(IsBadTextures(albedo))
            {
                // Reload textures and replace

                albedo = new List<Texture2D>();

                string frameName = firstFrameName;
                int frame = 0;
                while (ModManager.Instance.TryGetAsset(frameName, clone: false, out Texture2D frameAsset))
                {
                    albedo.Add(frameAsset);
                    frameName = $"{archive}{prefix}_{record}-{++frame}";
                }

                if (frame == 0)
                {
                    Debug.LogError($"[VE-AP] Could not load frames for '{firstFrameName}'");
                    return null;
                }

                textureCache[firstFrameName] = albedo;
            }

            summary.ImportedTextures.HasImportedTextures = true;
            summary.ImportedTextures.Albedo = albedo;
            summary.ImportedTextures.FrameCount = albedo.Count;

            if(verboseLogs) Debug.Log("VE-AP: Attempting to rescale based on XML");
            // Read XML configuration for scaling and UV
            Vector2 uv = Vector2.zero;
            if (XMLManager.TryReadXml(TextureReplacement.TexturesPath, xmlFileName, out xml))
            {
                if(verboseLogs) Debug.Log("VE-AP: Rescaling based on XML");
                scale = xml.GetVector2("scaleX", "scaleY", Vector2.one);
                uv = xml.GetVector2("uvX", "uvY", uv);
            }

            // Make material
            Material material = MaterialReader.CreateBillboardMaterial();
            summary.Rect = new Rect(uv.x, uv.y, 1 - 2 * uv.x, 1 - 2 * uv.y);

            // Set textures on material
            material.SetTexture(Uniforms.MainTex, albedo[0]);

            return material;
        }
    }
}
