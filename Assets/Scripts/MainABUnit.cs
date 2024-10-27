using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

namespace Game {
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
        public string InfosDownloadedBytesStr => $"{InfosDownloadedBytes.FormatByte()} ({InfosDownloadedProgress * 100f:0.00}%)";

        public const string AssetBundleInfoFileName = "AssetBundleInfo.json";

        public string FullAssetBundleInfosURL => Path.Combine(URL, AssetBundleInfoFileName);
        public string FullAssetBundleInfosSavePath => Path.Combine(SavePath, AssetBundleInfoFileName);
        public ulong InfosDownloadedBytes => _infosRequest?.downloadedBytes ?? 0;

        private UnityWebRequest _infosRequest;

        public float InfosDownloadedProgress => _infosRequest?.downloadProgress ?? 0f;

        public MainABUnit(string url, string name, string savePath, string saveFileName = "")
                : base(url, name, savePath, saveFileName) {
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
            string infoPath = Path.Combine(SavePath, AssetBundleInfoFileName);
            if (File.Exists(infoPath)) {
                string json = File.ReadAllText(infoPath);
                _infos = JsonUtility.FromJson<AssetBundleInfos>(json);
            } else {
                _infos = null;
            }
        }
    }
}
