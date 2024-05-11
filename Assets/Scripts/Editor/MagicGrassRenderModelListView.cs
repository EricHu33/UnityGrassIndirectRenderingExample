using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections;
using MagicGrass;

public class MagicGrassRenderModelListView : VisualElement
{
    private SerializedProperty _magicGrassRenderModelsProperty;
    private float _lastRebuildTimestamp;
    private void TryRebuild(IndirectDrawController parent)
    {
        if (Time.time - _lastRebuildTimestamp > 0.01f)
        {
            parent.ForceRebuildRenderers();
            _lastRebuildTimestamp = Time.time;
        }
    }

    public MagicGrassRenderModelListView(SerializedProperty magicGrassRenderModelsProperty, IList sources, IndirectDrawController parent)
    {
        if(sources == null || sources.Count == 0)
            return;
        foreach (var element in sources)
        {
            if (element == null)
            {
                Add(new IMGUIContainer(()=>EditorGUILayout.HelpBox($"One of the render model is empty! Assign a scriptable object on it", MessageType.Warning)));   
            return;
            }
        }
        _magicGrassRenderModelsProperty = magicGrassRenderModelsProperty;
        VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/magic_grass_model_list_item.uxml");
        // Set up the ListView
        var listView = new ListView(sources);
        listView.showFoldoutHeader = true;
        listView.showBoundCollectionSize = false;
        listView.style.unityBackgroundImageTintColor = listView.style.backgroundColor;
        listView.style.flexGrow = 1;
        listView.showBorder = true;
        listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        listView.selectionType = SelectionType.None;
        listView.makeItem = () => uxml.CloneTree();
       /* listView.Q<Button>("unity-list-view__add-button").clickable = new Clickable(() =>
        {
        //    _magicGrassRenderModelsProperty.arraySize++;
           // parent.AddModelFromEditor();
            // sources.Add(new IndirectDrawController.MagicGrassRenderModel());
            listView.RefreshItems();
        //    _magicGrassRenderModelsProperty.serializedObject.ApplyModifiedProperties();
        });*/
        listView.itemsRemoved += (list) =>
        {
            _magicGrassRenderModelsProperty.arraySize--;
            
            listView.RefreshItems();
          //  _magicGrassRenderModelsProperty.serializedObject.ApplyModifiedProperties();
            parent.ForceRebuildRenderers();
            parent.RemoveModelFromEditor();
        };
        listView.bindItem = (element, index) =>
        {
            var arrayData = new SerializedObject(_magicGrassRenderModelsProperty.GetArrayElementAtIndex(index).objectReferenceValue);
            var foldout = element.Q<Foldout>("Foldout");
            var modelId = index;
            var headerStr = "";
            switch (modelId % 4)
            {
                case 0: headerStr = "R - Channel Detail"; break;
                case 1: headerStr = "G - Channel Detail"; break;
                case 2: headerStr = "B - Channel Detail"; break;
                case 3: headerStr = "A - Channel Detail"; break;
            }
            foldout.text = headerStr;

            var mapSize = element.Q<EnumField>("MapSize");
            var mapProp = arrayData.FindProperty("MapSize");
            mapSize.BindProperty(mapProp);
            mapSize.RegisterValueChangedCallback((val) =>
            {
                if (val.previousValue != val.newValue)
                    TryRebuild(parent);
            });

            var randomSeed = element.Q<IntegerField>("RandomSeed");
            randomSeed.BindProperty(arrayData.FindProperty("RandomSeed"));
            randomSeed.RegisterValueChangedCallback((val) =>
            {
                if (val.previousValue != val.newValue)
                    TryRebuild(parent);
            });

            var mesh = element.Q<PropertyField>("Mesh");
            mesh.BindProperty(arrayData.FindProperty("Mesh"));
            mesh.RegisterValueChangeCallback((val) =>
            {

                TryRebuild(parent);
            });

            var mat = element.Q<PropertyField>("InstanceMaterial");
            mat.BindProperty(arrayData.FindProperty("InstanceMaterial"));
            mat.RegisterValueChangeCallback((val) =>
            {

                TryRebuild(parent);
            });


            var excludeLayer = element.Q<IMGUIContainer>("ExcludeLayer");
            excludeLayer.onGUIHandler = () =>
            {
                EditorGUILayout.BeginVertical();

                SerializedProperty arrayProperty = arrayData.FindProperty("ExcludeLayers");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(arrayProperty, new GUIContent("ExcludeLayers"));
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log("exclude layer change");
                    arrayProperty.serializedObject.ApplyModifiedProperties();
                    TryRebuild(parent);
                }
                EditorGUILayout.EndVertical();
            };

            var includeLayer = element.Q<IMGUIContainer>("IncludeLayer");
            includeLayer.onGUIHandler = () =>
            {
                EditorGUILayout.BeginVertical();

                SerializedProperty arrayProperty = arrayData.FindProperty("IncludeLayers");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(arrayProperty, new GUIContent("IncludeLayers"));
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log("include layer change");
                    arrayProperty.serializedObject.ApplyModifiedProperties();
                    TryRebuild(parent);
                }
                EditorGUILayout.EndVertical();
            };


            var spawnExcludeThreshold = element.Q<Slider>("SpawnExcludeThreshold");
            spawnExcludeThreshold.BindProperty(arrayData.FindProperty("SpawnExcludeThreshold"));

            var spawnIncludeThreshold = element.Q<Slider>("SpawnIncludeThreshold");
            spawnIncludeThreshold.BindProperty(arrayData.FindProperty("SpawnIncludeThreshold"));

            var grassDensity = element.Q<SliderInt>("GrassDensity");
            grassDensity.BindProperty(arrayData.FindProperty("GrassDensity"));
            grassDensity.RegisterValueChangedCallback((val) =>
            {
                TryRebuild(parent);
            });
        };
        Add(listView);
    }
}