using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using WASD.Runtime;

namespace WASD.Editors
{
    [CustomEditor(typeof(SimulatedNeonText))]
    public class SimulatedNeonTextEditor : Editor
    {
        #region Fields
        public SimulatedNeonText m_NeonTextSimul;
        private VisualElement _Root;
        private VisualElement _DefaultEditor;
        #endregion


        public override VisualElement CreateInspectorGUI()
        {
            _DefaultEditor =  base.CreateInspectorGUI();
            m_NeonTextSimul = target as SimulatedNeonText;

            _Root = new VisualElement();
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath: "Assets/Editor/WASD/SimulatedNeonTextEditor/SimulatedNeonTextEditor.uxml");
            visualTree.CloneTree(target: _Root);

            _Root.Q<IMGUIContainer>(name: "EditorContainer").onGUIHandler = () => DrawDefaultInspector();
            _Root.Q<Button>(name: "ApplyButton").clicked += m_NeonTextSimul.UpdateAllDisplayValues;

            return _Root;
        }
    }
}

