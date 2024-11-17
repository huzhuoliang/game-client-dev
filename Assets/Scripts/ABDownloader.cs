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

        [ShowInInspector]
        [PropertyOrder(3)]
        [DisplayAsString]
        private string FullURL {
            get {
                if (url.EndsWith("/") || url.EndsWith("\\")) {
                    return url + buildTarget;
                }

                return url + "/" + buildTarget;
            }
        }

        [SerializeField]
        [PropertyOrder(4)]
        private List<string> bundleNames = new();

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
        [ListDrawerSettings(IsReadOnly = true)]
        [HideReferenceObjectPicker]
        [LabelText("AssetBundle Unit List")]
        [HideInEditorMode]
        [PropertyOrder(101)]
        private List<ABUnit> _abUnits = new();

        [ShowInInspector]
        [NonSerialized]
        [InlineButton(nameof(LoadBundle))]
        [HideLabel]
        [HideInEditorMode]
        [PropertyOrder(200)]
        private string _bundleName;

        private void Awake() {
            _savePath = Application.persistentDataPath;
        }

        private void OnDestroy() {
            DisposeAllABUnit();
        }

        private void LoadBundle() {
            if (string.IsNullOrEmpty(_bundleName))
                return;
            ABUnit abUnit = new ABUnit(FullURL, _bundleName, 0, FullSavePath);
            _abUnits.Add(abUnit);
            StartCoroutine(DownloadABUnit(abUnit));
        }

        [PropertySpace(10f)]
        [Button("Download", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        [DisableInEditorMode]
        [PropertyOrder(1000)]
        private void Download() {
            LoadMainAssetBundle();
            //            DisposeAllABUnit();
            //            foreach (string bundleName in bundleNames) {
            //                _abUnits.Add(new ABUnit(FullURL, bundleName, FullSavePath));
            //            }
            //
            //            foreach (ABUnit ab in _abUnits) {
            //                StartCoroutine(DownloadABUnit(ab));
            //            }
        }

        private void LoadMainAssetBundle() {
            DisposeAllABUnit();
            _mainABUnit = new MainABUnit(FullURL, buildTarget.ToString(), FullSavePath);
            StartCoroutine(DownloadABUnit(_mainABUnit));
        }

        /// <summary>
        /// Dispose All ABUnit instance
        /// </summary>
        /// <param name="includeMainABUnit">Dispose Main ABUnit or not</param>
        private void DisposeAllABUnit(bool includeMainABUnit = true) {
            foreach (ABUnit ab in _abUnits) {
                ab.Dispose();
            }

            _abUnits.Clear();

            // ReSharper disable once InvertIf
            if (includeMainABUnit && _mainABUnit != null) {
                _mainABUnit.Dispose();
                _mainABUnit = null;
            }
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

            abUnit.Load();
            if (abUnit is MainABUnit mainABUnit) {
                AfterLoadMainABUint(mainABUnit);
            }
        }

        private static void AfterLoadMainABUint(MainABUnit abUnit) {
            abUnit.LoadAssetBundleInfos();
        }
    }
}
