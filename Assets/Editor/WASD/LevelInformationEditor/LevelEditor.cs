using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using WASD.Enums;
using WASD.Runtime.Audio;
using WASD.Runtime.Levels;

namespace WASD.Editors
{
    [Serializable]
    public class LevelEditor : EditorWindow
    {
        #region Types

        private class LevelGeneral
        {
            public LevelInformation Level => LevelField.value as LevelInformation;
            public ObjectField LevelField;
            public ObjectField MusicField;
            public EnumField DifficultyField;
            public IntegerField CoreValueField;
            public VisualElement LevelVisual;
            public VisualElement CoreValueVisual;

            public LevelGeneral(VisualElement root)
            {
                MusicField = root.Q<ObjectField>("level_object_music");
                MusicField.RegisterValueChangedCallback(ctx =>
                {
                    if (Level == null) return;
                    Level.Music = ctx.newValue as AudioContainer;
                });

                DifficultyField = root.Q<EnumField>("level_difficulty");
                DifficultyField.RegisterValueChangedCallback(ctx =>
                {
                    if (Level == null) return;
                    Level.LevelDifficulty = (LevelDifficulty)ctx.newValue;
                });

                CoreValueField = root.Q<IntegerField>("level_core_value");
                CoreValueField.RegisterValueChangedCallback(ctx =>
                {
                    if (Level == null) return;
                    Level.CoreLevelValue = ctx.newValue;
                });

                CoreValueVisual = root.Q<VisualElement>("level_core_visual");

                LevelField = root.Q<ObjectField>("level_object");
                LevelField.RegisterValueChangedCallback(ctx =>
                {
                    if (Level == null) return;

                    MusicField.SetValueWithoutNotify(Level.Music);
                    DifficultyField.SetValueWithoutNotify(Level.LevelDifficulty);
                    CoreValueField.SetValueWithoutNotify(Level.CoreLevelValue);
                });

                LevelVisual = root.Q<VisualElement>("level-visual");
            }

            public void Update()
            {
                LevelVisual.style.visibility = Level != null ? Visibility.Visible : Visibility.Hidden;

                if (Level == null)
                {
                    MusicField.SetValueWithoutNotify(null);
                    DifficultyField.SetValueWithoutNotify(LevelDifficulty.Core);
                    CoreValueField.SetValueWithoutNotify(0);
                }
                else
                {
                    CoreValueVisual.style.visibility = Level.LevelDifficulty == LevelDifficulty.Core
                        ? Visibility.Visible
                        : Visibility.Hidden;
                }
            }
        }

        private class EditorMainContainers
        {
            public LevelInformation Level => LevelField.value as LevelInformation;
            public ObjectField LevelField;
            public VisualElement Main;
            public VisualElement PathFull;
            public VisualElement PathPath;
            public VisualElement PathHeight;

            private int _CurrentPathIndex;

            public EditorMainContainers(VisualElement root, ObjectField levelField)
            {
                LevelField = levelField;
                Main = root.Q<VisualElement>("editor-visual-full");
                PathFull = root.Q<VisualElement>("editor-visual-path");
                PathPath = root.Q<VisualElement>("editor-visual-path-path");
                PathHeight = root.Q<VisualElement>("editor-visual-path-height");
            }

            public void Update()
            {
                Main.style.visibility = Level != null ? Visibility.Visible : Visibility.Hidden;
                if (Level == null)
                {
                    PathPath.style.display = DisplayStyle.None;
                    PathHeight.style.display = DisplayStyle.None;
                    return;
                }

                PathFull.style.visibility = Level.Data is { Count: > 0 } ? Visibility.Visible : Visibility.Hidden;
                if (_CurrentPathIndex >= 0 && Level.Data != null && _CurrentPathIndex < Level.Data.Count)
                {
                    PathPath.style.display = Level.Data[_CurrentPathIndex].Type == LevelPathStep.Path
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                    PathHeight.style.display = Level.Data[_CurrentPathIndex].Type == LevelPathStep.ChangeHeight
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }
                else
                {
                    PathPath.style.display = DisplayStyle.None;
                    PathHeight.style.display = DisplayStyle.None;
                }
            }

            public void HandleCurrentPathIndexChange(int value)
            {
                _CurrentPathIndex = value;
            }
        }

        private class EditorPathSelect
        {
            public LevelInformation Level => LevelField.value as LevelInformation;
            public ObjectField LevelField;
            private int _CurrentPathIndex;

            private Label _MainLabel;
            private Button _AddNewBeforeButton;
            private Button _AddNewAfterButton;
            private Button _CopyButton;
            private Button _PasteButton;

            private IntegerField _GoToIndexField;
            private Button _PrevButton;
            private Button _NextButton;

            private Button _MoveBeforeButton;
            private Button _MoveAfterButton;

            private Button _RemoveButton;
            private Button _ClearUnusedDataButton;

            private StyleColor _EnabledColor;
            private StyleColor _DisabledColor;

            private LevelInformation.PathData _CopyData;

            public EditorPathSelect(
                VisualElement root,
                ObjectField levelField,
                Action<string> onRequestChangePathIndex,
                Action<bool> onRequestAddNew,
                Func<LevelInformation.PathData> onCopyData,
                Action<LevelInformation.PathData> onPasteData,
                Action<bool> moveData,
                Action removeData,
                Action clearUnusedData,
                Action<int> goToIndex)
            {
                _EnabledColor = new StyleColor(new Color(0.345098048f, 0.345098048f, 0.345098048f, 1f));
                _DisabledColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f, 1f));
                LevelField = levelField;
                
                _MainLabel = root.Q<Label>("editor-select-label");
                
                _AddNewBeforeButton = root.Q<Button>("editor-select-new-before");
                _AddNewBeforeButton.clicked += () => onRequestAddNew?.Invoke(true);
                
                _AddNewAfterButton = root.Q<Button>("editor-select-new-after");
                _AddNewAfterButton.clicked += () => onRequestAddNew?.Invoke(false);
                
                _CopyButton = root.Q<Button>("editor-select-copy");
                _CopyButton.clicked += () => _CopyData = onCopyData?.Invoke();
                
                _PasteButton = root.Q<Button>("editor-select-paste");
                _PasteButton.clicked += () => onPasteData?.Invoke(_CopyData);

                _GoToIndexField = root.Q<IntegerField>("editor-select-goto");
                _GoToIndexField.RegisterValueChangedCallback(ctx =>
                {
                    _GoToIndexField.SetValueWithoutNotify(-1);
                    goToIndex?.Invoke(ctx.newValue);
                });
                
                _PrevButton = root.Q<Button>("editor-select-prev");
                _PrevButton.clicked += () => onRequestChangePathIndex?.Invoke("prev");
                
                _NextButton = root.Q<Button>("editor-select-next");
                _NextButton.clicked += () => onRequestChangePathIndex?.Invoke("next");

                _MoveBeforeButton = root.Q<Button>("editor-select-moveup");
                _MoveBeforeButton.clicked += () => moveData?.Invoke(true);
                _MoveAfterButton = root.Q<Button>("editor-select-movedown");
                _MoveAfterButton.clicked += () => moveData?.Invoke(false);
                
                _RemoveButton = root.Q<Button>("editor-select-remove");
                _RemoveButton.clicked += removeData;
                
                _ClearUnusedDataButton = root.Q<Button>("editor-select-clear");
                _ClearUnusedDataButton.clicked += clearUnusedData;
            }

            private void SetButtonEnabled(Button button, bool value)
            {
                button.style.backgroundColor = value ? _EnabledColor : _DisabledColor;
                button.pickingMode = value ? PickingMode.Position : PickingMode.Ignore;
            }

            public void Update()
            {
                if (Level == null || Level.Data == null || _CurrentPathIndex == -1)
                {
                    SetButtonEnabled(_AddNewBeforeButton, false);
                    SetButtonEnabled(_AddNewAfterButton, true);
                    SetButtonEnabled(_CopyButton, false);
                    SetButtonEnabled(_PasteButton, false);
                    SetButtonEnabled(_PrevButton, false);
                    SetButtonEnabled(_MoveBeforeButton, false);
                    SetButtonEnabled(_MoveAfterButton, false);
                    SetButtonEnabled(_NextButton, false);
                    SetButtonEnabled(_RemoveButton, false);
                    SetButtonEnabled(_ClearUnusedDataButton, false);
                    _MainLabel.text = "Path Ids: (0/0)\nId: ...";
                    return;
                }

                _MainLabel.text = Level.Data is { Count: > 0 }
                    ? $"Path Ids: ({_CurrentPathIndex + 1}/{Level.Data.Count})\nId: {_CurrentPathIndex}"
                    : "Path Ids: (0/0)\nId: ...";

                if (Level.Data == null) return;

                SetButtonEnabled(_AddNewBeforeButton, _CurrentPathIndex > 0);
                SetButtonEnabled(_AddNewAfterButton, true);
                SetButtonEnabled(_CopyButton, true);
                SetButtonEnabled(_PasteButton, _CopyData != null);
                SetButtonEnabled(_PrevButton, _CurrentPathIndex > 0);
                SetButtonEnabled(_MoveBeforeButton, _CurrentPathIndex > 0);
                SetButtonEnabled(_MoveAfterButton, _CurrentPathIndex < Level.Data.Count - 1);
                SetButtonEnabled(_NextButton, _CurrentPathIndex < Level.Data.Count - 1);
                SetButtonEnabled(_RemoveButton, Level.Data.Count > 0);
                SetButtonEnabled(_ClearUnusedDataButton, true);
            }

            public void HandlePathIndexChange(int index)
            {
                _CurrentPathIndex = index;
            }
        }

        private class EditorValueFields
        {
            public event Action<int> OnEditValue;
            
            private LevelInformation Level => _LevelField.value as LevelInformation;
            private LevelInformation.PathData CurrentPathData => _CurrentPathIndex >= 0 ? Level.Data[_CurrentPathIndex] : null;
            private readonly ObjectField _LevelField;
            private int _CurrentPathIndex;

            private EnumField _PathTypeField;

            //Path
            private EnumField _PlatformSizeField;
            private VisualElement _PlatformCustomSizeVisual;
            private FloatField _PlatformCustomSizeField;
            private Toggle _UseCustomObstaclePathField;
            private ObjectField _PathObstacleDataField;
            private Toggle _InvertObstacleDataValuesToggle;
            private VisualElement _CustomObstacleDataVisual;
            private PropertyField _CustomObstacleDataLeft;
            private PropertyField _CustomObstacleDataRight;

            //Height
            private FloatField _LeftSideHeightField;
            private FloatField _RightSideHeightField;

            public EditorValueFields(VisualElement root, ObjectField levelField)
            {
                _LevelField = levelField;

                _PathTypeField = root.Q<EnumField>("editor-path-type");
                _PathTypeField.RegisterValueChangedCallback(ctx =>
                {
                    LevelPathStep value = (LevelPathStep)ctx.newValue;
                    CurrentPathData.Type = value;
                    OnEditValue?.Invoke(_CurrentPathIndex);
                });

                _PlatformSizeField = root.Q<EnumField>("editor-path-size-enum");
                _PlatformSizeField.RegisterValueChangedCallback(ctx =>
                {
                    LevelPathSize value = (LevelPathSize)ctx.newValue;
                    CurrentPathData.Size = value;
                    OnEditValue?.Invoke(_CurrentPathIndex);
                });

                _PlatformCustomSizeVisual = root.Q<VisualElement>("editor-path-visual-custom-size");

                _PlatformCustomSizeField = root.Q<FloatField>("editor-path-size-custom");
                _PlatformCustomSizeField.RegisterValueChangedCallback(ctx =>
                {
                    CurrentPathData.PathCustomSize = ctx.newValue;
                    OnEditValue?.Invoke(_CurrentPathIndex);
                });

                _UseCustomObstaclePathField = root.Q<Toggle>("editor-path-custom-obstacle-toggle");
                _UseCustomObstaclePathField.RegisterValueChangedCallback(ctx =>
                {
                    CurrentPathData.UseCustomObstaclePath = ctx.newValue;
                    OnEditValue?.Invoke(_CurrentPathIndex);
                });

                _PathObstacleDataField = root.Q<ObjectField>("editor-path-obstacle-data");
                _PathObstacleDataField.RegisterValueChangedCallback(ctx =>
                {
                    if (CurrentPathData != null)
                    {
                        ObstaclePathData value = ctx.newValue as ObstaclePathData;
                        CurrentPathData.ObstaclePath = value;
                        OnEditValue?.Invoke(_CurrentPathIndex);
                    }
                });

                _InvertObstacleDataValuesToggle = root.Q<Toggle>("editor-path-obstacle-invert");
                _InvertObstacleDataValuesToggle.RegisterValueChangedCallback(ctx =>
                {
                    CurrentPathData.InvertObstacleValues = ctx.newValue;
                    OnEditValue?.Invoke(_CurrentPathIndex);
                });

                _CustomObstacleDataVisual = root.Q<VisualElement>("editor-path-visual-custom-path-data");

                _CustomObstacleDataLeft = root.Q<PropertyField>("editor-path-obstacle-imgui-left");
                _CustomObstacleDataLeft.RegisterCallback<SerializedPropertyChangeEvent>(ctx =>
                {
                    OnEditValue?.Invoke(_CurrentPathIndex);
                });
               
                _CustomObstacleDataRight = root.Q<PropertyField>("editor-path-obstacle-imgui-right");
                _CustomObstacleDataRight.RegisterCallback<SerializedPropertyChangeEvent>(ctx =>
                {
                    OnEditValue?.Invoke(_CurrentPathIndex);
                });
                

                _LeftSideHeightField = root.Q<FloatField>("editor-path-height-left");
                _LeftSideHeightField.RegisterValueChangedCallback(ctx =>
                {
                    CurrentPathData.SetLeftSideHeight = ctx.newValue;
                    OnEditValue?.Invoke(_CurrentPathIndex);
                });

                _RightSideHeightField = root.Q<FloatField>("editor-path-height-right");
                _RightSideHeightField.RegisterValueChangedCallback(ctx =>
                {
                    CurrentPathData.SetRightSideHeight = ctx.newValue;
                    OnEditValue?.Invoke(_CurrentPathIndex);
                });
            }

            public void HandleSwitchPathIndex(int index)
            {
                _CurrentPathIndex = index;
                if (CurrentPathData != null && _CurrentPathIndex >= 0)
                {
                    _PathTypeField.SetValueWithoutNotify(CurrentPathData.Type);
                    _PlatformSizeField.SetValueWithoutNotify(CurrentPathData.Size);
                    _PlatformCustomSizeField.SetValueWithoutNotify(CurrentPathData.PathCustomSize);
                    _UseCustomObstaclePathField.SetValueWithoutNotify(CurrentPathData.UseCustomObstaclePath);
                    _PathObstacleDataField.SetValueWithoutNotify(CurrentPathData.ObstaclePath);
                    _InvertObstacleDataValuesToggle.SetValueWithoutNotify(CurrentPathData.InvertObstacleValues);
                    _LeftSideHeightField.SetValueWithoutNotify(CurrentPathData.SetLeftSideHeight);
                    _RightSideHeightField.SetValueWithoutNotify(CurrentPathData.SetRightSideHeight);

                    SerializedObject so = new SerializedObject(Level);
                    SerializedProperty propertyLeft = so.FindProperty("Data").GetArrayElementAtIndex(_CurrentPathIndex)
                        .FindPropertyRelative("CustomLeftSide");
                    SerializedProperty propertyRight = so.FindProperty("Data").GetArrayElementAtIndex(_CurrentPathIndex)
                        .FindPropertyRelative("CustomRightSide");
                    _CustomObstacleDataLeft.BindProperty(propertyLeft);
                    _CustomObstacleDataRight.BindProperty(propertyRight);
                }
            }

            private void SetElementEnabled(VisualElement element, bool value)
            {
                element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public void Update()
            {
                if (Level == null || Level.Data == null || CurrentPathData == null)
                {
                    SetElementEnabled(_PlatformCustomSizeVisual, false);
                    SetElementEnabled(_PathObstacleDataField, false);
                    SetElementEnabled(_InvertObstacleDataValuesToggle, false);
                    SetElementEnabled(_CustomObstacleDataVisual, false);
                    return;
                }

                SetElementEnabled(_PlatformCustomSizeVisual, CurrentPathData.Size == LevelPathSize.Custom);
                SetElementEnabled(_PathObstacleDataField, !CurrentPathData.UseCustomObstaclePath);
                SetElementEnabled(_InvertObstacleDataValuesToggle, !CurrentPathData.UseCustomObstaclePath);
                SetElementEnabled(_CustomObstacleDataVisual, CurrentPathData.UseCustomObstaclePath);
            }
        }

        private class Visualizer
        {
            private class Container
            {
                private readonly VisualElement _Parent;
                
                private readonly VisualElement _Root;
                public VisualElement PathVisual;
                public VisualElement LeftPathElement;
                public VisualElement RightPathElement;
                public VisualElement InfoVisual;

                public Label PathInfoLabel;
                public Label MainInfoLabel;

                public int Index;
                public event Action<int> OnSelect;

                public Container(VisualElement parent)
                {
                    _Parent = parent;
                    VisualTreeAsset visualTree =
                        AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                            "Assets/Editor/WASD/LevelInformationEditor/VisualizerContainer.uxml");
                    _Root = new VisualElement();
                    visualTree.CloneTree(_Root);

                    PathVisual = _Root.Q<VisualElement>("path-visual");
                    InfoVisual = _Root.Q<VisualElement>("path-info-visual");

                    LeftPathElement = _Root.Q<VisualElement>("left-path");
                    RightPathElement = _Root.Q<VisualElement>("right-path");

                    PathInfoLabel = _Root.Q<Label>("path-info-label");
                    MainInfoLabel = _Root.Q<Label>("info-label");
                    MainInfoLabel.RegisterCallback<ClickEvent>(ctx =>
                    {
                        OnSelect?.Invoke(Index);
                    });
                    
                    _Parent.Add(_Root);
                }

                public void Remove()
                {
                    _Parent.Remove(_Root);
                }
            }
            
            public LevelInformation Level => _LevelField.value as LevelInformation;
            private readonly ObjectField _LevelField;
            private readonly Color _ColorBlue;
            private readonly Color _ColorRed;
            private readonly float _ShortSize;
            private readonly float _NormalSize;
            private readonly float _LongSize;
            private readonly float _BasePlatformElementSize;

            private List<Container> _Containers;

            private VisualElement _ParentVisualElement;

            private Action<int> _SelectIndexDelegate;
            
            public Visualizer(VisualElement root, ObjectField levelField, Action<int> selectIndexDelegate)
            {
                _LevelField = levelField;
                _ColorRed = new Color(.6f, .2f, .2f);
                _ColorBlue =  new Color(.2f,.2f,.8f);
                _BasePlatformElementSize = 150f;
                _ShortSize = .5f;
                _NormalSize = 1f;
                _LongSize = 2f;
                _Containers = new List<Container>();
                _ParentVisualElement = root.Q<VisualElement>("visualizer-parent");
                _SelectIndexDelegate = selectIndexDelegate;

                root.Q<Button>("visualizer-force-refresh").clicked += GenerateVisualizer;
            }

            public void GenerateVisualizer()
            {
                ClearVisualizer();
                if (Level.Data == null || Level.Data.Count == 0)
                {
                    return;
                }

                for (int index = 0; index < Level.Data.Count; index++)
                {
                    Container newContainer = new Container(_ParentVisualElement);
                    newContainer.OnSelect += _SelectIndexDelegate;
                    _Containers.Add(newContainer);
                    RefreshContainer(index);
                }
            }

            private void ClearVisualizer()
            {
                while (_Containers.Count > 0)
                {
                    _Containers[0].Remove();
                    _Containers.RemoveAt(0);
                }
            }

            public void RefreshContainer(int index)
            {
                if (Level == null) return;
                if (index >= Level.Data.Count) return;
                Container container = _Containers[index];
                container.Index = index;
                LevelInformation.PathData data = Level.Data[index];

                container.MainInfoLabel.text = $"Id: {index}";
                container.PathVisual.style.display =
                    data.Type == LevelPathStep.Path ? DisplayStyle.Flex : DisplayStyle.None;
                container.InfoVisual.style.display =
                    data.Type != LevelPathStep.Path ? DisplayStyle.Flex : DisplayStyle.None;
                
                if (data.Type == LevelPathStep.Path)
                {
                    container.MainInfoLabel.text += $" | Size: {data.Size}";
                    if (data.Size == LevelPathSize.Custom) container.MainInfoLabel.text += $" ({data.PathCustomSize})";
                    bool switchColors = AreColorsSwitched(index);
                    
                    container.LeftPathElement.style.backgroundColor = !switchColors ? _ColorBlue : _ColorRed;
                    container.RightPathElement.style.backgroundColor = !switchColors ? _ColorRed : _ColorBlue;

                    float platformSize = _BasePlatformElementSize;
                    switch (data.Size)
                    {
                        case LevelPathSize.Short:
                            platformSize *= _ShortSize;
                            break;
                        case LevelPathSize.Normal:
                            platformSize *= _NormalSize;
                            break;
                        case LevelPathSize.Long:
                            platformSize *= _LongSize;
                            break;
                        case LevelPathSize.Custom:
                            platformSize *= data.PathCustomSize;
                            break;
                        default:
                            platformSize *= 1f;
                            break;
                    }

                    container.LeftPathElement.style.height = platformSize;
                    container.RightPathElement.style.height = platformSize;

                    void FPopulateObstacles(VisualElement platform, List<ObstaclePathData.Obstacle> obstacles)
                    {
                        platform.Clear();
                        if (obstacles == null) return;

                        bool isCountEven = obstacles.Count % 2 == 0;
                        float inBetween = isCountEven
                            ? (platformSize - 20f) / (obstacles.Count + 1)
                            : (platformSize - 20f) / (obstacles.Count - 1);

                        for (var i = 0; i < obstacles.Count; i++)
                        {
                            ObstaclePathData.Obstacle obstacle = obstacles[i];
                            Label newLabel = new Label($"{obstacle.Type}");
                            newLabel.style.position = Position.Absolute;

                            if (obstacle.AutomaticPosition)
                            {
                                if (obstacles.Count == 1)
                                {
                                    newLabel.style.bottom = (platformSize - 20f) / 2f;
                                }
                                else
                                {
                                    newLabel.style.bottom = inBetween * (i + (isCountEven ? 1 : 0));
                                }

                                platform.Add(newLabel);
                            }
                            else
                            {
                                newLabel.style.bottom = (platformSize - 20f) * obstacle.PositionOnPath;
                                platform.Add(newLabel);
                            }
                        }
                    }
                    
                    if (!data.UseCustomObstaclePath && data.ObstaclePath != null)
                    {
                        FPopulateObstacles(container.LeftPathElement,
                            !data.InvertObstacleValues ? data.ObstaclePath.LeftSide : data.ObstaclePath.RightSide);
                        FPopulateObstacles(container.RightPathElement,
                            !data.InvertObstacleValues ? data.ObstaclePath.RightSide : data.ObstaclePath.LeftSide);
                    }
                    else
                    {
                        FPopulateObstacles(container.LeftPathElement,
                            data.UseCustomObstaclePath ? data.CustomLeftSide : null);
                        FPopulateObstacles(container.RightPathElement,
                            data.UseCustomObstaclePath ? data.CustomRightSide : null);
                    }
                    
                }
                else
                {
                    container.PathInfoLabel.text = $"{data.Type}";
                    if (data.Type == LevelPathStep.ChangeHeight)
                    {
                        container.PathInfoLabel.text += $"\nL: {data.SetLeftSideHeight} | R: {data.SetRightSideHeight}";
                    }
                }
            }

            private bool AreColorsSwitched(int index)
            {
                bool result = false;
                for (int i = 0; i < index; i++)
                {
                    if (Level.Data[i].Type == LevelPathStep.SwitchColors) result = !result;
                }
                return result;
            }
        }

        #endregion

        #region Events

        public event Action<int> OnPathIndexChange;

        #endregion

        #region Fields

        private LevelGeneral _LevelGeneral;
        private EditorMainContainers _EditorMainContainers;
        private EditorPathSelect _EditorPathSelect;
        private EditorValueFields _EditorValueFields;
        private Visualizer _Visualizer;

        private int _CurrentPathDataId;

        #endregion

        [MenuItem("WASD/Level Editor")]
        public static void ShowExample()
        {
            LevelEditor wnd = GetWindow<LevelEditor>();
            wnd.minSize = new Vector2(620, 300);
            wnd.titleContent = new GUIContent("WaveRun Level Editor");
        }

        public void CreateGUI()
        {
            VisualTreeAsset visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Editor/WASD/LevelInformationEditor/LevelEditor.uxml");
            visualTree.CloneTree(rootVisualElement);

            _LevelGeneral = new LevelGeneral(rootVisualElement);
            _LevelGeneral.LevelField.RegisterValueChangedCallback(ctx =>
            {
                LevelInformation value = ctx.newValue as LevelInformation;
                HandleCurrentPathIndexChange(value != null && value.Data is { Count: > 0 } ? 0 : -1);
                _Visualizer.GenerateVisualizer();
            });
            
            _EditorMainContainers = new EditorMainContainers(rootVisualElement, _LevelGeneral.LevelField);
            OnPathIndexChange += _EditorMainContainers.HandleCurrentPathIndexChange;
            _EditorPathSelect = new EditorPathSelect(
                rootVisualElement,
                _LevelGeneral.LevelField,
                ChangeCurrentPathIndex,
                AddNewPathData,
                CopyData,
                PasteData,
                MoveCurrentData,
                RemoveCurrentData,
                ClearUnusedData,
                GoToIndex);
            OnPathIndexChange += _EditorPathSelect.HandlePathIndexChange;
            _EditorValueFields = new EditorValueFields(rootVisualElement, _LevelGeneral.LevelField);
            OnPathIndexChange += _EditorValueFields.HandleSwitchPathIndex;
            _Visualizer = new Visualizer( rootVisualElement,  _LevelGeneral.LevelField, HandleCurrentPathIndexChange);
            _EditorValueFields.OnEditValue += _Visualizer.RefreshContainer;
            
            _CurrentPathDataId = -1;
            OnPathIndexChange?.Invoke(-1);
        }

        private void Update()
        {
            _LevelGeneral.Update();
            _EditorMainContainers.Update();
            _EditorPathSelect.Update();
            _EditorValueFields.Update();
        }

        private void GoToIndex(int index)
        {
            if (_LevelGeneral.Level == null || _LevelGeneral.Level.Data == null) return;
            if (index < 0 || index >= _LevelGeneral.Level.Data.Count)
            {
                Debug.LogWarning($"LevelEditor: Invalid GoTo Index (Id: {index} | Count: _LevelGeneral.Level.Data.Count)");
                return;
            }
            HandleCurrentPathIndexChange(index);
        }
                
        private void ChangeCurrentPathIndex(string direction)
        {
            if (_LevelGeneral.Level == null || _LevelGeneral.Level.Data == null) return;
            switch (direction)
            {
                case "prev":
                    _CurrentPathDataId--;
                    if (_CurrentPathDataId < 0) _CurrentPathDataId = 0;
                    break;
                case "next":
                    _CurrentPathDataId++;
                    if (_CurrentPathDataId >= _LevelGeneral.Level.Data.Count)
                        _CurrentPathDataId = _LevelGeneral.Level.Data.Count - 1;
                    break;
            }
            HandleCurrentPathIndexChange(_CurrentPathDataId);
        }

        private void HandleCurrentPathIndexChange(int newValue)
        {
            if (_LevelGeneral.Level.Data.Count == 0)
                newValue = -1;
            else if (newValue >= _LevelGeneral.Level.Data.Count)
                newValue = _LevelGeneral.Level.Data.Count - 1;
            
            _CurrentPathDataId = newValue;
            OnPathIndexChange?.Invoke(newValue);
        }

        private void AddNewPathData(bool isBefore)
        {
            if (_LevelGeneral.Level == null) return;
            _LevelGeneral.Level.Data ??= new List<LevelInformation.PathData>();

            int newIndex = _CurrentPathDataId == -1 ? 0 : _CurrentPathDataId + (isBefore ? 0 : 1);
            _LevelGeneral.Level.Data.Insert(newIndex, new LevelInformation.PathData());
            HandleCurrentPathIndexChange(newIndex);
            _Visualizer.GenerateVisualizer();
        }

        private LevelInformation.PathData CopyData()
        {
            return _LevelGeneral.Level.Data[_CurrentPathDataId].Copy();
        }

        private void PasteData(LevelInformation.PathData data)
        {
            _LevelGeneral.Level.Data[_CurrentPathDataId] = data.Copy();
            _Visualizer.RefreshContainer(_CurrentPathDataId);
            HandleCurrentPathIndexChange(_CurrentPathDataId);
        }

        private void RemoveCurrentData()
        {
            _LevelGeneral.Level.Data.RemoveAt(_CurrentPathDataId);
            _Visualizer.GenerateVisualizer();
            HandleCurrentPathIndexChange(_CurrentPathDataId);
        }

        private void ClearUnusedData()
        {
            
        }

        private void MoveCurrentData(bool isBefore)
        {
            int movedToIndex = isBefore ? _CurrentPathDataId - 1 : _CurrentPathDataId + 1;
            LevelInformation.PathData currentCopy = _LevelGeneral.Level.Data[_CurrentPathDataId].Copy();
            LevelInformation.PathData otherCopy = _LevelGeneral.Level.Data[movedToIndex];
            _LevelGeneral.Level.Data[movedToIndex] = currentCopy;
            _LevelGeneral.Level.Data[_CurrentPathDataId] = otherCopy;
            HandleCurrentPathIndexChange(movedToIndex);
            
            _Visualizer.RefreshContainer(movedToIndex);
            _Visualizer.RefreshContainer(_CurrentPathDataId);
        }

        /**
        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
    
            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            VisualElement label = new Label("Hello World! From C#");
            root.Add(label);
    
            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/WASD/LevelInformationEditor/LevelEditor.uxml");
            VisualElement labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);
    
            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/WASD/LevelInformationEditor/LevelEditor.uss");
            VisualElement labelWithStyle = new Label("Hello World! With Style");
            labelWithStyle.styleSheets.Add(styleSheet);
            root.Add(labelWithStyle);
        }
        */
    }
}
