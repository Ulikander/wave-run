using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using WASD.Enums;
using WASD.Runtime.Audio;

namespace WASD.Editors
{
    [CustomEditor(typeof(AudioContainer))]
    public class AudioContainerEditor : Editor
    {
        #region Fields
        public AudioContainer m_Container;
        private VisualElement _Root;
        private VisualElement _AudioBgmFields;
        private VisualElement _CustomLoopFields;
        #endregion


        public override VisualElement CreateInspectorGUI()
        {
            //return base.CreateInspectorGUI();
            m_Container = target as AudioContainer;

            _Root = new VisualElement();
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                assetPath: "Assets/Editor/WASD/AudioContainerEditor/AudioContainerEditor.uxml");
            visualTree.CloneTree(target: _Root);

            ConfigureElements();
            ValidateLoopType();
            ValidateBgmFields();
            return _Root;
        }

        private void ConfigureElements()
        {
            _AudioBgmFields = _Root.Q<VisualElement>(name: "BgmFields");
            _Root.Q<EnumField>("AudioType").RegisterValueChangedCallback(callback: (evt) =>
            {
                ValidateBgmFields();
            });

            _CustomLoopFields = _Root.Q<VisualElement>(name: "CustomLoopFields");
            _Root.Q<EnumField>(name: "LoopType").RegisterValueChangedCallback(callback: (evt) =>
            {
                ValidateLoopType();
            });
        }

        private void ValidateBgmFields()
        {
            _AudioBgmFields.style.display = m_Container.AudioType == AudioContainerType.BGM ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ValidateLoopType()
        {
            _CustomLoopFields.style.display = m_Container.LoopType == AudioLoopType.Custom ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

