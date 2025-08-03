using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.UI {
    public class UIManager : MonoBehaviour {
        [SerializeField]
        [Required]
        private Transform parent;

        [SerializeReference]
        private List<GameWindowBase> windowTemplateList = new();

        [ShowInInspector]
        [DictionaryDrawerSettings(IsReadOnly = true)]
        private readonly Dictionary<ulong, GameWindowBase> _windows = new();

        private void Start() {
            foreach (GameWindowBase window in windowTemplateList) {
                if (window != null) {
                    window.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 当前存在的窗口实例 ID
        /// </summary>
        private static ulong _currMaxWindowInstanceID;

        private static ulong GetNewWindowInstanceID() {
            _currMaxWindowInstanceID++;
            return _currMaxWindowInstanceID;
        }

        public T ShowWindow<T>() where T : GameWindowBase {
            return ShowWindow<T>(0);
        }

        public T ShowWindow<T>(GameWindowBase window) where T : GameWindowBase {
            return ShowWindow<T>(window == null ? 0 : window.WindowInstanceID);
        }

        public T ShowWindow<T>(ulong parentWindowInstanceID) where T : GameWindowBase {
            T window = CrateWindow<T>();
            if (window == null) {
                return null;
            }

            window.WindowInstanceID = GetNewWindowInstanceID();
            window.Parent = FindWindow(parentWindowInstanceID);
            if (window.Parent != null) {
                window.Parent.Child.Add(window);
            }

            _windows[window.WindowInstanceID] = window;
            window.Show(this);
            return window;
        }

        public GameWindowBase FindWindow(ulong windowInstanceId) {
            if (windowInstanceId == 0 || !_windows.TryGetValue(windowInstanceId, out GameWindowBase window)) {
                return null;
            }

            return window;
        }

        public void HideWindow(GameWindowBase window) {
            HideWindow(window.WindowInstanceID);
        }

        public void HideWindow(ulong windowInstanceId) {
            GameWindowBase window = FindWindow(windowInstanceId);
            if (window == null) {
                return;
            }

            if (window.Child.Count > 0) {
                for (int i = window.Child.Count - 1; i >= 0; i--) {
                    HideWindow(window.Child[i].WindowInstanceID);
                }
            }

            window.Hide();
            if (window.Parent != null && window.Parent.Child.Contains(window)) {
                window.Parent.Child.Remove(window);
            }

            window.Child.Clear();
            _windows.Remove(window.WindowInstanceID);
            Destroy(window.gameObject);
        }

        private T CrateWindow<T>() where T : GameWindowBase {
            T temp = FindWindowTemplate<T>();
            if (temp == null) {
                return null;
            }

            GameObject windowObj = Instantiate(temp.gameObject, parent);
            if (!windowObj.TryGetComponent(out T window)) {
                Destroy(windowObj);
                return null;
            }

            windowObj.SetActive(true);
            return window;
        }

        private T FindWindowTemplate<T>() where T : GameWindowBase {
            foreach (GameWindowBase temp in windowTemplateList) {
                if (temp is T tempClass) {
                    return tempClass;
                }
            }

            return null;
        }
    }
}
