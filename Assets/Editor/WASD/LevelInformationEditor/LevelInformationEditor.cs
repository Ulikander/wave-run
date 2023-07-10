using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using WASD.Runtime.Levels;
using System;
using UnityEditor.UIElements;
using WASD.Enums;
using static WASD.Runtime.Levels.ObstaclePathData;
using static WASD.Runtime.Levels.LevelInformation;

namespace WASD.Editors
{
    [CustomEditor(typeof(LevelInformation))]
    public class LevelInformationEditor : Editor
    {
        #region Fields
        public LevelInformation m_Data;
        private VisualElement _Root;
        private VisualTreeAsset _LevelInfoPathModify;
        private VisualElement _CustomEditorContainer;
        private IMGUIContainer _DefaultEditorContainer;

        private IntegerField _LevelInfoCoreLevelValue;
        private ListView _LevelInfoData;
        private ScrollView _ObstaclePathDataLeft;
        private ScrollView _ObstaclePathDataRight;
        #endregion

        public override VisualElement CreateInspectorGUI()
        {
            m_Data = target as LevelInformation;

            return base.CreateInspectorGUI();
            
            _Root = new VisualElement();
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                assetPath: "Assets/Editor/WASD/LevelInformationEditor/LevelInformationEditor.uxml");
            visualTree.CloneTree(target: _Root);

            _LevelInfoPathModify = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                assetPath: "Assets/Editor/WASD/LevelInformationEditor/LevelInfo_PathRead.uxml");

            ConfigurePropertiesView();

            return _Root;
        }

        private void ConfigurePropertiesView()
        {
            //Editor view
            _Root.Q<Toggle>(name: "DefaultEditorToggle").RegisterValueChangedCallback(callback: DefaultInspectorToggle);
            _CustomEditorContainer = _Root.Q<VisualElement>(name: "CustomEditorContainer");
            _DefaultEditorContainer = _Root.Q<IMGUIContainer>(name: "DefaultEditorContainer");
            _DefaultEditorContainer.onGUIHandler = () => DrawDefaultInspector();

            //Properties
            _Root.Q<Button>(name: "ClearPropertiesButton").clicked += delegate
            {
                m_Data.ClearUnusedValues();
            };

            _LevelInfoCoreLevelValue = _Root.Q<IntegerField>(name: "CoreLevelValue");

            _Root.Q<EnumField>(name: "DifficultyRank").RegisterValueChangedCallback(callback: (evt) =>
            {
                _LevelInfoCoreLevelValue.style.display = m_Data.LevelDifficulty == LevelDifficulty.Core ? DisplayStyle.Flex : DisplayStyle.None;
            });
            _LevelInfoCoreLevelValue.style.display = m_Data.LevelDifficulty == LevelDifficulty.Core ? DisplayStyle.Flex : DisplayStyle.None;
            
            SetupLevelInformationDataList();
            //Visualizer
        }

        private void SetupLevelInformationDataList()
        {
            _LevelInfoData = _Root.Q<ListView>(name: "DataList");
            _LevelInfoData.fixedItemHeight = 190;
            _LevelInfoData.itemsSource = m_Data.Data;
            _LevelInfoData.makeItem = LevelInfoMakeItem;
            _LevelInfoData.bindItem = LevelInfoBindItem;
        }

        private void LevelInfoBindItem(VisualElement container, int index)
        {
            //Buttons
            Button moveUp = container.Q<Button>(name: "moveup");
            moveUp.clicked += delegate { MoveLevelInfoData(ref m_Data.Data, index, direction: -1); };
            moveUp.style.opacity = index == 0 ? 0.2f : 1f;

            Button moveDown = container.Q<Button>(name: "movedown");
            moveDown.clicked += delegate { MoveLevelInfoData(ref m_Data.Data, index, direction: 1); };
            moveDown.style.opacity = index >= m_Data.Data.Count - 1 ? 0.2f : 1f;

            container.Q<Button>(name: "remove").clicked += delegate { RemoveLevelInfoData(index: index); };

            //Containers
            void fValidateContainers()
            {
                container.Q<VisualElement>(name: "obstacles-container").style.display =
                    m_Data.Data[index].Type == LevelPathStep.Path ? DisplayStyle.Flex : DisplayStyle.None;
                container.Q<VisualElement>(name: "set-height-container").style.display =
                    m_Data.Data[index].Type == LevelPathStep.ChangeHeight ? DisplayStyle.Flex : DisplayStyle.None;
            }

            container.Q<EnumField>(name: "type").value = m_Data.Data[index].Type;
            container.Q<EnumField>(name: "type").RegisterValueChangedCallback(callback: (ctx) =>
            {
                m_Data.Data[index].Type = (LevelPathStep)ctx.newValue;
                fValidateContainers();
            });
            fValidateContainers();

            //Obstacles
            void fValidateObstacleSize()
            {
                container.Q<FloatField>(name: "size-custom").style.display = m_Data.Data[index].Size == LevelPathSize.Custom ? DisplayStyle.Flex : DisplayStyle.None;
            }

            container.Q<EnumField>(name: "size").value = m_Data.Data[index].Size;
            container.Q<EnumField>(name: "size").RegisterValueChangedCallback(callback: (ctx) =>
            {
                m_Data.Data[index].Size = (LevelPathSize)ctx.newValue;
                fValidateObstacleSize();
            });
            fValidateObstacleSize();

            container.Q<FloatField>(name: "size-custom").value = m_Data.Data[index].PathCustomSize;
            container.Q<FloatField>(name: "size-custom").RegisterValueChangedCallback(callback: (ctx) =>
            {
                m_Data.Data[index].PathCustomSize = ctx.newValue;
            });

            VisualElement fObstacleDataMakeItem()
            {
                PropertyField property = new();
                property.name = "property";
                return property;
            }

            _ObstaclePathDataLeft = container.Q<ScrollView>(name: "scroll-obstacles-left");
            _ObstaclePathDataLeft.Add(child: fObstacleDataMakeItem());
            _ObstaclePathDataRight = container.Q<ScrollView>(name: "scroll-obstacles-right");
            _ObstaclePathDataRight.Add(child: fObstacleDataMakeItem());


            container.Q<ObjectField>(name: "obstacles-asset").value = m_Data.Data[index].ObstaclePath;
            container.Q<ObjectField>(name: "obstacles-asset").RegisterValueChangedCallback(callback: (ctx) =>
            {
                m_Data.Data[index].ObstaclePath = ctx.newValue as ObstaclePathData;
                m_Data.Data[index].UseCustomObstaclePath = m_Data.Data[index].ObstaclePath == null;
                ValidateObstacleAsset(
                    propertyLeft: _ObstaclePathDataLeft.Q<PropertyField>(name: "property"),
                    propertyRight: _ObstaclePathDataRight.Q<PropertyField>(name: "property"),
                    pathDataIndex: index);
            });
            container.Q<Toggle>(name: "obstacles-invert").value = m_Data.Data[index].InvertObstacleValues;
            container.Q<Toggle>(name: "obstacles-invert").RegisterValueChangedCallback(callback: (ctx) =>
            {
                m_Data.Data[index].InvertObstacleValues = ctx.newValue;
            });

            ValidateObstacleAsset(
                    propertyLeft: _ObstaclePathDataLeft.Q<PropertyField>(name: "property"),
                    propertyRight: _ObstaclePathDataRight.Q<PropertyField>(name: "property"),
                    pathDataIndex: index);

            //Set Height
            /*
            container.Q<IntegerField>(name: "set-height-left").value = m_Data.Data[index].SetLeftSideHeight;
            container.Q<IntegerField>(name: "set-height-left").RegisterValueChangedCallback(callback: (ctx) =>
            {
                m_Data.Data[index].SetLeftSideHeight = ctx.newValue;
            });

            container.Q<IntegerField>(name: "set-height-right").value = m_Data.Data[index].SetRightSideHeight;
            container.Q<IntegerField>(name: "set-height-right").RegisterValueChangedCallback(callback: (ctx) =>
            {
                m_Data.Data[index].SetRightSideHeight = ctx.newValue;
            });
            */
            //Decorations
        }

       

        void ValidateObstacleAsset(PropertyField propertyLeft, PropertyField propertyRight, int pathDataIndex)
        {
            //also should check for  m_Data.Data[index].InvertObstacleValues;

            /*
            if (m_Data.Data[pathDataIndex].ObstaclePath == null)
            {
                propertyLeft.BindProperty(property: m_Data.Data[pathDataIndex].CustomLeftSide);
                propertyRight.BindProperty(property: m_Data.Data[pathDataIndex].CustomRightSide);
            }
            else
            {
                propertyLeft.BindProperty(property: m_Data.Data[pathDataIndex].ObstaclePath.LeftSide);
                propertyRight.BindProperty(property: m_Data.Data[pathDataIndex].ObstaclePath.RightSide);
            }
            */
        }

        private VisualElement LevelInfoMakeItem()
        {
            VisualElement newItem = new();
            _LevelInfoPathModify.CloneTree(target: newItem);
            newItem.style.flexShrink = 1f;
            newItem.AddToClassList(className: "border-red");
            return newItem;
        }

        private void DefaultInspectorToggle(ChangeEvent<bool> evt)
        {
            _CustomEditorContainer.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
            _DefaultEditorContainer.style.display = !evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
        }

        
        private void MoveLevelInfoData(ref List<PathData> data, int index, int direction)
        {
            if (direction == 0) return;
            if (index == 0 && direction == -1) return;
            if (index >= data.Count - 1 && direction == 1) return;

            var copy = new PathData(data[index]);
            int newIndex = index + direction;
            
            data.RemoveAt(index);
            data.Insert(newIndex, copy);
        }
        
        /*
        private void MoveLevelInfoData<T>(ref T[] array, int oldIndex, int direction)
        {
            if (oldIndex + direction < 0 || oldIndex + direction >= m_Data.Data.Count)
            {
                return;
            }

            int newIndex = oldIndex + direction;

            // TODO: Argument validation
            if (oldIndex == newIndex)
            {
                return; // No-op
            }
            T tmp = array[oldIndex];
            if (newIndex < oldIndex)
            {
                // Need to move part of the array "up" to make room
                Array.Copy(array, newIndex, array, newIndex + 1, oldIndex - newIndex);
            }
            else
            {
                // Need to move part of the array "down" to fill the gap
                Array.Copy(array, oldIndex + 1, array, oldIndex, newIndex - oldIndex);
            }
            array[newIndex] = tmp;

            _LevelInfoData.RefreshItems();
        }
        */

        private void RemoveLevelInfoData(int index)
        {
            List<PathData> newData = new(collection: m_Data.Data);
            newData.RemoveAt(index);

            m_Data.Data = newData;
            _LevelInfoData.RefreshItems();
        }
    }

    [CustomPropertyDrawer(typeof(Obstacle[]))]
    public class ObstacleDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return base.CreatePropertyGUI(property);
        }

        static void Convert(ClickEvent evt, SerializedProperty property)
        {

        }
    }

}
