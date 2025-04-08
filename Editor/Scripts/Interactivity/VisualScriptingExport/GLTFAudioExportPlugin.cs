using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.VisualScripting
{ 
    /// <summary>
    /// Pluging for the audio emitter extension
    /// </summary>
public class GLTFAudioExportPlugin : VisualScriptingExportPlugin
    {
        /// <summary>
        /// Plugin name
        /// </summary>
        public override string DisplayName => "GLTF_Audio_Export";

        /// <summary>
        /// Plugin descriptions
        /// </summary>
        public override string Description => "Exports KHR Audio source nodes(literals) from the visual scripting graph";

        /// <summary>
        /// Creates a audio export context with the save to external file arg as false.
		/// The second arugment option for the GLTDAudionExportContext tells it to 
		/// forces a save of the audio to external file by setting to true
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new GLTFAudioExportContext(this, false);
        }
    }

}