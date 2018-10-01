/// <summary>
/// Handles importing object from schema into Unity and handles exporting of objects from Unity into schema
/// </summary>
public partial class UnityGLTFImporter
{
    public UnityGLTFImporter(
        IDataLoader dataLoader,
        UnityGLTFImporterConfig config = new UnityGLTFImporterConfig()
        );

    /// <summary>
    /// Creates a Unity GameObject from a glTF scene
    /// </summary>
    /// <param name="unityGLTFFObject">Object which contains information to parse</param>
    /// <param name="sceneId">Scene of the glTF to load</param>
    /// <returns>The created Unity object</returns>
    public Task<GameObject> ImportSceneAsync(
        UnityGLTFObject unityGLTFFObject,
        int sceneId = -1
        );

    /// <summary>
    /// Creates a Unity GameObject from a glTF node
    /// </summary>
    /// <param name="unityGLTFFObject">Object which contains information to parse</param>
    /// <param name="nodeId">Node of the glTF object to load.</param>        
    /// <returns>The created Unity object</returns>
    public Task<GameObject> ImportNodeAsync(
        UnityGLTFObject unityGLTFFObject,
        int nodeId
        );

    /// <summary>
    /// Creates a Unity Texture2D from a glTF texture
    /// </summary>
    /// <param name="unityGLTFFObject">Object which contains information to parse</param>
    /// <param name="textureId">Texture to load from glTF object.</param>        
    /// <returns>The created Unity object</returns>
    public Task<Texture2D> ImportTextureAsync(
        UnityGLTFObject unityGLTFFObject,
        int textureId
        );
}

// UnityNode.cs
public partial class UnityGLTFImporter
{
    private Task<GameObject> ConstructNode();
}

// UnityTexture.cs
public partial class UnityGLTFImporter
{
    private Task<Texture2D> ConstructTexture();
}

public class UnityGLTFImporterConfig
{
    public UnityGLTFImporterConfig();
    public UnityGLTFImporterConfig(GLTFExtensionRegistry registry, GLTFImportOptions importOptions);
}

/// <summary>
/// Rename of ILoader. Now returns tasks instead of IEnumerator.
/// Handles the reading in of data from a path
/// </summary>
public class IDataLoader
{
    Task<Stream> LoadStream(string uri);
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

public partial class UnityGLTFExporter
{
    /// <param name="dataWriter">Interface for handling the streams of data to write out</param>
    /// <param name="exportConfig">Configuration of extension settings and export otpions</param>
    public UnityGLTFImporter(
        IDataWriter dataWriter,
        UnityGLTFExporterConfig exportConfig = new UnityGLTFExporterConfig()
        );

    /// <summary>
    /// Exports a Unity object to a glTF file
    /// </summary>
    /// <param name="unityObject">The object to export</param>
    /// <returns></returns>
    public Task<GLTFFObject> Export(
        GameObject unityObject
        );
}

/// <summary>
/// Writes data for an export operation
/// </summary>
public class IDataWriter
{
    Task<bool> WriteStream(string uri, Stream stream);
}

public class UnityGLTFExporterConfig
{
    public UnityGLTFExporterConfig();
    public UnityGLTFExporterConfig(GLTFExtensionRegistry registry, GLTFExportOptions exportOptions);
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

    Task<ExtensionReturnObject<GameObject>> CreateSceneAsync(GLTF.Schema.GLTFScene gltfScene, int indexToLoad);
    Task<ExtensionReturnObject<GameObject>> CreateNodeAsync(Node gltfNode, int indexToLoad);
    Task<ExtensionReturnObject<MeshPrimitive>> CreateMeshPrimitiveAsync(GLTFMesh mesh, ILoaderContext loaderContext, int indexToLoad);
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

    UnityGLTFImporter gltfImporter = new UnityGLTFImporter(dataLoader);
    await gltfImporter.ImportSceneAsync(sampleObject);
    await gltfImporter.ImportSceneAsync(boxObject);
}