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

namespace UnityGLTF.Interactivity.VisualScripting
{
    /// <summary>
    /// Audio GLTF export context 
    /// </summary>
    public class GLTFAudioExportContext : VisualScriptingExportContext
    {
        /// <summary>
        /// Contains summary information for an audio clip.
        /// </summary>
        public class AudioDescription
        {
            public int Id;
            public string Name;
            public AudioClip Clip;
        }

        // container for audio sources by description
        private static List<AudioDescription> _audioSourceIds = new();

        private const string AudioExtension = ".mp3";
        private const string AudioRelDirectory = "audio";
        private const string MimeTypeString = "audio/mpeg";

        private bool _saveAudioToFile = false;

        private List<ExportGraph> addedGraphs = new List<ExportGraph>();
        private List<UnitExporter> nodesToExport = new List<UnitExporter>();

        internal new ExportGraph currentGraphProcessing { get; private set; } = null;
        private GLTFRoot _gltfRoot = null;

        /// <summary>
        /// Default construction initializes the parent GLTFInteractivity export context.
        /// </summary>
        /// <param name="plugin"></param>
        public GLTFAudioExportContext(GLTFAudioExportPlugin plugin): base(plugin)
        {

        }

        /// <summary>
        /// Constructor sets the save to audio bool using the supplied argument
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="saveToExternalFile"></param>
        public GLTFAudioExportContext(GLTFAudioExportPlugin plugin, bool saveToExternalFile) : base(plugin)
        {
            _saveAudioToFile = saveToExternalFile;
        }

        public override void BeforeSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot) { }

        /// <summary>
        /// Called after the scene has been exported to add khr audio data.
        ///
        /// This overload of AfterSceneExport exposes the origins as a parameter to simplify tests.
        /// </summary>
        /// <param name="exporter"> GLTFSceneExporter object used to export the scene</param>
        /// <param name="gltfRoot"> Root GLTF object for the gltf object tree</param>
        /// <param name="visualScriptingComponents"> list of ScriptMachines in the scene.</param>
        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
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
                GetAudio(flowGraph);
            }
        }

        /// <summary>
        /// Gets the export graph which is not currently being used.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        internal ExportGraph GetAudio(FlowGraph graph)
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
                    AudioSource audio = null;
                    // If there is a connection, then we can return the value of the literal
                    if (literal.value is Component component && component is AudioSource)
                        audio = component.GetComponent<AudioSource>();
                    if (audio == null)
                        continue;

                    ProcessAudioSource(unit, audio);
                }
            }

            newExportGraph.nodes = GltfAudioNodeHelper.GetTranslatableNodes(topologicallySortedNodes, this);

            nodesToExport.AddRange(newExportGraph.nodes.Select(g => g.Value));

            currentGraphProcessing = lastCurrentGraph;
            return newExportGraph;
        }

        public static AudioDescription AddAudioSource(AudioSource audioSource)
        {
            AudioClip clip = audioSource.clip;
            string name = clip.name;

            foreach (var a in _audioSourceIds)
            {
                if (name == a.Name && clip == a.Clip)
                {
                    return a;
                }
            }
            AudioDescription ad = new AudioDescription() { Id = _audioSourceIds.Count, Name = clip.name, Clip = clip };
            _audioSourceIds.Add(ad);
            return ad;
        }

        /// <summary>
        /// Sets up and process the audio emitter data and sets the extension data for 
        /// the exporter to write to glb
        /// </summary>
        /// <param name="unit"> supplied iunit which is not currently used</param>
        /// <param name="audioSource">the audio source in the unity scene to parse</param>
        internal void ProcessAudioSource(IUnit unit, AudioSource audioSource)
        {
            List<AudioClip> audioDataClips = new List<AudioClip>();
            List<KHR_AudioEmitter> audioEmitters = new List<KHR_AudioEmitter>();

            var clip = audioSource.clip;

            var audioSourceId = AddAudioSource(audioSource);

            var emitterId = new AudioEmitterId
            {
                Id = audioSourceId.Id,
                Root = _gltfRoot
            };

            var emitter = new KHR_PositionalAudioEmitter()
            {
                type = "positional",
                sources = new List<AudioSourceId>() { new AudioSourceId() { Id = audioSourceId.Id, Root = _gltfRoot } },
                gain = audioSource.volume,
                minDistance = audioSource.minDistance,
                maxDistance = audioSource.maxDistance,
                distanceModel = PositionalAudioDistanceModel.linear
            };

            audioEmitters.Add(emitter);

            var path = AssetDatabase.GetAssetPath(clip);

            var fileName = Path.GetFileName(path);
            var settings = GLTFSettings.GetOrCreateSettings();


            string savePath = string.Empty;

            var audio = new KHR_AudioData();

            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);

            if (settings != null && !string.IsNullOrEmpty(settings.SaveFolderPath) && _saveAudioToFile)
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

                audio.uri = uriPath + fileName; //"." + Path.DirectorySeparatorChar + AudioRelDirectory + Path.DirectorySeparatorChar + fileName;
                audio.mimeType = MimeTypeString;
            }
            else
            {
                var result = exporter.ExportFile(fileName, "audio/mpeg", fileStream);
                audio.uri = result.uri;
                audio.mimeType = result.mimeType;
                audio.bufferView = result.bufferView;

            }

            var audioData = new List<KHR_AudioData>();

            audioData.Add(audio);

            var audioSources = new List<KHR_AudioSource>();

            var khrAudio = new KHR_AudioSource
            {
                audio = new AudioDataId { Id = audioSourceId.Id, Root = _gltfRoot },
                autoPlay = audioSource.playOnAwake,
                loop = audioSource.loop,
                gain = audioSource.volume,
                sourceName = Path.GetFileNameWithoutExtension(path)
            };

            audioSources.Add(khrAudio);

            var extension = new KHR_audio
            {
                audio = new List<KHR_AudioData>(audioData),
                sources = new List<KHR_AudioSource>(audioSources),
                emitters = new List<KHR_AudioEmitter>(audioEmitters),
            };

            if (_gltfRoot != null)
            {
                _gltfRoot.AddExtension(GltfAudioExtension.AudioExtensionName, (IExtension)extension);
                exporter.DeclareExtensionUsage(GltfAudioExtension.AudioExtensionName);
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



        //private static LinkedList<IUnit> TopologicalSort(IEnumerable<IUnit> nodes)
        //{
        //    var sorted = new LinkedList<IUnit>();
        //    var visited = new Dictionary<IUnit, bool>();

        //    void Visit(IUnit node)
        //    {
        //        bool inProcess;
        //        bool alreadyVisited = visited.TryGetValue(node, out inProcess);

        //        if (alreadyVisited)
        //        {
        //            if (inProcess)
        //            {
        //                // TODO: Should quit the topological sort and cancel the export
        //                // throw new ArgumentException("Cyclic dependency found.");
        //            }
        //        }
        //        else
        //        {
        //            visited[node] = true;

        //            // Get the dependencies from incoming connections and ignore self-references
        //            HashSet<IUnit> dependencies = new HashSet<IUnit>();
        //            foreach (IUnitConnection connection in node.connections)
        //            {
        //                if (connection.source.unit != node)
        //                {
        //                    dependencies.Add(connection.source.unit);
        //                }
        //            }

        //            foreach (IUnit dependency in dependencies)
        //            {
        //                Visit(dependency);
        //            }

        //            visited[node] = false;
        //            sorted.AddLast(node);
        //        }
        //    }

        //    foreach (var node in nodes)
        //    {
        //        Visit(node);
        //    }

        //    return sorted;
        //}

        // remaining context callbacks which are not currently being used.
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
