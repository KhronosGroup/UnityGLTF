/// <summary>
/// Handles importing object from schema into Unity and handles exporting of objects from Unity into schema
/// </summary>
public static class UnityGLTFLoader
{
    /// <summary>
    /// Creates a Unity GameObject from a glTF structure
    /// </summary>
    /// <param name="unityGLTFFObject">Object which contains information to parse</param>
    /// <param name="dataLoader">Previously ILoader, gets stream data</param>
    /// <param name="importOptions">Load options for file, such as whether to disable GameObject on load</param>
    /// <returns>The created Unity object</returns>
    public Task<GameObject> Import(
        UnityGLTFObject unityGLTFFObject,
        IDataLoader dataLoader,          
        GLTFImportOptions importOptions = new GLTFImportOptions()
        );

    /// <summary>
    /// Exports a Unity object to a glTF file
    /// </summary>
    /// <param name="unityObject">The object to export</param>
    /// <param name="dataWriter">Interface for handling the streams of data to write out</param>
    /// <param name="writeAsGLB">Whether to write the object out as a GLB</param>
    /// <param name="writeOptions">Write options for the file</param>
    /// <returns></returns>
    public Task<GLTFFObject> Export(
        GameObject unityObject,
        IDataWriter dataWriter,
        bool writeAsGLB,
        GLTFWriteOptions writeOptions = new GLTFWriteOptions()
        );  

    public bool AddExtension(
        IUnityGLTFExtension extension,
        int priority
        );

    /// <summary>Scheduler of tasks. Can be replaced with custom app implementation so app can handle background threads </summary>
    public static ITaskScheduler TaskScheduler { get; set; }
}

/// <summary>
/// Unity wrapper for glTF object schema class from GLTFSerialization
/// </summary>
public class UnityGLTFObject()
{
    /// <summary>
    /// Constructor for already parsed glTF or GLB
    /// </summary>
    /// <param name="gltfObject">Already parsed glTF or GLB</param>
    public UnityGLTFObject(GLTFObject gltfObject);

    /// <summary>
    /// Constructor for not yet parsed glTF or GLB
    /// The IDataReader will handle loading the data to load the file
    /// </summary>
    /// <param name="fileName">Name of file to load</param>
    public UnityGLTFObject(string fileName);
}

/// <summary>
/// Unity glTF extension wrapper
/// </summary>
public interface IUnityGLTFExtension
{
    IGLTFExtension GLTFExtension { get; };
    Func<Task<UnityGLTFObject, int, GameObject>> CreateSceneAsyncFunc { get; }
    Func<Task<UnityGLTFObject, int, GameObject>> CreateNodeAsyncFunc  { get; }
    Func<Task<UnityGLTFObject, int, MeshPrimitive>> CreateMeshPrimitiveAsyncFunc { get; }
    /// etc. 
}