using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Editor.MyToolKitWindow {
    public class MyToolKitWindow : EditorWindow {
        // ReSharper disable once InconsistentNaming
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset;

        private ListView _listView;

        private GameObject[] _sceneObjects;

        private ToolbarButton _refreshButton;
        private ToolbarButton _button1;

        [MenuItem("MyWindow/MyToolKitWindow")]
        public static void ShowExample() {
            MyToolKitWindow wnd = GetWindow<MyToolKitWindow>();
            wnd.titleContent = new GUIContent("MyToolKitWindow");
        }

        public void CreateGUI() {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUxml = m_VisualTreeAsset.Instantiate();
            labelFromUxml.style.flexGrow = 1;
            root.Add(labelFromUxml);

            HelpBox helpBox = new HelpBox("信息哈哈哈哈", HelpBoxMessageType.None);
            VisualElement rightVe = root.Q<VisualElement>("right");

            rightVe.Add(helpBox);

            _refreshButton = root.Q<ToolbarButton>("RefreshButton");
            _refreshButton.clicked += OnRefreshObjects;
            
            _button1 = root.Q<ToolbarButton>("MyButton1");
            _button1.clicked += OnButton1;

            _listView = root.Q<ListView>("LeftListView");
            _listView.itemsSource = _sceneObjects;
            _listView.makeItem = MakeListItem;
            _listView.bindItem = BindListItem;
            _listView.selectionChanged += ListViewOnSelectionChanged;
        }

        private void ScheduleAction(TimerState obj) {
            Debug.LogErrorFormat("ScheduleAction");
        }

        private static void ListViewOnSelectionChanged(IEnumerable<object> objList) {
            foreach (object obj in objList) {
                if (obj is not GameObject gameObject) continue;
                Selection.activeGameObject = gameObject;
                break;
            }
        }

        private void BindListItem(VisualElement ve, int index) {
            if (ve is not Label label)
                return;
            GameObject go = _sceneObjects[index];
            label.text = go.name;
        }

        private static VisualElement MakeListItem() {
            Label label = new() {
                    style = {
                            unityTextAlign = TextAnchor.MiddleLeft,
                            paddingLeft = 2
                    }
            };
            return label;
        }

        private void OnButton1() {
            IVisualElementScheduledItem scheduleItem = rootVisualElement.schedule.Execute(ScheduleAction);
            scheduleItem.ExecuteLater(2000);
        }

        private void OnRefreshObjects() {
            Scene scene = SceneManager.GetActiveScene();
            _sceneObjects = scene.GetRootGameObjects();
            _listView.itemsSource = _sceneObjects;
        }
    }
}
