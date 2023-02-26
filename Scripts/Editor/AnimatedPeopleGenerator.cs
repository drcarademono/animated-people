using Codice.CM.Common;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AnimatedPeople
{
    public class AnimatedPeopleGenerator : MonoBehaviour
    {
        struct ArchiveRecord
        {
            public int Archive;
            public int Record;
        }

        static readonly ArchiveRecord[] animatedRecords = new ArchiveRecord[]
        {
        new ArchiveRecord() { Archive=180, Record=0 }
        // 182_0 skipped
        , new ArchiveRecord() { Archive=182, Record=1 }
        , new ArchiveRecord() { Archive=182, Record=2 }
        , new ArchiveRecord() { Archive=182, Record=3 }
        , new ArchiveRecord() { Archive=182, Record=4 }
        , new ArchiveRecord() { Archive=182, Record=5 }
        , new ArchiveRecord() { Archive=182, Record=6 }
        , new ArchiveRecord() { Archive=182, Record=7 }
        , new ArchiveRecord() { Archive=182, Record=8 }
        , new ArchiveRecord() { Archive=182, Record=9 }
        , new ArchiveRecord() { Archive=182, Record=10 }
        , new ArchiveRecord() { Archive=182, Record=11 }
        , new ArchiveRecord() { Archive=182, Record=12 }
        , new ArchiveRecord() { Archive=182, Record=13 }
        , new ArchiveRecord() { Archive=182, Record=14 }
        , new ArchiveRecord() { Archive=182, Record=15 }
        , new ArchiveRecord() { Archive=182, Record=16 }
        , new ArchiveRecord() { Archive=182, Record=17 }
        , new ArchiveRecord() { Archive=182, Record=18 }
        , new ArchiveRecord() { Archive=182, Record=19 }
        , new ArchiveRecord() { Archive=182, Record=20 }
        // 182_21 skipped
        , new ArchiveRecord() { Archive=182, Record=22 }
        , new ArchiveRecord() { Archive=182, Record=23 }
        , new ArchiveRecord() { Archive=182, Record=24 }
        , new ArchiveRecord() { Archive=182, Record=25 }
        , new ArchiveRecord() { Archive=182, Record=26 }
        , new ArchiveRecord() { Archive=182, Record=27 }
        , new ArchiveRecord() { Archive=182, Record=28 }
        , new ArchiveRecord() { Archive=182, Record=29 }
        , new ArchiveRecord() { Archive=182, Record=30 }
        , new ArchiveRecord() { Archive=182, Record=31 }
        , new ArchiveRecord() { Archive=182, Record=32 }
        // 182_33 skipped
        , new ArchiveRecord() { Archive=182, Record=34 }
        , new ArchiveRecord() { Archive=182, Record=35 }
        // 182_36 skipped
        // 182_37 skipped
        , new ArchiveRecord() { Archive=182, Record=38 }
        , new ArchiveRecord() { Archive=182, Record=39 }
        , new ArchiveRecord() { Archive=182, Record=40 }
        , new ArchiveRecord() { Archive=182, Record=41 }
        , new ArchiveRecord() { Archive=182, Record=42 }
        , new ArchiveRecord() { Archive=182, Record=43 }
        , new ArchiveRecord() { Archive=182, Record=44 }
        , new ArchiveRecord() { Archive=182, Record=45 }
        , new ArchiveRecord() { Archive=182, Record=46 }
        // 182_47 skipped
        , new ArchiveRecord() { Archive=182, Record=48 }
        // 182_49 skipped
        // 182_50 skipped
        // 182_51 skipped
        // 182_52 skipped
        // 182_53 skipped
        , new ArchiveRecord() { Archive=182, Record=54 }
        , new ArchiveRecord() { Archive=182, Record=55 }
        , new ArchiveRecord() { Archive=182, Record=56 }
        , new ArchiveRecord() { Archive=182, Record=57 }
        , new ArchiveRecord() { Archive=182, Record=58 }
        , new ArchiveRecord() { Archive=183, Record=0 }
        , new ArchiveRecord() { Archive=183, Record=1 }
        , new ArchiveRecord() { Archive=183, Record=2 }
        , new ArchiveRecord() { Archive=183, Record=3 }
        , new ArchiveRecord() { Archive=183, Record=4 }
        , new ArchiveRecord() { Archive=183, Record=5 }
        , new ArchiveRecord() { Archive=183, Record=6 }
        , new ArchiveRecord() { Archive=183, Record=7 }
        , new ArchiveRecord() { Archive=183, Record=8 }
        , new ArchiveRecord() { Archive=183, Record=9 }
        , new ArchiveRecord() { Archive=183, Record=10 }
        , new ArchiveRecord() { Archive=183, Record=11 }
        , new ArchiveRecord() { Archive=183, Record=12 }
        , new ArchiveRecord() { Archive=183, Record=13 }
        , new ArchiveRecord() { Archive=183, Record=14 }
        , new ArchiveRecord() { Archive=183, Record=15 }
        , new ArchiveRecord() { Archive=183, Record=16 }
        , new ArchiveRecord() { Archive=183, Record=17 }
        , new ArchiveRecord() { Archive=183, Record=18 }
        , new ArchiveRecord() { Archive=183, Record=19 }
        , new ArchiveRecord() { Archive=183, Record=20 }
        , new ArchiveRecord() { Archive=183, Record=21 }
        , new ArchiveRecord() { Archive=184, Record=0 }
        , new ArchiveRecord() { Archive=184, Record=1 }
        // 184_2 skipped
        // 184_3 skipped
        , new ArchiveRecord() { Archive=184, Record=4 }
        , new ArchiveRecord() { Archive=184, Record=5 }
        , new ArchiveRecord() { Archive=184, Record=6 }
        , new ArchiveRecord() { Archive=184, Record=7 }
        , new ArchiveRecord() { Archive=184, Record=8 }
        , new ArchiveRecord() { Archive=184, Record=9 }
        , new ArchiveRecord() { Archive=184, Record=10 }
        , new ArchiveRecord() { Archive=184, Record=11 }
        , new ArchiveRecord() { Archive=184, Record=12 }
        , new ArchiveRecord() { Archive=184, Record=13 }
        , new ArchiveRecord() { Archive=184, Record=14 }
        // 184_15 skipped
        , new ArchiveRecord() { Archive=184, Record=16 }
        , new ArchiveRecord() { Archive=184, Record=17 }
        , new ArchiveRecord() { Archive=184, Record=18 }
        , new ArchiveRecord() { Archive=184, Record=19 }
        , new ArchiveRecord() { Archive=184, Record=20 }
        , new ArchiveRecord() { Archive=184, Record=21 }
        , new ArchiveRecord() { Archive=184, Record=22 }
        , new ArchiveRecord() { Archive=184, Record=23 }
        , new ArchiveRecord() { Archive=184, Record=24 }
        , new ArchiveRecord() { Archive=184, Record=25 }
        , new ArchiveRecord() { Archive=184, Record=26 }
        // 184_27 skipped
        , new ArchiveRecord() { Archive=184, Record=28 }
        , new ArchiveRecord() { Archive=184, Record=29 }
        , new ArchiveRecord() { Archive=184, Record=30 }
        , new ArchiveRecord() { Archive=184, Record=31 }
        , new ArchiveRecord() { Archive=184, Record=32 }
        , new ArchiveRecord() { Archive=184, Record=33 }
        , new ArchiveRecord() { Archive=334, Record=0 }
        , new ArchiveRecord() { Archive=334, Record=1 }
        , new ArchiveRecord() { Archive=334, Record=2 }
        , new ArchiveRecord() { Archive=334, Record=3 }
        , new ArchiveRecord() { Archive=334, Record=4 }
        , new ArchiveRecord() { Archive=334, Record=5 }
        , new ArchiveRecord() { Archive=334, Record=6 }
        , new ArchiveRecord() { Archive=334, Record=7 }
        , new ArchiveRecord() { Archive=334, Record=8 }
        , new ArchiveRecord() { Archive=334, Record=9 }
        , new ArchiveRecord() { Archive=334, Record=10 }
        // 334_11 skipped
        // 334_12 skipped
        , new ArchiveRecord() { Archive=334, Record=13 }
        // 334_14 skipped
        , new ArchiveRecord() { Archive=334, Record=15 }
        , new ArchiveRecord() { Archive=334, Record=16 }
        , new ArchiveRecord() { Archive=334, Record=17 }
        , new ArchiveRecord() { Archive=334, Record=18 }
        , new ArchiveRecord() { Archive=334, Record=19 }
        , new ArchiveRecord() { Archive=334, Record=20 }
        , new ArchiveRecord() { Archive=334, Record=21 }
        , new ArchiveRecord() { Archive=346, Record=0 }
        , new ArchiveRecord() { Archive=346, Record=1 }
        , new ArchiveRecord() { Archive=346, Record=2 }
        , new ArchiveRecord() { Archive=346, Record=3 }
        , new ArchiveRecord() { Archive=346, Record=4 }
        , new ArchiveRecord() { Archive=346, Record=5 }
        , new ArchiveRecord() { Archive=346, Record=6 }
        , new ArchiveRecord() { Archive=346, Record=7 }
        , new ArchiveRecord() { Archive=346, Record=8 }
        , new ArchiveRecord() { Archive=346, Record=9 }
        , new ArchiveRecord() { Archive=346, Record=10 }
        , new ArchiveRecord() { Archive=346, Record=11 }
        // 346_12 skipped
        , new ArchiveRecord() { Archive=346, Record=13 }
        , new ArchiveRecord() { Archive=346, Record=14 }
        , new ArchiveRecord() { Archive=346, Record=14 }
        , new ArchiveRecord() { Archive=346, Record=15 }
        , new ArchiveRecord() { Archive=346, Record=16 }
        , new ArchiveRecord() { Archive=346, Record=17 }
        // 346_18 skipped
        , new ArchiveRecord() { Archive=346, Record=19 }
        , new ArchiveRecord() { Archive=346, Record=20 }
        , new ArchiveRecord() { Archive=346, Record=21 }
        , new ArchiveRecord() { Archive=346, Record=22 }
        , new ArchiveRecord() { Archive=357, Record=0 }
        , new ArchiveRecord() { Archive=357, Record=1 }
        , new ArchiveRecord() { Archive=357, Record=2 }
        , new ArchiveRecord() { Archive=357, Record=3 }
        , new ArchiveRecord() { Archive=357, Record=4 }
        // 357_5 skipped
        , new ArchiveRecord() { Archive=357, Record=6 }
        , new ArchiveRecord() { Archive=357, Record=7 }
        // 357_8 skipped
        // 357_9 skipped
        , new ArchiveRecord() { Archive=357, Record=10 }
        , new ArchiveRecord() { Archive=357, Record=11 }
        , new ArchiveRecord() { Archive=357, Record=12 }
        , new ArchiveRecord() { Archive=357, Record=13 }
        , new ArchiveRecord() { Archive=357, Record=14 }
        , new ArchiveRecord() { Archive=357, Record=15 }
        };

        [MenuItem("Daggerfall Tools/Vanilla Enhanced/Generate Animated People")]
        static void Init()
        {
            string animatedPeoplePath = Path.Combine("Assets/Game/Mods/animated-people/Prefabs");
            if(!AssetDatabase.IsValidFolder(animatedPeoplePath))
                AssetDatabase.CreateFolder("Assets/Game/Mods/animated-people", "Prefabs");

            foreach (ArchiveRecord animatedRecord in animatedRecords)
            {
                string prefabName = $"{animatedRecord.Archive}_{animatedRecord.Record}";

                GameObject recordObject = new GameObject(prefabName);
                recordObject.AddComponent<MeshFilter>();
                var meshRender = recordObject.AddComponent<MeshRenderer>();
                meshRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var apBillboard = recordObject.AddComponent<AnimatedPeopleBillboard>();
                apBillboard.SecondsPerFrame = 0.2f;
                apBillboard.DelayMin = 3;
                apBillboard.DelayMax = 6;
                apBillboard.RepeatMin = 0;
                apBillboard.RepeatMax = 1;
                apBillboard.Archive = animatedRecord.Archive;
                apBillboard.Record = animatedRecord.Record;

                string prefabPath = Path.Combine(animatedPeoplePath, prefabName + ".prefab");
                AssetDatabase.DeleteAsset(prefabPath);
                PrefabUtility.SaveAsPrefabAsset(recordObject, prefabPath);

                DestroyImmediate(recordObject);
            }
        }
    }
}