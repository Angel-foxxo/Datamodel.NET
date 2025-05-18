using Datamodel.Format;
using System.Numerics;
using DMElement = Datamodel.Element;

namespace ValveResourceFormat.IO.ContentFormats.ValveMap;

/// <summary>
///  Valve Map (VMAP) format version 29.
/// </summary>
internal class CMapRootElement : DMElement
{
    public bool IsPrefab { get; set; }
    public int EditorBuild { get; set; } = 8600;
    public int EditorVersion { get; set; } = 400;
    public bool ShowGrid { get; set; } = true;
    public int SnapRotationAngle { get; set; } = 15;
    public float GridSpacing { get; set; } = 64;
    public bool Show3DGrid { get; set; } = true;
    [DMProperty(name: "itemFile")]
    public string ItemFile { get; set; } = string.Empty;
    public CStoredCamera DefaultCamera { get; set; } = [];
    [DMProperty(name: "3dcameras")]
    public CStoredCameras Cameras { get; set; } = [];
    public CMapWorld world { get; set; } = [];
    public CVisibilityMgr Visibility { get; set; } = [];
    [DMProperty(name: "mapVariables")]
    public CMapVariableSet MapVariables { get; set; } = [];
    [DMProperty(name: "rootSelectionSet")]
    public CMapSelectionSet RootSelectionSet { get; set; } = [];
    [DMProperty(name: "m_ReferencedMeshSnapshots")]
    public Datamodel.ElementArray ReferencedMeshSnapshots { get; set; } = [];
    [DMProperty(name: "m_bIsCordoning")]
    public bool IsCordoning { get; set; }
    [DMProperty(name: "m_bCordonsVisible")]
    public bool CordonsVisible { get; set; }
    [DMProperty(name: "nodeInstanceData")]
    public Datamodel.ElementArray NodeInstanceData { get; set; } = [];
}


internal class CStoredCamera : DMElement
{
    public Vector3 Position { get; set; } = new Vector3(0, -1000, 1000);
    public Vector3 LookAt { get; set; }
}


internal class CStoredCameras : DMElement
{
    [DMProperty(name: "activecamera")]
    public int ActiveCameraIndex { get; set; } = -1;
    public Datamodel.ElementArray Cameras { get; set; } = [];
}


internal abstract class MapNode : DMElement
{
    public Vector3 origin { get; set; }
    public Datamodel.QAngle angles { get; set; }
    public Vector3 scales { get; set; } = new Vector3(1, 1, 1);

    public int nodeID { get; set; }
    public ulong referenceID { get; set; }

    public Datamodel.ElementArray children { get; set; } = [];

    public bool editorOnly { get; set; }
    [DMProperty(name: "force_hidden")]
    public bool ForceHidden { get; set; }
    public bool transformLocked { get; set; }
    public Datamodel.StringArray variableTargetKeys { get; set; } = [];
    public Datamodel.StringArray variableNames { get; set; } = [];
}

internal class CMapPrefab : MapNode
{
    public bool FixupEntityNames { get; set; } = true;
    public bool LoadAtRuntime { get; set; }
    public bool LoadIfNested { get; set; } = true;
    public string TargetMapPath { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
}


internal abstract class BaseEntity : MapNode
{
    public DmePlugList RelayPlugData { get; set; } = [];
    public Datamodel.ElementArray ConnectionsData { get; set; } = [];
    [DMProperty(name: "entity_properties")]
    public EditGameClassProps EntityProperties { get; set; } = [];

    public BaseEntity WithProperty(string name, string value)
    {
        EntityProperties[name] = value;
        return this;
    }

    public BaseEntity WithProperties(params (string name, string value)[] properties)
    {
        foreach (var (name, value) in properties)
        {
            EntityProperties[name] = value;
        }

        return this;
    }

    public BaseEntity WithClassName(string className)
        => WithProperty("classname", className);
}


internal class DmePlugList : DMElement
{
    public Datamodel.StringArray Names { get; set; } = [];
    public Datamodel.IntArray DataTypes { get; set; } = [];
    public Datamodel.IntArray PlugTypes { get; set; } = [];
    public Datamodel.StringArray Descriptions { get; set; } = [];
}


internal class DmeConnectionData : DMElement
{
    public string OutputName { get; set; } = string.Empty;
    public int TargetType { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string InputName { get; set; } = string.Empty;
    public string OverrideParam { get; set; } = string.Empty;
    public float Delay { get; set; }
    public int TimesToFire { get; set; } = -1;
}

/// <summary>
///  A string->string dictionary. This stores entity KeyValues.
/// </summary>
internal class EditGameClassProps : DMElement
{
}

/// <summary>
/// The world entity.
/// </summary>

internal class CMapWorld : BaseEntity
{
    public int NextDecalID { get; set; }
    public bool FixupEntityNames { get; set; } = true;
    public string MapUsageType { get; set; } = "standard";

    public CMapWorld()
    {
        EntityProperties["classname"] = "worldspawn";
    }
}


internal class CVisibilityMgr : MapNode
{
    public Datamodel.ElementArray Nodes { get; set; } = [];
    public Datamodel.IntArray HiddenFlags { get; set; } = [];
}


internal class CMapVariableSet : DMElement
{
    public Datamodel.StringArray VariableNames { get; set; } = [];
    public Datamodel.StringArray VariableValues { get; set; } = [];
    public Datamodel.StringArray VariableTypeNames { get; set; } = [];
    public Datamodel.StringArray VariableTypeParameters { get; set; } = [];
    [DMProperty(name: "m_ChoiceGroups")]
    public Datamodel.ElementArray ChoiceGroups { get; set; } = [];
}


internal class CMapSelectionSet : DMElement
{
    public Datamodel.ElementArray Children { get; set; } = [];
    public string SelectionSetName { get; set; } = string.Empty;
    public CObjectSelectionSetDataElement SelectionSetData { get; set; } = [];

    public CMapSelectionSet() { }
    public CMapSelectionSet(string name)
    {
        SelectionSetName = name;
    }
}


internal class CObjectSelectionSetDataElement : DMElement
{
    public Datamodel.ElementArray SelectedObjects { get; set; } = [];
}


internal class CMapEntity : BaseEntity
{
    public Vector3 HitNormal { get; set; }
    public bool IsProceduralEntity { get; set; }
}


internal class CMapInstance : BaseEntity
{
    /// <summary>
    /// A target <see cref="CMapGroup"/> to instance. With custom tint and transform.
    /// </summary>
    public DMElement? Target { get; set; }
    public Datamodel.Color TintColor { get; set; } = new Datamodel.Color(255, 255, 255, 255);
}

internal class CMapGroup : MapNode
{
}


internal class CMapWorldLayer : CMapGroup
{
    public string WorldLayerName { get; set; } = string.Empty;
}


internal class CMapMesh : MapNode
{
    public string CubeMapName { get; set; } = string.Empty;
    public string LightGroup { get; set; } = string.Empty;
    [DMProperty(name: "visexclude")]
    public bool VisExclude { get; set; }
    [DMProperty(name: "renderwithdynamic")]
    public bool RenderWithDynamic { get; set; }
    public bool DisableHeightDisplacement { get; set; }
    [DMProperty(name: "fademindist")]
    public float FadeMinDist { get; set; } = -1;
    [DMProperty(name: "fademaxdist")]
    public float FadeMaxDist { get; set; }
    [DMProperty(name: "bakelighting")]
    public bool BakeLighting { get; set; } = true;
    [DMProperty(name: "precomputelightprobes")]
    public bool PrecomputeLightProbes { get; set; } = true;
    public bool RenderToCubemaps { get; set; } = true;
    public int DisableShadows { get; set; }
    public float SmoothingAngle { get; set; } = 40f;
    public Datamodel.Color TintColor { get; set; } = new Datamodel.Color(255, 255, 255, 255);
    [DMProperty(name: "renderAmt")]
    public int RenderAmount { get; set; } = 255;
    public string PhysicsType { get; set; } = "default";
    public string PhysicsGroup { get; set; } = string.Empty;
    public string PhysicsInteractsAs { get; set; } = string.Empty;
    public string PhysicsInteractWsith { get; set; } = string.Empty;
    public string PhysicsInteractsExclude { get; set; } = string.Empty;
    public CDmePolygonMesh meshData { get; set; } = [];
    public bool UseAsOccluder { get; set; }
    public bool PhysicsSimplificationOverride { get; set; }
    public float PhysicsSimplificationError { get; set; }
}


internal class CDmePolygonMesh : MapNode
{
    /// <summary>
    /// Index to one of the edges stemming from this vertex.
    /// </summary>
    public Datamodel.IntArray VertexEdgeIndices { get; set; } = [];

    /// <summary>
    /// Index to the <see cref="VertexData"/> streams.
    /// </summary>
    public Datamodel.IntArray VertexDataIndices { get; set; } = [];

    /// <summary>
    /// The destination vertex of this edge.
    /// </summary>
    public Datamodel.IntArray EdgeVertexIndices { get; set; } = [];

    /// <summary>
    /// Index to the opposite/twin edge.
    /// </summary>
    public Datamodel.IntArray EdgeOppositeIndices { get; set; } = [];

    /// <summary>
    /// Index to the next edge in the loop, in counter-clockwise order.
    /// </summary>
    public Datamodel.IntArray EdgeNextIndices { get; set; } = [];

    /// <summary>
    /// Per half-edge index to the adjacent face. -1 if void (open edge).
    /// </summary>
    public Datamodel.IntArray EdgeFaceIndices { get; set; } = [];

    /// <summary>
    /// Per half-edge index to the <see cref="EdgeData"/> streams.
    /// </summary>
    public Datamodel.IntArray EdgeDataIndices { get; set; } = [];

    /// <summary>
    /// Per half-edge index to the <see cref="FaceVertexData"/> streams.
    /// </summary>
    public Datamodel.IntArray EdgeVertexDataIndices { get; set; } = [];

    /// <summary>
    /// Per face index to one of the *inner* edges encapsulating this face.
    /// </summary>
    public Datamodel.IntArray FaceEdgeIndices { get; set; } = [];

    /// <summary>
    /// Per face index to the <see cref="FaceData"/> streams.
    /// </summary>
    public Datamodel.IntArray FaceDataIndices { get; set; } = [];

    /// <summary>
    /// List of material names. Indexed by the 'meshindex' <see cref="FaceData"/> stream.
    /// </summary>
    public Datamodel.StringArray Materials { get; set; } = [];

    /// <summary>
    /// Stores vertex positions.
    /// </summary>
    public CDmePolygonMeshDataArray VertexData { get; set; } = [];

    /// <summary>
    /// Stores vertex uv, normal, tangent, etc. Two per vertex (for each half?).
    /// </summary>
    public CDmePolygonMeshDataArray FaceVertexData { get; set; } = [];

    /// <summary>
    /// Stores edge data such as soft or hard normals.
    /// </summary>
    public CDmePolygonMeshDataArray EdgeData { get; set; } = [];

    /// <summary>
    /// Stores face data such as texture scale, UV offset, material, lightmap bias.
    /// </summary>
    public CDmePolygonMeshDataArray FaceData { get; set; } = [];

    public CDmePolygonMeshSubdivisionData SubdivisionData { get; set; } = [];
}


internal class CDmePolygonMeshDataArray : DMElement
{
    public int Size { get; set; }
    /// <summary>
    /// Array of <see cref="CDmePolygonMeshDataStream"/>.
    /// </summary>
    public Datamodel.ElementArray Streams { get; set; } = [];
}


internal class CDmePolygonMeshSubdivisionData : DMElement
{
    public Datamodel.IntArray SubdivisionLevels { get; set; } = [];
    /// <summary>
    /// Array of <see cref="CDmePolygonMeshDataStream"/>.
    /// </summary>
    public Datamodel.ElementArray Streams { get; set; } = [];
}


internal class CDmePolygonMeshDataStream<T> : DMElement
{
    public string StandardAttributeName { get; set; } = string.Empty;
    public string SemanticName { get; set; } = string.Empty;
    public int SemanticIndex { get; set; }
    public int VertexBufferLocation { get; set; }
    public int DataStateFlags { get; set; }
    public DMElement? SubdivisionBinding { get; set; }
    /// <summary>
    /// An int, vector2, vector3, or vector4 array.
    /// </summary>
    public required Datamodel.Array<T> Data { get; set; }
}
