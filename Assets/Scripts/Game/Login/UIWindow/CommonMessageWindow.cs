using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class CommonMessageWindow : GameWindowBase {
        [SerializeField]
        [Required]
        private Button cancelButton;

        [SerializeField]
        [Required]
        private Button confirmButton;

        [SerializeField]
        [Required]
        private TMP_Text messageText;

        [SerializeField]
        [Required]
        private TMP_Text titleText;

        private Action _onCancelCallback;
        private Action _onConfirmCallback;

        public CommonMessageWindow Init() {
            messageText.text = "";
            titleText.text = "";
            cancelButton.gameObject.SetActive(false);
            confirmButton.gameObject.SetActive(false);
            _onCancelCallback = null;
            _onConfirmCallback = null;
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancel);
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirm);
            return this;
        }

        public CommonMessageWindow SetMessage(string message) {
            messageText.text = message;
            return this;
        }

        public CommonMessageWindow SetTitle(string title) {
            titleText.text = title;
            return this;
        }

        public CommonMessageWindow SetShowCancelButton(bool showButton) {
            cancelButton.gameObject.SetActive(showButton);
            return this;
        }

        public CommonMessageWindow SetShowConfirmButton(bool showButton) {
            confirmButton.gameObject.SetActive(showButton);
            return this;
        }

        public CommonMessageWindow SetOnCancel(Action callback) {
            _onCancelCallback = null;
            _onCancelCallback += callback;
            return this;
        }

        public CommonMessageWindow SetOnConfirm(Action callback) {
            _onConfirmCallback = null;
            _onConfirmCallback += callback;
            return this;
        }

        private void OnCancel() {
            _onCancelCallback?.Invoke();
            HideMySelf();
        }

        private void OnConfirm() {
            _onConfirmCallback?.Invoke();
            HideMySelf();
        }
    }
}
