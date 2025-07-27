using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI {
    public abstract class GameWindowBase : MonoBehaviour {
        private UIManager _uiManager;
        public ulong WindowInstanceID { get; internal set; }
        public GameWindowBase Parent { get; internal set; }
        public List<GameWindowBase> Child { get; } = new();

        public bool IsShow { get; private set; }

        public event Action OnShowEvent;
        public event Action OnHideEvent;

        internal void Show(UIManager uiManager) {
            _uiManager = uiManager;
            IsShow = true;
            OnShow();
            OnShowEvent?.Invoke();
        }

        internal void Hide() {
            OnHide();
            OnHideEvent?.Invoke();
            IsShow = false;
        }

        protected void HideMySelf() {
            _uiManager.HideWindow(WindowInstanceID);
        }

        protected virtual void OnShow() {
        }

        protected virtual void OnHide() {
        }
    }
}
