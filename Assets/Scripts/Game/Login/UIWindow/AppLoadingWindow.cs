using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public sealed class AppLoadingWindow : GameWindowBase {
        [SerializeField]
        [Required]
        private TMP_Text infoText;

        [SerializeField]
        [Required]
        private Slider progressSlider;

        protected override void OnShow() {
            Debug.LogErrorFormat("============ AppLoadingWindow.OnShow()");
        }

        protected override void OnHide() {
            Debug.LogErrorFormat("============ AppLoadingWindow.OnHide()");
        }

        public void SetInfoText(string text) {
            infoText.text = text;
        }

        /// <summary>
        /// 设置进度条进度
        /// </summary>
        /// <param name="progress">加载进度条显示的进度; 范围 [0f, 1f]</param>
        public void SetProgress(float progress) {
            progressSlider.value = Mathf.Clamp(progress, 0f, 1f);
        }
    }
}
