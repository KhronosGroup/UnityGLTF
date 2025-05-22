using System;
using System.IO;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityGLTF
{
    public static class ShaderModifier 
    {
        enum Mode
        {
            ClipSpace,
            WorldSpace,
        }
        
        public static Shader PatchShaderUVsToClipSpace(Shader shader, int uvChannel = 0)
        {
            var shaderSource = GetShaderSource(shader);
          
            var lastIndex = 0;
            var index = -1;
            var inserts = 0;
            
            var uvChannelName = $"texCoord{uvChannel}";
            
            var mode = Mode.WorldSpace;
            
            // Add  Pass - Conservative mode
            string passString = "Pass\n{";
            int addedConservativeToPasses = 0;
            int passIndex = 0;
            while (true)
            {
                passIndex = shaderSource.IndexOf(passString, passIndex + 1, StringComparison.Ordinal);
                if (passIndex == -1)
                    break;

                addedConservativeToPasses++;
                shaderSource = shaderSource.Insert(passIndex+passString.Length, "\n Conservative True\n");
            }
            Debug.Log($"<color=#808080ff>Shader {shader.name}: found {addedConservativeToPasses} passes to patch for >Conservative True<.</color>");

            
            while (true)
            {
                lastIndex = index;
                index = shaderSource.IndexOf("PackedVaryings PackVaryings (Varyings input)", lastIndex + 1, StringComparison.Ordinal);

                if (index == -1)
                    break;

                var indexOfReturn = shaderSource.IndexOf("return output;", index, StringComparison.Ordinal);

                if (indexOfReturn != -1)
                {
                    var foundTexCoord = shaderSource.IndexOf(uvChannelName, index, StringComparison.Ordinal);

                    if (foundTexCoord == -1 || foundTexCoord > indexOfReturn)
                    {
                        // TexCoord to PackedVaryings Struct
                        var shaderSourceUntilReturn = shaderSource.Substring(0, indexOfReturn);
                        
                        var structIndex = shaderSourceUntilReturn.LastIndexOf("struct PackedVaryings", shaderSourceUntilReturn.Length-1, StringComparison.Ordinal);
                        if (structIndex == -1)
                            continue;
                       
                        var structEndIndex = shaderSourceUntilReturn.IndexOf("}", structIndex, StringComparison.Ordinal); 
                        var structLength = structEndIndex - structIndex;
                        var structBlock = shaderSource.Substring(structIndex, structLength);
                        
                        bool structHasTex = structBlock.IndexOf($"float4 {uvChannelName}", StringComparison.Ordinal) != -1;
                   
                        if (!structHasTex)
                        {
                            int lastInterpolate = 0;
                            int interpolateIndex = 0;
                            do
                            {
                                interpolateIndex = structBlock.IndexOf(": INTERP", interpolateIndex, StringComparison.Ordinal);
                                if (interpolateIndex == -1)
                                    break;
                                int interpLength = ": INTERP".Length;
                                int lineEndIndex = structBlock.IndexOf(";", interpolateIndex, StringComparison.Ordinal);
                                int interpNumberLength = lineEndIndex - interpolateIndex - interpLength;
                                var interopNumber = structBlock.Substring(interpolateIndex + interpLength, interpNumberLength);
                                var interopInt = int.Parse(interopNumber);
                                lastInterpolate = Math.Max(lastInterpolate, interopInt);
                                interpolateIndex = lineEndIndex + 1;
                                
                            } while (interpolateIndex != -1);
                            
                            shaderSource = shaderSource.Insert(structIndex + structLength - 1,
                                $"\nfloat4 {uvChannelName} : INTERP{lastInterpolate+1};");
                            foundTexCoord = structIndex + structLength - 1;
                            inserts++;
                        }
                    }

                    if (foundTexCoord != -1 && foundTexCoord < indexOfReturn)
                    {
                        indexOfReturn = shaderSource.IndexOf("return output;", index, StringComparison.Ordinal);

                        switch (mode) {
                            case Mode.ClipSpace:
                                shaderSource = shaderSource.Insert(indexOfReturn - 1,
                                    "\nfloat4 p = output." + uvChannelName + ";" +
                                    "p.w = 1; p.z = 0.999999; p.xy -= -1; p.z *= -1;" +
                                    "output.positionCS = p;");
                                break;
                            case Mode.WorldSpace:
                                shaderSource = shaderSource.Insert(indexOfReturn - 1, $"\noutput.positionCS = TransformObjectToHClip(output.{uvChannelName});");
                                break;
                        }
                        inserts++;
                    }
                }

                index = indexOfReturn;
            }
            
            Debug.Log($"<color=#808080ff>Shader {shader.name}: found {inserts} to patch for {uvChannelName}.</color>");
            if (inserts < 1)
            {
                // For debugging, output the shader source to a file
                var sourcePath = AssetDatabase.GetAssetPath(shader);
                File.WriteAllText(sourcePath + ".txt", shaderSource);
            }
            return ShaderUtil.CreateShaderAsset(null, shaderSource, true);
        }
        
        public static string GetShaderSource(Shader shader)
        {
            var shaderPath = UnityEditor.AssetDatabase.GetAssetPath(shader);
            var assetCollection = new AssetCollection();
            
            // access private method "ShaderGraphImporter.GatherDependenciesFromSourceFile" by reflection
            var shaderGraphImporterType = typeof(ShaderGraphImporter);
            var gatherDependenciesMethod = shaderGraphImporterType.GetMethod("GatherDependenciesFromSourceFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (gatherDependenciesMethod == null)
            {
                Debug.LogError("GatherDependenciesFromSourceFile method not found");
                return null;
            }
            
            // call the method
            var parameters = new object[] { shaderPath };
            var dep = (string[]) gatherDependenciesMethod.Invoke(null, parameters);
            foreach (var d in dep)
            {
                var assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(d);
                if (assetType == typeof(SubGraphAsset)) 
                    assetCollection.AddAssetDependency(UnityEditor.AssetDatabase.GUIDFromAssetPath(d),AssetCollection.Flags.IsSubGraph );
            }

            return ShaderGraphImporter.GetShaderText(shaderPath, out _, assetCollection, out _);
        }
    }
}