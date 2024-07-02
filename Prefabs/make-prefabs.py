import os

# Function to create prefab content based on archive and record
def create_prefab_content(archive, record):
    content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &2662389662062758938
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 1536915003649397482}}
  - component: {{fileID: 4257095762411141614}}
  - component: {{fileID: 4717224951235134239}}
  - component: {{fileID: 4396741959773428355}}
  m_Layer: 0
  m_Name: {archive}_{record}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &1536915003649397482
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 2662389662062758938}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children: []
  m_Father: {{fileID: 0}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &4257095762411141614
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 2662389662062758938}}
  m_Mesh: {{fileID: 0}}
--- !u!23 &4717224951235134239
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 2662389662062758938}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
--- !u!114 &4396741959773428355
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 2662389662062758938}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 77c1a55b5a4eeca4384417a163af0953, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  summary:
    Size: {{x: 0, y: 0}}
    Rect:
      serializedVersion: 2
      x: 0
      y: 0
      width: 0
      height: 0
    AtlasRects: []
    AtlasIndices: []
    AtlasedMaterial: 0
    AnimatedMaterial: 0
    CurrentFrame: 0
    FlatType: 0
    EditorFlatType: 0
    IsMobile: 0
    Archive: {archive}
    Record: {record}
    Flags: 0
    FactionOrMobileID: 0
    NameSeed: 0
    FixedEnemyType: 0
    WaterLevel: 0
    CastleBlock: 0
  SecondsPerFrame: 0.1
  DelayMin: 0
  DelayMax: 0
  RepeatMin: 0
  RepeatMax: 0
  Archive: {archive}
  Record: {record}
"""
    return content

# Function to create prefab files from png filenames
def create_prefab_files():
    for filename in os.listdir():
        if filename.endswith(".png"):
            archive, record = filename.split('_')[0], filename.split('_')[1].split('-')[0]
            prefab_content = create_prefab_content(archive, record)
            prefab_filename = f"{archive}_{record}.prefab"
            with open(prefab_filename, "w") as prefab_file:
                prefab_file.write(prefab_content)

# Create prefab files
create_prefab_files()
