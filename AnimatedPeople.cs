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
    public class AnimatedPeople : MonoBehaviour
    {
        public static Dictionary<uint, List<AnimatedPeopleBillboard.FlatReplacement>> flatReplacements;

        private static bool verboseLogs = true; // You can load this from settings if needed

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            // Initialize the flatReplacements dictionary
            flatReplacements = new Dictionary<uint, List<AnimatedPeopleBillboard.FlatReplacement>>();
            LoadFlatReplacements();
        }

        static void LoadFlatReplacements()
        {
            if (verboseLogs) Debug.Log("[VE-AP] Loading flat replacements.");

            const string replacementDirectory = "FlatReplacements";
            var replacementPath = Path.Combine(Application.streamingAssetsPath, replacementDirectory);
            if (!Directory.Exists(replacementPath))
            {
                if (verboseLogs) Debug.LogWarning($"[VE-AP] Replacement directory not found: {replacementPath}");
                return; // Don't do anything without this folder.
            }

            var replacementFiles = Directory.GetFiles(replacementPath);
            var serializer = new fsSerializer();

            foreach (var replacementFile in replacementFiles)
            {
                if (verboseLogs) Debug.Log($"[VE-AP] Reading replacement file: {replacementFile}");
                using (var streamReader = new StreamReader(replacementFile))
                {
                    var fsResult = fsJsonParser.Parse(streamReader.ReadToEnd(), out var fsData); // Parse whole file.
                    if (!fsResult.Equals(fsResult.Success))
                    {
                        if (verboseLogs) Debug.LogError($"[VE-AP] Failed to parse replacement file: {replacementFile}");
                        continue;
                    }

                    List<AnimatedPeopleBillboard.FlatReplacementRecord> replacementRecords = null;
                    serializer.TryDeserialize(fsData, ref replacementRecords).AssertSuccess();

                    // Load flat graphics
                    foreach (var record in replacementRecords)
                    {
                        int replaceTextureArchive = record.ReplaceTextureArchive;
                        int replaceTextureRecord = record.ReplaceTextureRecord;

                        // Check if ReplaceTextureArchive and ReplaceTextureRecord are -1
                        if (replaceTextureArchive == -1 && replaceTextureRecord == -1 && !string.IsNullOrEmpty(record.FlatTextureName))
                        {
                            // Try to parse the FlatTextureName if it's in the format "ReplaceTextureArchive_ReplaceTextureRecord-"
                            var flatTextureNameParts = record.FlatTextureName.Split('_', '-');
                            if (flatTextureNameParts.Length >= 2 &&
                                int.TryParse(flatTextureNameParts[0], out replaceTextureArchive) &&
                                int.TryParse(flatTextureNameParts[1], out replaceTextureRecord))
                            {
                                if (verboseLogs) Debug.Log($"[VE-AP] Parsed FlatTextureName: {record.FlatTextureName} as ReplaceTextureArchive={replaceTextureArchive}, ReplaceTextureRecord={replaceTextureRecord}");

                                // Assign the parsed values back to the record
                                record.ReplaceTextureArchive = replaceTextureArchive;
                                record.ReplaceTextureRecord = replaceTextureRecord;
                            }
                            else
                            {
                                // If parsing fails, ignore this entry
                                continue;
                            }
                        }

                        // Only process valid replacements
                        var isValidVanillaFlat = replaceTextureArchive > -1 && replaceTextureRecord > -1;

                        if (isValidVanillaFlat)
                        {
                            if (verboseLogs) Debug.Log($"[VE-AP] Adding valid vanilla flat replacement: {record.TextureArchive}-{record.TextureRecord}");
                            var key = ((uint)record.TextureArchive << 16) + (uint)record.TextureRecord; // Pack archive and record into single unsigned 32-bit integer
                            if (!flatReplacements.ContainsKey(key))
                                flatReplacements[key] = new List<AnimatedPeopleBillboard.FlatReplacement>();

                            flatReplacements[key].Add(new AnimatedPeopleBillboard.FlatReplacement() { Record = record });
                        }
                    }
                }
            }
        }
    }
}

