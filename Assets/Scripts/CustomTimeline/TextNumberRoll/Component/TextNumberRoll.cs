using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CustomTimeline.TextNumberRoll {
    [HideMonoScript]
    public class TextNumberRoll : MonoBehaviour {
        [FormerlySerializedAs("Formatter")]
        [SerializeReference]
        public TextNumberFormatter formatter;

        [ReadOnly]
        [SerializeField]
        [HideIf("@this.textMeshPro == null")]
        [LabelText("Text Component")]
        private TextMeshProUGUI textMeshPro;

        private bool _haveTextMeshPro;

        [ReadOnly]
        [SerializeField]
        [HideIf("@this.text == null")]
        [LabelText("Text Component")]
        private Text text;

        private bool _haveText;

        private bool TryGetTextComponents() {
            _haveTextMeshPro = TryGetComponent(out textMeshPro);
            _haveText = TryGetComponent(out text);
            return _haveTextMeshPro || _haveText;
        }

        private void OnValidate() {
            TryGetTextComponents();
        }

        private void Awake() {
            if (!TryGetTextComponents()) {
                Debug.LogError("TextNumberRoll need at least one Text component.");
            }
        }

        public void SetString(int number) {
            if (ReferenceEquals(formatter, null))
                return;
            SetString(formatter.FormatNumber(number));
        }

        public void SetString(float number) {
            if (ReferenceEquals(formatter, null))
                return;
            SetString(formatter.FormatNumber(number));
        }

        public void SetString(double number) {
            if (ReferenceEquals(formatter, null))
                return;
            SetString(formatter.FormatNumber(number));
        }

        private void SetString(string textString) {
            SetNumberTextMeshPro(textString);
            SetNumberText(textString);
        }

        private void SetNumberTextMeshPro(string textString) {
            if (!_haveTextMeshPro)
                return;
            textMeshPro.text = textString;
        }

        private void SetNumberText(string textString) {
            if (!_haveText)
                return;
            text.text = textString;
        }
    }
}
