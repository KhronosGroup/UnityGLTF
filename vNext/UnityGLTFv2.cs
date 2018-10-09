namespace UnityGLTF {

/// <summary>
/// Handles importing object from schema into Unity and handles exporting of objects from Unity into schema
/// </summary>
public virtual class Importer
{
    public Importer(
        IDataLoader dataLoader,
        ImporterConfig config = new ImporterConfig()
        );

    /// <summary>
    /// Creates a Unity GameObject from a glTF scene
    /// </summary>
    /// <param name="unityGLTFObject">Object which contains information to parse</param>
    /// <param name="sceneId">Scene of the glTF to load</param>
    /// <param name="progress">Progress of load completion</param>
    /// <returns>The created Unity object</returns>
    public virtual Task<GameObject> ImportSceneAsync(
        UnityGLTFObject unityGLTFObject,
        int sceneId = -1,
        IProgress<int> progress = null,
        CancellationToken cancellationToken = CancellationToken.None
        );

    /// <summary>
    /// Creates a Unity GameObject from a glTF node
    /// </summary>
    /// <param name="unityGLTFObject">Object which contains information to parse</param>
    /// <param name="nodeId">Node of the glTF object to load.</param>        
    /// <returns>The created Unity object</returns>
    public virtual Task<GameObject> ImportNodeAsync(
        UnityGLTFObject unityGLTFObject,
        int nodeId,
        CancellationToken cancellationToken = CancellationToken.None
        );

    /// <summary>
    /// Creates a Unity Texture2D from a glTF texture
    /// </summary>
    /// <param name="unityGLTFObject">Object which contains information to parse</param>
    /// <param name="textureId">Texture to load from glTF object.</param>        
    /// <returns>The created Unity object</returns>
    public virtual Task<Texture2D> ImportTextureAsync(
        UnityGLTFObject unityGLTFObject,
        int textureId,
        CancellationToken cancellationToken = CancellationToken.None,
        );
}

// UnityNode.cs
public virtual partial class Importer
{
    private virtual Task<GameObject> ConstructNode();
}

// UnityTexture.cs
public virtual partial class Importer
{
    private virtual Task<Texture2D> ConstructTexture();
}

public class ImporterConfig
{
    public ImporterConfig();
    public ImporterConfig(List<IUnityGLTFExtension> registry, GLTFImportOptions importOptions);
}

/// <summary>
/// Rename of ILoader. Now returns tasks instead of IEnumerator.
/// Handles the reading in of data from a path
/// </summary>
public class IDataLoader
{
    Task<Stream> LoadStreamAsync(string uri, CancellationToken ct = CancellationToken.None);
}

public class GLTFImportOptions
{
    /// <summary> Scheduler of tasks. Can be replaced with custom app implementation so app can handle background threads </summary>
    System.Threading.Tasks.TaskScheduler TaskScheduler { get; set; }

    /// <summary>Override for the shader to use on created materials </summary>
    string CustomShaderName { get; set; }

    /// <summary> Adds colliders to primitive objects when created </summary>
    ColliderType Collider { get; set; }
}

public partial class Exporter
{
    /// <param name="dataWriter">Interface for handling the streams of data to write out</param>
    /// <param name="progress">Progress of export</param>
    public Exporter(
        IDataWriter dataWriter,
        ExporterConfig exportConfig = new ExporterConfig()
        );

    /// <summary>
    /// Exports a Unity object to a glTF file
    /// </summary>
    /// <param name="unityObject">The object to export</param>
    /// <param name="exportConfig">Configuration of extension settings and export options</param>
    /// <returns>Strongly typed wrapper of exported object</returns>
    public Task<GLTFObject> ExportAsync(
        GameObject unityObject,
        IProgress<int> progress = null,
        CancellationToken ct = CancellationToken.None
        );
}

/// <summary>
/// Writes data for an export operation
/// </summary>
public class IDataWriter
{
    Task<bool> WriteStreamAsync(string uri, Stream stream, CancellationToken ct = CancellationToken.None);
}

public class ExporterConfig
{
    public ExporterConfig();
    public ExporterConfig(List<IUnityGLTFExtension> registry, GLTFExportOptions exportOptions);
}

public class GLTFExportOptions
{
    /// <summary>Whether to write the object out as a GLB</summary>
    bool ShouldWriteGLB { get; set ; }
}

/// <summary>
/// Unity wrapper for glTF object schema class from GLTFSerialization
/// Properly cleans up data
/// </summary>
public class UnityGLTFObject : IDisposable
{
    /// <summary>
    /// Constructor for already parsed glTF or GLB
    /// </summary>
    /// <param name="gltfObject">Already parsed glTF or GLB</param>
    public UnityGLTFObject(IGLTFObject gltfObject);

    /// <summary>
    /// Constructor for not yet parsed glTF or GLB
    /// The IDataReader will handle loading the data to load the file
    /// </summary>
    /// <param name="fileName">Name of file to load</param>
    public UnityGLTFObject(string fileName);

    internal AssetCache AssetCache { get; }
}

/// <summary>
/// Unity glTF extension wrapper
/// </summary>
public interface IUnityGLTFExtension
{
    IGLTFExtension GLTFExtension { get; };

    /// <summary>
    /// Creates a Unity object out of the glTF schema object
    /// </summary>
    /// <param name="importer">The importer that is used to load the object</param>
    /// <param name="unityGLTFObject">Object that is being loaded</param>
    /// <param name="sceneId">Index object which resolves to object in GLTFRoot</param>
    /// <returns>The loaded glTF scene as a GameObject hierarchy</returns>
    Task<ExtensionReturnObject<GameObject>> CreateSceneAsync(Importer importer, UnityGLTFObject unityGLTFObject, GLTF.Schema.SceneId sceneId);


    /// <summary>
    /// Creates a Unity object out of the node object
    /// </summary>
    /// <param name="importer">The importer that is used to load the object</param>
    /// <param name="unityGLTFObject">Object that is being loaded</param>
    /// <param name="nodeId">Node object from GLTFRoot</param>
    /// <returns>The loaded glTF node as a GameObject</returns>
    Task<ExtensionReturnObject<GameObject>> CreateNodeAsync(Importer importer,  UnityGLTFObject unityGLTFObject, GLTF.Schmea.NodeId nodeId);

    /// <summary>
    /// Creates a Mesh primitive out of a mesh primitive schema object
    /// </summary>
    /// <param name="importer">The importer that is used to load the object</param>
    /// <param name="unityGLTFObject">Object that is being loaded</param>
    /// <param name="meshId">Mesh object to load from</param>
    /// <param name="primitiveIndex">Primitive to load from the mesh</param>
    /// <returns>Returns the mesh primitive</returns>
    Task<ExtensionReturnObject<MeshPrimitive>> CreateMeshPrimitiveAsync(Importer importer, UnityGLTFObject unityGLTFObject, MeshId meshId, int primitiveIndex);
    /// etc. 
}

public struct ExtensionReturnObject<T>
{
    ExtensionContinuationBehavior ContinuationBehavior;
    T ReturnObject;
}

public enum ExtensionContinuationBehavior
{
    NotHandled,
    ContinueDefaultExecution,
    Handled
}

// Example calling pattern:
public async void LoadGLBs()
{
    UnityGLTFObject sampleObject = new UnityGLTFObject("http://samplemodels/samplemodel.glb");
    UnityGLTFObject boxObject = new UnityGLTFObject("http://samplemodels/box.glb");
    IDataLoader dataLoader = new WebRequestLoader();

    Importer gltfImporter = new Importer(dataLoader);
    await gltfImporter.ImportSceneAsync(sampleObject);
    await gltfImporter.ImportSceneAsync(boxObject);
}
}