using System;
using TMPro;
using UnityEngine;

namespace EMCC.Ship {
    public class ShipAimTool : MonoBehaviour {
        public TMP_Text testText;
        public TMP_Text testFocusText;
        public TMP_Text testPauseText;
        public TMP_Text testCursorLockStateText;

        private static bool IsFocus;
        private static bool IsPause;
        private static bool IsCursorLock => IsFocus && !IsPause;

        public Vector2 MousePosition => mousePosition;
        private Vector2 mousePosition;
        private CursorLockMode _cursorLockMode;

        private void Awake() {
            IsFocus = true;
            IsPause = false;
        }

        private void OnEnable() {
            _cursorLockMode = Cursor.lockState;
            testCursorLockStateText.text = $"Cursor Lock State = {_cursorLockMode}";
        }

        private void Update() {
            UpdateCursorLockState(Cursor.lockState);

            Vector2 pos = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            if (pos == mousePosition) return;

            testText.text = pos.ToString();
            mousePosition = pos;
        }

        private void OnApplicationFocus(bool hasFocus) {
            IsFocus = hasFocus;
            testFocusText.text = $"IsFocus={IsFocus}";
            UpdateCursorLockState();
        }

        private void OnApplicationPause(bool pauseStatus) {
            IsPause = pauseStatus;
            testPauseText.text = $"IsPause={IsPause}";
            UpdateCursorLockState();
        }

        private static void UpdateCursorLockState() {
            Cursor.lockState = IsCursorLock ? CursorLockMode.Locked : CursorLockMode.Confined;
        }

        private void UpdateCursorLockState(CursorLockMode cursorLockMode) {
            if (cursorLockMode == _cursorLockMode) return;
            _cursorLockMode = cursorLockMode;
            testCursorLockStateText.text = $"Cursor Lock State = {_cursorLockMode}";
        }
    }
}
