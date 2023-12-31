using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using WASD.Runtime.Levels;


namespace WASD.Editors
{
    /*
    [CustomEditor(typeof(ObstaclePathData.Obstacle), true)]
    public class ObstacleDataEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement myInspector = new VisualElement();
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Editor/WASD/LevelInformationEditor/ObstacleDataEditor.uxml");
            visualTree.CloneTree(myInspector);
            return myInspector;
        }
    }
    */
    
    [CustomPropertyDrawer(typeof(ObstaclePathData.Obstacle))]
    public class ObstacleDataEditor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement myInspector = new VisualElement();
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Editor/WASD/LevelInformationEditor/ObstacleDataEditor.uxml");
            visualTree.CloneTree(myInspector);

            VisualElement positionVisual = myInspector.Q<VisualElement>("position-visual");
            myInspector.Q<Toggle>("position-auto-toggle").RegisterValueChangedCallback(ctx =>
            {
                positionVisual.style.display = !ctx.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            positionVisual.style.display = !property.FindPropertyRelative("AutomaticPosition").boolValue
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            
            return myInspector;
        }
    }
    
}
