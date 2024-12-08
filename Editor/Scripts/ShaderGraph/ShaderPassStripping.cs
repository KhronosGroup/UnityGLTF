using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Rendering;

namespace UnityGLTF
{
    internal class ShaderPassStripping : IPreprocessShaders
    {
        private static readonly string[] builtInPasses = new[]
        {
            "BuiltIn Forward", 
            "BuiltIn ForwardAdd", 
            "BuiltIn Deferred",
        };
        
        private static readonly string[] urpDeferredPasses = new[]
        {
            "GBuffer", 
        };

        private static readonly string[] urpForwardPasses = new[]
        {
            "Universal ForwardAdd", 
            // Ignore ForwardOnly pass, because it can also be used for Deferred rendering!
        };

        
        // Use callbackOrder to set when Unity calls this shader preprocessor. Unity starts with the preprocessor that has the lowest callbackOrder value.
        public int callbackOrder => 0;
        public GLTFSettings.ShaderStrippingSettings settings;
        
        public ShaderPassStripping()
        {
            if (GLTFSettings.TryGetSettings(out var s))
                settings = s.shaderStrippingSettings;
        }

        private bool ShouldStripPass(ShaderSnippetData snippet)
        {
            if (settings.stripPasses.HasFlag(GLTFSettings.ShaderStrippingSettings.ShaderPassStrippingMode.URPDeferredPasses) && urpDeferredPasses.Contains(snippet.passName))
                return true;
            if (settings.stripPasses.HasFlag(GLTFSettings.ShaderStrippingSettings.ShaderPassStrippingMode.URPForwardPasses) && urpForwardPasses.Contains(snippet.passName))
                return true;
            if (settings.stripPasses.HasFlag(GLTFSettings.ShaderStrippingSettings.ShaderPassStrippingMode.BuiltInPasses) && builtInPasses.Contains(snippet.passName))
                return true;
            return false;
        }
        
        public void OnProcessShader(
            Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (!settings.stripPassesFromAllShaders && !shader.name.Contains("UnityGLTF/PBRGraph"))
                return;
            
            if (ShouldStripPass(snippet))
            {
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Stripping UnityGLTF shader: {0} with pass: {1}", shader.name, snippet.passName);
                data.Clear();
            }
        }
    }
}