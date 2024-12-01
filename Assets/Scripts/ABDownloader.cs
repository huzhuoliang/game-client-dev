using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        //        [LabelText("CRC")]
        //        [PropertyOrder(2)]
        //        public uint crc;

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
        [PropertyOrder(100)]
        [HideInEditorMode]
        [HideLabel]
        [HideReferenceObjectPicker]
        [BoxGroup("Main AssetBundle", centerLabel: true)]
        [HideIf("@this._mainABUnit == null")]
        private MainABUnit _mainABUnit;

        [NonSerialized]
        [ShowInInspector]
        [PropertyOrder(101)]
        [BoxGroup("Main AssetBundle 2", CenterLabel = true)]
        // [HideInEditorMode]
        [HideLabel]
        [HideReferenceObjectPicker]
        // [HideIf("@this._mainABUnit == null")]
        private MainAB _mainAB;

        [ShowInInspector]
        [NonSerialized]
        [InlineButton(nameof(LoadBundle))]
        [HideLabel]
        [HideInEditorMode]
        [PropertyOrder(200)]
        private string _bundleName;

        private void Awake() {
            _savePath = Path.GetFullPath(Application.persistentDataPath);
        }

        private void OnDestroy() {
            _mainABUnit?.Dispose();
        }

        private void LoadBundle() {
            if (string.IsNullOrEmpty(_bundleName)) {
                return;
            }

            SubABUnit unit = _mainABUnit.GetSubABUnit(_bundleName);
            StartCoroutine(unit.StartDownloadCoroutine());
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

            // _mainABUnit = new MainABUnit(FullURL, buildTarget.ToString(), FullSavePath);
            // StartCoroutine(DownloadABUnit(_mainABUnit));
        }

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
