using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityGLTF
{
    /// <summary>
    /// Editor for create ShaderVariantCollection Asset for the UnityGltf PBRGraph Shader
    /// </summary>

    public class ShaderVariantEditor : EditorWindow
    {
        private static readonly string[] predefinedFilters = new string[] {"ALPHA", "LIGHT,DIRECTIONAL,POINT", "SHADOW"};
        private Shader _pbrGraphShader;
        private Shader _unlitGraphShader;
        private Dictionary<string, bool> _pbrGraphKeywords = new Dictionary<string, bool>();
        private Dictionary<string, bool> _unlitGraphKeywords = new Dictionary<string, bool>();
        private Dictionary<string, bool> _pbrKeywords = new Dictionary<string, bool>();
        private Dictionary<string, bool> _unlitKeywords = new Dictionary<string, bool>();
        private List<string> _visible = new List<string>();
        private Vector2 _scrollPosition;
        private string _filter = "";
        private int _selectedShader;
        private bool _includePbrGraph = true;
        private bool _includeUnlitGraph = true;

        
        private static readonly string[] pbrKeywords = {
            "_CLEARCOAT_ON",
            "_IRIDESCENCE_ON",
            "_SPECULAR_ON",
            "_TEXTURE_TRANSFORM_ON",
            "_VOLUME_TRANSMISSION_ON",
            "_VOLUME_TRANSMISSION_ANDDISPERSION",
            "_SHEEN_ON",
            "INSTANCING_ON",
        };
        
        private static readonly string[] unlitKeywords = {
            "_TEXTURE_TRANSFORM_ON",
            "INSTANCING_ON",
        };
        
        [MenuItem("UnityGltf/ShaderVariantCollection Editor")]
        public static void OpenEditor()
        {
            // Open a ShaderVariantEditor instance
            var editor = EditorWindow.GetWindow<ShaderVariantEditor>();
            editor.titleContent = new GUIContent("PBR Graph Shader Variant Editor");
            editor.Show();
        }

        private void OnEnable()
        {
            _pbrGraphShader = Shader.Find("UnityGLTF/PBRGraph");
            _unlitGraphShader = Shader.Find("UnityGLTF/UnlitGraph");
            foreach (var l in _pbrGraphShader.keywordSpace.keywords.Select(k => k.name).Except(pbrKeywords))
                _pbrGraphKeywords.Add(l, false);

            foreach (var l in _unlitGraphShader.keywordSpace.keywords.Select(k => k.name).Except(pbrKeywords))
                _unlitGraphKeywords.Add(l, false);
            
            foreach (var p in pbrKeywords)
                _pbrKeywords.Add(p, true);
            
            foreach (var p in unlitKeywords)
                _unlitKeywords.Add(p, true);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("This editor is for the UnityGLTF PBRGraph/UnlitGraph Shader. " +
                                    "It will generate a ShaderVariantCollection Asset for the selected keywords.", MessageType.None);

            EditorGUILayout.BeginVertical(GUI.skin.window, GUILayout.MaxHeight(_pbrKeywords.Count % 3 * EditorGUIUtility.singleLineHeight));
            EditorGUILayout.Space(-10);
            _selectedShader = GUILayout.Toolbar(_selectedShader, new string[2] {"PBR Graph Shader", "Unlit Graph Shader"});
            Dictionary<string, bool> currentFeatureKeywords = null;
            currentFeatureKeywords = _selectedShader == 0 ? _pbrKeywords : _unlitKeywords;

            EditorGUILayout.LabelField("PBR Material Features");
            EditorGUI.indentLevel++;
            foreach (var k in currentFeatureKeywords)
            {
                GUI.color = k.Value ? Color.green : Color.white;
                if (GUILayout.Button(k.Key, GUILayout.Width(300)))
                {
                    currentFeatureKeywords[k.Key] = !currentFeatureKeywords[k.Key];
                    GUI.color = Color.white;
                    Repaint();
                    EditorGUILayout.EndVertical();
                    return;
                }
                GUI.color = Color.white;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Extra Shader Keywords");
            EditorGUILayout.HelpBox("It's not required to add Unity specific rendering keywords, only keywords that are directly related to the Material.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            
            _filter = EditorGUILayout.TextField("Filter", _filter);
            if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
            {
                _filter = "";
                EditorGUILayout.EndHorizontal();
                Repaint();
                return;
            }

            GUILayout.Label("Predefined Filters:", GUILayout.MaxWidth(100));
            foreach (var pf in predefinedFilters)
            {
                if (GUILayout.Button(pf))
                {
                    _filter = pf;
                    EditorGUILayout.EndHorizontal();
                    Repaint();
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            var filters = _filter.Split(",").Select( f => f.Trim().ToUpper()).ToArray();
            _visible.Clear();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            EditorGUI.indentLevel++;

            Dictionary<string, bool> currentKeywords = null;
            currentKeywords = _selectedShader == 0 ? _pbrGraphKeywords : _unlitGraphKeywords;
            
            foreach (var k in currentKeywords)
            {
                var upperKey = k.Key.ToUpper();
                
                if (filters.Length > 0 && !filters.Any(f => upperKey.Contains(f)))
                    continue;
                
                _visible.Add(k.Key);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(k.Key, GUILayout.Width(500));
                bool keywordActive = EditorGUILayout.Toggle("", k.Value);
                if (keywordActive != k.Value)
                {
                    currentKeywords[k.Key] = keywordActive;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();
                    Repaint();
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                foreach (var v in _visible)
                    currentKeywords[v] = true;
            }
            if (GUILayout.Button("Deselect All"))
            {
                foreach (var v in _visible)
                    currentKeywords[v] = false;
            }
            EditorGUILayout.EndHorizontal();

            
            var activeKeywords = _pbrGraphKeywords.Sum( k => k.Value ? 1 : 0);
            activeKeywords += _pbrKeywords.Sum( k => k.Value ? 1 : 0);
            float countPbr = Mathf.Pow(2, activeKeywords);
            
            var activeKeywordsUnlit = _unlitGraphKeywords.Sum( k => k.Value ? 1 : 0);
            activeKeywordsUnlit += _unlitKeywords.Sum( k => k.Value ? 1 : 0);
            float countUnlit = Mathf.Pow(2, activeKeywordsUnlit);
    
            EditorGUILayout.LabelField("Total Variants: PBR Graph " + countPbr+ " | Unlit Graph " + countUnlit);

            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Save", GUILayout.Height(EditorGUIUtility.singleLineHeight*2f)))
            {
                SaveShaderVariantCollection();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            _includePbrGraph = EditorGUILayout.ToggleLeft("Include PBR Graph", _includePbrGraph);
            _includeUnlitGraph = EditorGUILayout.ToggleLeft("Include Unlit Graph", _includeUnlitGraph);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Don't forget to add the created ShaderVariantCollection Asset to the Project Settings/Graphics > Shader Loading > Preloaded Shaders Graph.", MessageType.Info);
        }
        
        private void SaveShaderVariantCollection()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Variant Collection to", "pbrGraphVariantCollection", "asset", "File");
            if (string.IsNullOrEmpty(path))
                return;
            
            var svc = new ShaderVariantCollection();
            List<string> keywords = new List<string>();
       
            void AddVariantsForShader(Shader shader, List<string> keywords)
            {
                EditorUtility.DisplayProgressBar("Creating ShaderVariantCollection", "Prepare...", 0);
                float count = Mathf.Pow(2, keywords.Count);
                List<string> combination = new List<string>(keywords.Count);

                float updateTick = count / 1000;
                for (int i = 0; i < count; i++)
                {
                    if (i % updateTick == 0)
                        if (EditorUtility.DisplayCancelableProgressBar("Creating ShaderVariantCollection",
                                $"Variants {svc.variantCount}/{count - 1}", (float)i / count))
                        {
                            EditorUtility.ClearProgressBar();
                            return;
                        }

                    combination.Clear();
                    for (int j = 0; j < keywords.Count; j++)
                        if ((i & (1 << j)) != 0)
                            combination.Add(keywords[j]);

                    if (combination.Count > 0)
                    {
                        var sv = new ShaderVariantCollection.ShaderVariant();
                        sv.shader = shader;
                        sv.keywords = combination.ToArray();
                        svc.Add(sv);
                    }
                }
            }

            if (_includePbrGraph)
            {
                keywords.AddRange(_pbrKeywords.Concat(_pbrGraphKeywords)
                    .Where(kv => kv.Value)
                    .Select(kv => kv.Key));
                AddVariantsForShader(_pbrGraphShader, keywords);
            }

            if (_includeUnlitGraph)
            {
                keywords.Clear();
                keywords.AddRange(_unlitKeywords.Concat(_unlitGraphKeywords)
                    .Where(kv => kv.Value)
                    .Select(kv => kv.Key));
                AddVariantsForShader(_unlitGraphShader, keywords);
            }

            Debug.Log( "Total Variants Count: " + svc.variantCount);

            EditorUtility.DisplayProgressBar("Creating ShaderVariantCollection Asset", "Saving...", 0);
            Debug.Log("Saved Variant Collection to: " + path);
            AssetDatabase.CreateAsset(svc, path);
            EditorUtility.ClearProgressBar();
        }
        
    }
}