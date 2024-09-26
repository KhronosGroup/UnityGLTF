using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
    [CustomEditor(typeof(MaterialLibrary))]
    public class MaterialLibraryEditor: Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var v = new VisualElement();

            var materialLibrary = (MaterialLibrary) target;
            var assetPath = AssetDatabase.GetAssetPath(materialLibrary);
            
            void AddMaterial()
            {
                // parse the glTF file, append an entry to the materials list
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x is Material).Cast<Material>().ToList();
                var count = subAssets.Count;
                var newMaterial = new Material(Shader.Find("UnityGLTF/PBRGraph"));
                newMaterial.name = $"Material {count}";
                subAssets.Add(newMaterial);

                // write the file out again
                MaterialEditorBridge.SaveAssetWithMaterials(assetPath, subAssets);
                OnEnable();
                Repaint();
            };
            
            var btn = new Button(AddMaterial)
            {
                text = "Add Material"
            };
            var editBtn = new Button(() =>
            {
                var path = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(path)) return;
                EditorUtility.OpenWithDefaultApp(path);
            })
            {
                text = "Edit File"
            };
            v.Add(btn);
            v.Add(editBtn);
            
            var itemSize = 48;
            var list = new ListView();
            list.headerTitle = "Materials";
            list.showFoldoutHeader = true;
            list.showAddRemoveFooter = true;
            list.fixedItemHeight = itemSize;  
            list.makeItem = () => new MaterialElement(null, material =>
            {
                MaterialEditorBridge.SaveAssetWithMaterials(assetPath, AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x is Material).Cast<Material>().ToList());
            });
            list.bindItem = (e, i) =>
            {
                if (i >= materials.Length) return;
                if (materials == null) return;
                var mat = materials[i];
                if (!mat) return;
                var elem = e as MaterialElement;
                elem.SetMaterial(mat);
            };
            list.itemsSource = materials;
            list.itemsRemoved += (removedItems) =>
            {
                var removedMaterials = new List<Material>();
                foreach (var item in removedItems)
                {
                    var mat = materials[item];
                    if (mat) removedMaterials.Add(mat);
                }
                MaterialEditorBridge.SaveAssetWithMaterials(AssetDatabase.GetAssetPath(target), materials.Except(removedMaterials).ToList());
                OnEnable();
                Repaint();
            };
            list.itemsAdded += (addedItems) =>
            {
                var addedMaterials = new List<Material>();
                foreach (var item in addedItems)
                {
                    var mat = new Material(Shader.Find("UnityGLTF/PBRGraph"));
                    addedMaterials.Add(mat);
                }
                MaterialEditorBridge.SaveAssetWithMaterials(AssetDatabase.GetAssetPath(target), materials.Concat(addedMaterials).ToList());
                OnEnable();
                Repaint();
            };
            
            // v.Add(list);
            
            return v;
        }

        class MaterialElement: VisualElement
        {
            private Material material;
            public MaterialElement(Material mat, Action<Material> changeCallback)
            {
                var itemSize = 48;
                style.flexDirection = FlexDirection.Row;
                style.flexGrow = 0;
                style.marginRight = 8;
                
                var img = new Image
                {
                    style =
                    {
                        width = itemSize
                    }
                };
                Add(img);
                var lbl = new TextField()
                {
                    isDelayed = true,
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        height = itemSize
                    }
                };
                lbl.bindingPath = "m_Name";
                Add(lbl);
                
                // allow starting to drag on the image
                img.RegisterCallback<MouseDownEvent>(e =>
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { material };
                    DragAndDrop.StartDrag("Dragging Materials");
                    e.StopPropagation();
                });
                
                // on double click on the text, make the text editable
                lbl.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.clickCount != 2) return;
                });
                
                lbl.RegisterCallback<ChangeEvent<string>>(e =>
                {
                    material.name = e.newValue;
                    changeCallback?.Invoke(material);
                });
            }    
            
            public void SetMaterial(Material mat)
            {
                material = mat;
                var label = this.Q<TextField>();
                label.SetValueWithoutNotify(mat ? mat.name : "<empty>");
                var img = this.Q<Image>();
                if (!mat)
                {
                    img.image = null;
                    return;
                }
                var preview = AssetPreview.GetAssetPreview(mat);
                img.image = preview;
                if (!preview)
                {
                    var instanceId = mat.GetInstanceID();
                    void WaitForPreview()
                    {
                        if (AssetPreview.IsLoadingAssetPreview(instanceId)) return;
                        EditorApplication.update -= WaitForPreview;
                        img.image = AssetPreview.GetAssetPreview(mat);
                    }
                    EditorApplication.update += WaitForPreview; 
                }
            }
        }

        private Editor materialEditor;
        private Material[] materials;
        private void OnEnable()
        {
            if (!target) return;
            if (!(target is MaterialLibrary)) return;
            
            materials = AssetDatabase
                .LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(target))
                .Where(x => x is Material)
                .Cast<Material>()
                .ToArray();
            
            materialEditor = null;
        }
        
        public override void DrawPreview(Rect previewArea)
        {
            CreateCachedEditor(materials, typeof(MaterialEditor), ref materialEditor);
            if (!materialEditor) return;
            if (!materialEditor.target) return;
            foreach (var t in materialEditor.targets)
            {
                if (t) continue;
                OnEnable();
                return;
            }
            materialEditor.DrawPreview(previewArea);
        }
        
        public override bool HasPreviewGUI() => true;
    }
}