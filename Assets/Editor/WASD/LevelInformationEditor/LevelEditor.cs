using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using WASD.Enums;
using WASD.Runtime.Audio;
using WASD.Runtime.Levels;


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

        public EditorMainContainers(VisualElement root, ObjectField levelField)
        {
            LevelField = levelField;
            Main = root.Q<VisualElement>("editor-visual-full");
            PathFull = root.Q<VisualElement>("editor-visual-path");
            PathPath = root.Q<VisualElement>("editor-visual-path-path");
            PathHeight = root.Q<VisualElement>("editor-visual-path-height");
        }

        public void Update(int currentPathID)
        {
            Main.style.visibility = Level != null ? Visibility.Visible : Visibility.Hidden;
            if (Level == null) return;

            PathFull.style.visibility = Level.Data is { Count: > 0 } ? Visibility.Visible : Visibility.Hidden;
            if (Level.Data != null && currentPathID < Level.Data.Count)
            {
                PathPath.style.display = Level.Data[currentPathID].Type == LevelPathStep.Path
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                PathHeight.style.display = Level.Data[currentPathID].Type == LevelPathStep.ChangeHeight
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
            else
            {
                PathPath.style.display = DisplayStyle.None;
                PathHeight.style.display = DisplayStyle.None;
            }
        }
    }

    private class EditorPathSelect
    {
        public LevelInformation Level => LevelField.value as LevelInformation;
        public ObjectField LevelField;

        private Label _MainLabel;
        private Button _AddNewBeforeButton;
        private Button _AddNewAfterButton;
        private Button _CopyButton;
        private Button _PasteButton;

        private Button _PrevButton;
        private Button _NextButton;

        private Button _MoveBeforeButton;
        private Button _MoveAfterButton;

        private Button _RemoveButton;
        private Button _ClearUnusedDataButton;

        private StyleColor _EnabledColor;
        private StyleColor _DisabledColor;
        
        public EditorPathSelect(VisualElement root, ObjectField levelField)
        {
            _EnabledColor = new StyleColor(new Color(0.345098048f, 0.345098048f, 0.345098048f, 1f));
            _DisabledColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f, 1f));
            LevelField = levelField;
            _MainLabel = root.Q<Label>("editor-select-label");
            _AddNewBeforeButton = root.Q<Button>("editor-select-new-before");
            _AddNewAfterButton = root.Q<Button>("editor-select-new-after");
            _CopyButton = root.Q<Button>("editor-select-copy");
            _PasteButton = root.Q<Button>("editor-select-paste");
            _PrevButton = root.Q<Button>("editor-select-prev");
            _NextButton = root.Q<Button>("editor-select-next");
            _MoveBeforeButton = root.Q<Button>("editor-select-moveup");
            _MoveAfterButton = root.Q<Button>("editor-select-movedown");
            _RemoveButton = root.Q<Button>("editor-select-remove");
            _ClearUnusedDataButton = root.Q<Button>("editor-select-clear");
        }

        private void SetButtonEnabled(Button button, bool value)
        {
            button.style.backgroundColor = value ? _EnabledColor : _DisabledColor;
            button.pickingMode = value ? PickingMode.Position : PickingMode.Ignore;
        }

        public void Update(int currentPathId)
        {
            if (Level == null || Level.Data == null)
            {
                SetButtonEnabled(_AddNewBeforeButton, false);
                SetButtonEnabled(_AddNewAfterButton, false);
                SetButtonEnabled(_CopyButton, false);
                SetButtonEnabled(_PasteButton, false);
                SetButtonEnabled(_PrevButton, false);
                SetButtonEnabled(_MoveBeforeButton, false);
                SetButtonEnabled(_MoveAfterButton, false);
                SetButtonEnabled(_NextButton, false);
                SetButtonEnabled(_RemoveButton, false);
                SetButtonEnabled(_ClearUnusedDataButton, false);
                _MainLabel.text = "Path Ids: (0/0)";
                return;
            }
            _MainLabel.text = Level.Data is { Count: > 0 }
                ? $"Path Ids: ({currentPathId}/{Level.Data.Count - 1})"
                : "Path Ids: (0/0)";

            if (Level.Data == null) return;
            
            SetButtonEnabled(_AddNewBeforeButton, currentPathId > 0);
            SetButtonEnabled(_AddNewAfterButton, true);
            SetButtonEnabled(_CopyButton, true);
            SetButtonEnabled(_PrevButton, currentPathId > 0);
            SetButtonEnabled(_MoveBeforeButton, currentPathId > 0);
            SetButtonEnabled(_MoveAfterButton, currentPathId < Level.Data.Count - 1);
            SetButtonEnabled(_NextButton, currentPathId < Level.Data.Count - 1);
            SetButtonEnabled(_RemoveButton, Level.Data.Count > 0);
            SetButtonEnabled(_ClearUnusedDataButton, true);
        }
    }
    
    #endregion

    #region Fields

    private LevelInformation _SelectedLevel;
    private LevelGeneral _LevelGeneral;
    private EditorMainContainers _EditorMainContainers;
    private EditorPathSelect _EditorPathSelect;
    
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
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/WASD/LevelInformationEditor/LevelEditor.uxml");
        visualTree.CloneTree(rootVisualElement);

        _LevelGeneral = new LevelGeneral(rootVisualElement);
        _EditorMainContainers = new EditorMainContainers(rootVisualElement, _LevelGeneral.LevelField);
        _EditorPathSelect = new EditorPathSelect(rootVisualElement, _LevelGeneral.LevelField);
        _CurrentPathDataId = 0;
    }

    private void Update()
    {
        _LevelGeneral.Update();
        _EditorMainContainers.Update(_CurrentPathDataId);
        _EditorPathSelect.Update(_CurrentPathDataId);
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