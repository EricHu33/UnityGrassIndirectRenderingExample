using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using UnityEngine.UI;
using Button = UnityEngine.UIElements.Button;
using MagicGrass;

public class TestClassA
{
}

[CustomEditor(typeof(IndirectDrawController))]
[CanEditMultipleObjects]
public class IndirectDrawControllerEditor : Editor
{
    SerializedProperty _renderersArray;
    List<Editor> _materialEditors = new List<Editor>();
    List<IMGUIContainer> subGUIContainer = new List<IMGUIContainer>();
    private VisualElement myRoot;

    private ListView m_ItemListView;
    private SerializedObject m_SerializedObject;
    private SerializedProperty m_ArrayProperty;
    private Button button;

    bool hasScheduleTask = false;

    public override VisualElement CreateInspectorGUI()
    {
        _materialEditors.Clear();
        myRoot = new VisualElement();
        var modelsProperty = serializedObject.FindProperty("_magicGrassRenderModels");
        
        ((IndirectDrawController)target).OnForceRebuild = RepaintIMGUI;

        var modelListView = new MagicGrassRenderModelListView(modelsProperty,
            ((IndirectDrawController)target).Models, ((IndirectDrawController)target));

        var imguiContainer = new IMGUIContainer(() =>
        {
           // imguiEditor.OnInspectorGUI();
           EditorGUI.BeginChangeCheck();
           EditorGUILayout.PropertyField(modelsProperty);
           if (EditorGUI.EndChangeCheck())
           {
               serializedObject.ApplyModifiedProperties();
               if (myRoot.Contains(modelListView))
               {
                   myRoot.Remove(modelListView);
                   modelListView = new MagicGrassRenderModelListView(modelsProperty,
                       ((IndirectDrawController)target).Models, ((IndirectDrawController)target));
                   myRoot.Add(modelListView);
               }
               
               Debug.Log("array change!");
           }
            ((IndirectDrawController)target).NeedSyncMaterial = true;
        });
        
        myRoot.Add(imguiContainer);
        if (!myRoot.Contains(modelListView))
        {
            myRoot.Add(modelListView);
        }

        return myRoot;
    }

    private void RepaintIMGUI()
    {
        var models = ((IndirectDrawController)target).Models;
        myRoot.schedule.Execute(() =>
        {
            foreach (var subGUI in subGUIContainer)
            {
                if (myRoot.Contains(subGUI))
                    myRoot.Remove(subGUI);
            }

            subGUIContainer.Clear();
            DrawAllMaterial(myRoot, (IndirectDrawController)target);
            hasScheduleTask = false;
        });
    }


    private void DrawAllMaterial(VisualElement root, IndirectDrawController target)
    {
        _materialEditors.Clear();
        foreach (var model in target.Models)
        {
            if (model.InstanceMaterial == null)
                continue;
            var editor = DrawMaterialEditor(model.InstanceMaterial);
            if (editor != null)
            {
                subGUIContainer.Add(editor);
                root.Add(editor);
            }
        }
    }


    private IMGUIContainer DrawMaterialEditor(Material mat)
    {
        if (mat == null)
            return null;
        var matEditor = Editor.CreateEditor(mat);
        _materialEditors.Add(matEditor);
        var output = new IMGUIContainer(() =>
        {
            matEditor.DrawHeader();
            EditorGUILayout.BeginVertical();
            matEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        });
        return output;
    }
}