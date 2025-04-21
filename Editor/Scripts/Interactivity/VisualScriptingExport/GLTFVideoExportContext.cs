using GLTF.Schema;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using System.Linq;
using UnityGLTF.Audio;
using UnityGLTF.Plugins.Experimental;
using System.IO;
using UnityEditor;
using UnityGLTF.Interactivity.VisualScripting.Export;
using UnityGLTF.Interactivity.Schema;
using UnityEngine.Video;

namespace UnityGLTF.Interactivity.VisualScripting
{
    /// <summary>
    /// Audio GLTF export context 
    /// </summary>
    public class GLTFVideoExportContext : VisualScriptingExportContext
    {
        /// <summary>
        /// Contains summary information for an audio clip.
        /// </summary>
        public class VideoDescription
        {
            public int Id;
            public string Name;
            public VideoClip Clip;
        }

        // container for audio sources by description
        private static List<VideoDescription> _videoSourceIds = new();

        private const string AudioExtension = ".mp4";
        private const string AudioRelDirectory = "video";
        private const string MimeTypeString = "video/mpeg";

        private bool _saveVideoToFile = false;

        private List<ExportGraph> addedGraphs = new List<ExportGraph>();
        private List<UnitExporter> nodesToExport = new List<UnitExporter>();

        internal new ExportGraph currentGraphProcessing { get; private set; } = null;
        private GLTFRoot _gltfRoot = null;

        /// <summary>
        /// Default construction initializes the parent GLTFInteractivity export context.
        /// </summary>
        /// <param name="plugin"></param>
        public GLTFVideoExportContext(GLTFVideoExportPlugin plugin): base(plugin)
        {

        }

        /// <summary>
        /// Constructor sets the save to audio bool using the supplied argument
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="saveToExternalFile"></param>
        public GLTFVideoExportContext(GLTFVideoExportPlugin plugin, bool saveToExternalFile) : base(plugin)
        {
            _saveVideoToFile = saveToExternalFile;
        }

        public override void BeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) 
        {
        }

        /// <summary>
        /// Called after the scene has been exported to add video data.
        ///
        /// This overload of AfterSceneExport exposes the origins as a parameter to simplify tests.
        /// </summary>
        /// <param name="exporter"> GLTFSceneExporter object used to export the scene</param>
        /// <param name="gltfRoot"> Root GLTF object for the gltf object tree</param>
        /// <param name="visualScriptingComponents"> list of ScriptMachines in the scene.</param>
        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            var scenes = gltfRoot.Scenes;
            var scriptMachines = new List<ScriptMachine>();

            foreach (var root in exporter.RootTransforms)
            {
                if (!root) continue;
                var machines = root
                    .GetComponentsInChildren<ScriptMachine>()
                    .Where(x => x.isActiveAndEnabled && x.graph != null);
                scriptMachines.AddRange(machines);
            }

            AfterSceneExport(exporter, gltfRoot, scriptMachines);

            // add new scenes audio emitter extension before process and JSON is written out.
            var v = new Dictionary<string, IExtension>();
            v.Add(GltfVideoExtension.VideoExtensionName, new GltfSceneVideoEmitterExtension() { videos = GetVideoSourceIndexes() });
            gltfRoot.Scenes.Add(new GLTFScene() { Extensions = v });

        }

        /// <summary>
        /// Called after the scene has been exported to add interactivity data.
        ///
        /// This overload of AfterSceneExport exposes the origins as a parameter to simplify tests.
        /// </summary>
        /// <param name="exporter"> GLTFSceneExporter object used to export the scene</param>
        /// <param name="gltfRoot"> Root GLTF object for the gltf object tree</param>
        /// <param name="visualScriptingComponents"> list of ScriptMachines in the scene.</param>
        internal new void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, List<ScriptMachine> visualScriptingComponents)
        {
            if (visualScriptingComponents.Count == 0)
            {
                return;
            }
            this.exporter = exporter;
            _gltfRoot = gltfRoot;
            foreach (var scriptMachine in visualScriptingComponents)
            {
                ActiveScriptMachine = scriptMachine;
                FlowGraph flowGraph = scriptMachine.graph;
                GetVideo(flowGraph);
            }
        }

        /// <summary>
        /// Gets the export graph which is not currently being used.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        internal ExportGraph GetVideo(FlowGraph graph)
        {
            var newExportGraph = new ExportGraph();
            newExportGraph.gameObject = ActiveScriptMachine.gameObject;
            newExportGraph.parentGraph = currentGraphProcessing;
            newExportGraph.graph = graph;
            addedGraphs.Add(newExportGraph);

            if (currentGraphProcessing != null)
                currentGraphProcessing.subGraphs.Add(newExportGraph);

            var lastCurrentGraph = currentGraphProcessing;
            currentGraphProcessing = newExportGraph;

            // Topologically sort the graph to establish the dependency order
            LinkedList<IUnit> topologicallySortedNodes = TopologicalSort(graph.units);
            IEnumerable<IUnit> sortedNodes = topologicallySortedNodes;
            foreach (var unit in sortedNodes)
            {
                if (unit is Literal literal)
                {
                    VideoPlayer video = null;
                    // If there is a connection, then we can return the value of the literal
                    if (literal.value is Component component && component is VideoPlayer)
                        video = component.GetComponent<VideoPlayer>();
                    if (video == null)
                        continue;

                    ProcessVideoSource(unit, video);
                }
            }

            newExportGraph.nodes = GltfAudioVideoNodeHelper.GetTranslatableNodes(topologicallySortedNodes, this);

            nodesToExport.AddRange(newExportGraph.nodes.Select(g => g.Value));

            currentGraphProcessing = lastCurrentGraph;
            return newExportGraph;
        }

        public static List<int> GetVideoSourceIndexes()
        {
            return (_videoSourceIds.Select(r => r.Id).ToList());
        }

        public static VideoDescription AddVideoSource(VideoPlayer videoPlayer)
        {
            VideoClip clip = videoPlayer.clip;
            string name = clip.name;

            foreach (var v in _videoSourceIds)
            {
                if (name == v.Name && clip == v.Clip)
                {
                    return v;
                }
            }
            VideoDescription ad = new VideoDescription() { Id = _videoSourceIds.Count, Name = clip.name, Clip = clip };
            _videoSourceIds.Add(ad);
            return ad;
        }

        /// <summary>
        /// Sets up and process the audio emitter data and sets the extension data for 
        /// the exporter to write to glb
        /// </summary>
        /// <param name="unit"> supplied iunit which is not currently used</param>
        /// <param name="audioSource">the audio source in the unity scene to parse</param>
        internal void ProcessVideoSource(IUnit unit, VideoPlayer videoPlayer)
        {
            List<VideoClip> videoDataClips = new List<VideoClip>();

            var clip = videoPlayer.clip;

            var audioSourceId = AddVideoSource(videoPlayer);

            var path = AssetDatabase.GetAssetPath(clip);

            var fileName = Path.GetFileName(path);
            var settings = GLTFSettings.GetOrCreateSettings();

            string savePath = string.Empty;

            var videoSource = new GOOG_VideoSource();

            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);

            if (settings != null && !string.IsNullOrEmpty(settings.SaveFolderPath) && _saveVideoToFile)
            {
                string uriPath;
                (uriPath, savePath) = GetRelativePath(settings.SaveFolderPath);

                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }
                savePath += (Path.DirectorySeparatorChar + fileName);
                using (var fileWriteStream = new FileStream(savePath, FileMode.Create))
                {
                    byte[] data = new byte[fileStream.Length];
                    fileStream.Read(data, 0, (int)fileStream.Length);

                    fileWriteStream.Write(data, 0, data.Length);
                }

                videoSource.uri = uriPath + fileName; //"." + Path.DirectorySeparatorChar + AudioRelDirectory + Path.DirectorySeparatorChar + fileName;
            }
            else
            {
                var result = exporter.ExportFile(fileName, "video/mpeg", fileStream);
                videoSource.mimeType = result.mimeType;
                videoSource.bufferView = result.bufferView;
            }

            var videoSources = new List<GOOG_VideoSource>();

            videoSources.Add(videoSource);

            var videoDatas = new List<GOOG_VideoData>();
            var videoData = new GOOG_VideoData()
            {
                name = videoPlayer.clip?.name,
                speed = videoPlayer.playbackSpeed,
                video = audioSourceId.Id,
                autoPlay = videoPlayer.playOnAwake
            };

            videoDatas.Add(videoData);

            var extension = new GOOG_Video
            {
                videoData = new List<GOOG_VideoData>(videoDatas),
                videoSource = new List<GOOG_VideoSource>(videoSources)
            };

            if (_gltfRoot != null)
            {
                _gltfRoot.AddExtension(GltfVideoExtension.VideoExtensionName, (IExtension)extension);
                exporter.DeclareExtensionUsage(GltfVideoExtension.VideoExtensionName);
            }
        }

        /// <summary>
        /// Gets the relative paths and save paths from the supplied full path arg
        /// </summary>
        /// <param name="fullSavePath">full save path</param>
        /// <returns></returns>
        internal (string, string) GetRelativePath(string fullSavePath)
        {
            string relPath = "." + Path.DirectorySeparatorChar + AudioRelDirectory + Path.DirectorySeparatorChar;
            string savePath = Path.GetFullPath(fullSavePath) + Path.DirectorySeparatorChar + AudioRelDirectory;
            
            return (relPath, savePath); 
        }

        /// <summary>
        /// We are running this, but the converters and graph are not currently used other than 
        /// the audio source literal elements
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static LinkedList<IUnit> TopologicalSort(IEnumerable<IUnit> nodes)
        {
            var sorted = new LinkedList<IUnit>();
            var visited = new Dictionary<IUnit, bool>();

            void Visit(IUnit node)
            {
                bool inProcess;
                bool alreadyVisited = visited.TryGetValue(node, out inProcess);

                if (alreadyVisited)
                {
                    if (inProcess)
                    {
                        // TODO: Should quit the topological sort and cancel the export
                        // throw new ArgumentException("Cyclic dependency found.");
                    }
                }
                else
                {
                    visited[node] = true;

                    // Get the dependencies from incoming connections and ignore self-references
                    HashSet<IUnit> dependencies = new HashSet<IUnit>();
                    foreach (IUnitConnection connection in node.connections)
                    {
                        if (connection.source.unit != node)
                        {
                            dependencies.Add(connection.source.unit);
                        }
                    }

                    foreach (IUnit dependency in dependencies)
                    {
                        Visit(dependency);
                    }

                    visited[node] = false;
                    sorted.AddLast(node);
                }
            }

            foreach (var node in nodes)
            {
                Visit(node);
            }

            return sorted;
        }

        public override void BeforeNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node) { }
        public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node){}
        public override bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode) => false;
        public override void AfterMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode) { }
        public override void BeforeTextureExport(GLTFSceneExporter exporter, ref GLTFSceneExporter.UniqueTexture texture, string textureSlot) { }
        public override void AfterTextureExport(GLTFSceneExporter exporter, GLTFSceneExporter.UniqueTexture texture, int index, GLTFTexture tex) { }
        public override void AfterPrimitiveExport(GLTFSceneExporter exporter, Mesh mesh, MeshPrimitive primitive, int index) { }
        public override void AfterMeshExport(GLTFSceneExporter exporter, Mesh mesh, GLTFMesh gltfMesh, int index) { }
    }
}
