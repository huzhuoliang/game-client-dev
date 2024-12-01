using System;
using System.Collections;
using System.IO;
using System.Linq;
using Game;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using BuildTarget = Game.BuildTarget;

namespace DefaultNamespace {
    public class ABDownloader : MonoBehaviour {
        [SerializeField]
        [LabelText("URL")]
        [PropertyOrder(1)]
        private string url = "";

        [LabelText("Target Platform")]
        [PropertyOrder(2)]
        public BuildTarget buildTarget;

        [ShowInInspector]
        [PropertyOrder(3)]
        [DisplayAsString]
        private string FullURL {
            get {
                string u = url;
                if (!u.EndsWith("/"))
                    u += "/";
                return u + buildTarget;
            }
        }

        [NonSerialized]
        private string _savePath = "";

        [ShowInInspector]
        [PropertyOrder(5)]
        [DisplayAsString]
        [HideInEditorMode]
        private string FullSavePath => Path.Combine(_savePath, "bundle", buildTarget.ToString());

        [NonSerialized]
        [ShowInInspector]
        [PropertyOrder(101)]
        [BoxGroup("Main AssetBundle 2", CenterLabel = true)]
        [HideInEditorMode]
        [HideLabel]
        [HideReferenceObjectPicker]
        private MainAB _mainAB;

        [ShowInInspector]
        [ValueDropdown(nameof(GetAllAssetBundleInfoUnit), AppendNextDrawer = true)]
        [HideLabel]
        [InlineButton(nameof(OdinLoadAB), "Load")]
        [PropertyOrder(102)]
        private string _loadABName;

        private void Awake() {
            _savePath = Path.GetFullPath(Application.persistentDataPath);
        }

        private void OnDestroy() {
            _mainAB?.Dispose();
        }

        private IEnumerable GetAllAssetBundleInfoUnit() {
            return _mainAB?.Infos?.InfoList?.Select(v => v.BundleName);
        }

        private void OdinLoadAB() {
            if (_mainAB == null) {
                Debug.LogError("_mainAB is null");
                return;
            }

            StartCoroutine(_mainAB.LoadABAsync(_loadABName));
        }

        [PropertySpace(10f)]
        [Button("Download", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        [DisableInEditorMode]
        [PropertyOrder(1000)]
        private void Download() {
            LoadMainAssetBundle();
        }

        private void LoadMainAssetBundle() {
            _mainAB = new MainAB(FullURL, FullSavePath);
            StartCoroutine(_mainAB.LoadInfosAsync());
        }

        // TODO delete
        private IEnumerator DownloadABUnit(ABUnit abUnit) {
            if (!abUnit.StartDownload()) {
                yield break;
            }

            while (!abUnit.IsDownloadDone()) {
                yield return null;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            Debug.Log($"File \"{abUnit.BundleName}\" download finished.");
            if (abUnit.DownloadResult != UnityWebRequest.Result.Success) {
                Debug.LogError(abUnit.DownloadError);
                yield break;
            }

            // abUnit.Load();
            if (abUnit is MainABUnit mainABUnit) {
                AfterLoadMainABUint(mainABUnit);
            }
        }

        private static void AfterLoadMainABUint(MainABUnit abUnit) {
            abUnit.LoadAssetBundleInfos();
        }
    }
}
