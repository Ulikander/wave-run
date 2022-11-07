using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using WASD.Runtime.Levels;
using WASD.Runtime.Audio;
using System;

namespace WASD.Editors
{
    [CustomEditor(typeof(LevelInformation))]
    public class LevelInformationEditor : Editor
    {
        #region Fields
        public LevelInformation m_Data;
        private VisualElement _Root;
        private VisualElement _CustomEditorContainer;
        private IMGUIContainer _DefaultEditorContainer;
        #endregion

        public override VisualElement CreateInspectorGUI()
        {
            //return base.CreateInspectorGUI();
            _Root = new VisualElement();
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                assetPath: "Assets/Editor/WASD/LevelInformationEditor/LevelInformationEditor.uxml");
            visualTree.CloneTree(target: _Root);

            _Root.Q<Toggle>(name: "DefaultEditorToggle").RegisterValueChangedCallback(callback: DefaultInspectorToggle);
            _CustomEditorContainer = _Root.Q<VisualElement>(name: "CustomEditorContainer");
            _DefaultEditorContainer = _Root.Q<IMGUIContainer>(name: "DefaultEditorContainer");
            _DefaultEditorContainer.onGUIHandler = () => base.DrawDefaultInspector();

            return _Root;
        }

        private void DefaultInspectorToggle(ChangeEvent<bool> evt)
        {
            _CustomEditorContainer.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
            _DefaultEditorContainer.style.display = !evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

}
