using System;
using System.IO;
using System.Linq;
using DefaultNamespace;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

namespace Game {
    [Serializable]
    public class MainABUnit : ABUnit {
        [NonSerialized]
        [ShowInInspector]
        [HideLabel]
        [HideReferenceObjectPicker]
        [PropertyOrder(400)]
        [HideIf("@this._infos == null")]
        private AssetBundleInfos _infos;

        [ShowInInspector]
        [DisplayAsString]
        [LabelText("Infos Download Status")]
        [PropertyOrder(102)]
        public string InfosDownloadedBytesStr =>
                $"{InfosDownloadedBytes.FormatByte()} ({InfosDownloadedProgress * 100f:0.00}%)";

        public const string AssetBundleInfoFileName = "AssetBundleInfo.json";

        public string FullAssetBundleInfosURL => Path.Combine(URL, AssetBundleInfoFileName);
        public string FullAssetBundleInfosSavePath => Path.Combine(SavePath, AssetBundleInfoFileName);
        public ulong InfosDownloadedBytes => _infosRequest?.downloadedBytes ?? 0;

        private UnityWebRequest _infosRequest;

        public float InfosDownloadedProgress => _infosRequest?.downloadProgress ?? 0f;

        [NonSerialized]
        [ShowInInspector]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [HideLabel]
        private SubABUnitList _subABUnitList = new();

        public MainABUnit(string url, string name, string savePath, string saveFileName = "")
                : base(url, new Hash128(), name, 0, savePath, saveFileName) {
        }

        protected override bool GetIsDownloadedInternal() {
            return DownloadedProgress >= 1f && ManifestDownloadedProgress >= 1f && InfosDownloadedProgress >= 1f;
        }

        protected override float GetTotalProgressInternal() {
            return Mathf.Min(DownloadedProgress, ManifestDownloadedProgress, InfosDownloadedProgress);
        }

        protected override bool OnStartDownload() {
            try {
                _infosRequest = UnityWebRequestAssetBundle.GetAssetBundle(FullAssetBundleInfosURL);
                _infosRequest.downloadHandler = new DownloadHandlerFile(FullAssetBundleInfosSavePath);
                _infosRequest.SendWebRequest();
                return true;
            } catch (Exception e) {
                Debug.LogError($"Create web request for AssetBundleInfos failed.\n{e}");
                return false;
            }
        }

        public override bool IsSubClassDownloadDone() {
            if (_infosRequest == null) {
                throw new Exception("Please call StartDownload() before IsDownloadDone().");
            }

            return _infosRequest.isDone;
        }

        public void LoadAssetBundleInfos() {
            if (File.Exists(FullAssetBundleInfosSavePath)) {
                string json = File.ReadAllText(FullAssetBundleInfosSavePath);
                _infos = JsonUtility.FromJson<AssetBundleInfos>(json);
            } else {
                Debug.LogError($"Load AssetBundleInfos failed. File not exist. path={FullAssetBundleInfosSavePath}");
                _infos = null;
            }
        }

        public bool IsABExists(string bundleName) {
            if (_infos == null || _infos.InfoList.Count <= 0)
                return false;
            return _infos.InfoList.Any(info => info.BundleName.Equals(bundleName));
        }

        public bool TryGetABInfo(string bundleName, out AssetBundleInfoUnit infoUnit) {
            infoUnit = null;
            if (_infos == null || _infos.InfoList.Count <= 0)
                return false;

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (AssetBundleInfoUnit info in _infos.InfoList) {
                if (info.BundleName != bundleName) continue;
                infoUnit = info;
                return true;
            }

            return false;
        }

        public SubABUnit GetSubABUnit(string bundleName) {
            if (!TryGetABInfo(bundleName, out AssetBundleInfoUnit infoUnit))
                return null;
            if (_subABUnitList.TryGetValue(bundleName, out SubABUnit unit))
                return unit;
            SubABUnit abUnit = new SubABUnit(URL, infoUnit.Hash, infoUnit.BundleName, infoUnit.CRC, SavePath);
            _subABUnitList.Add(bundleName, abUnit);
            return abUnit;
        }
    }
}
