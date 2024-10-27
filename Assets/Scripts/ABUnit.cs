using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

namespace Game {
    [Serializable]
    public class ABUnit : IDisposable {
        [ShowInInspector]
        [DisplayAsString]
        public string BundleName => _bundleName;

        [ShowInInspector]
        [DisplayAsString]
        public string ManifestName => _bundleName + ".manifest";

        [ShowInInspector]
        [DisplayAsString]
        [LabelText("Download Status")]
        public string DownloadedBytesStr => $"{DownloadedBytes.FormatByte()} ({DownloadedProgress * 100f:0.00}%)";

        [ShowInInspector]
        [DisplayAsString]
        [LabelText("Manifest Download Status")]
        public string ManifestDownloadedBytesStr =>
                $"{ManifestDownloadedBytes.FormatByte()} ({ManifestDownloadedProgress * 100f:0.00}%)";

        [ShowInInspector]
        public bool IsDownloaded => DownloadedProgress >= 1f && ManifestDownloadedProgress >= 1f;

        [ProgressBar(0f, 1f, DrawValueLabel = false, ColorGetter = nameof(TotalProgressColorGetter))]
        [ShowInInspector]
        [HideLabel]
        public float TotalProgress => Mathf.Min(DownloadedProgress, ManifestDownloadedProgress);

        [ShowInInspector]
        public bool IsLoaded => _isLoaded;

        public ulong DownloadedBytes => _webRequest?.downloadedBytes ?? 0;

        public float DownloadedProgress => _webRequest?.downloadProgress ?? 0f;

        public ulong ManifestDownloadedBytes => _manifestWebRequest?.downloadedBytes ?? 0;

        public float ManifestDownloadedProgress => _manifestWebRequest?.downloadProgress ?? 0f;

        public UnityWebRequest.Result? DownloadResult => _webRequest?.result;

        public string DownloadError => _webRequest?.error ?? "";

        public string FullURL => Path.Combine(_url, BundleName);
        public string FullManifestURL => Path.Combine(_url, ManifestName);
        public string FullSavePath => Path.Combine(_savePath, BundleName);
        public string FullManifestSavePath => Path.Combine(_savePath, ManifestName);

        private string _bundleName;
        private string _url;
        private string _savePath;
        private UnityWebRequest _webRequest;
        private UnityWebRequest _manifestWebRequest;
        private bool _isLoaded;


        private AssetBundle _assetBundle;
        private AssetBundleManifest _assetBundleManifest;

        public ABUnit(string url, string name, string savePath, string saveFileName = "") {
            _url = url;
            _bundleName = name;
            _savePath = savePath;
        }

        public bool StartDownload() {
            try {
                _webRequest = UnityWebRequestAssetBundle.GetAssetBundle(FullURL);
                _webRequest.downloadHandler = new DownloadHandlerFile(FullSavePath);
                _webRequest.SendWebRequest();
                _manifestWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(FullManifestURL);
                _manifestWebRequest.downloadHandler = new DownloadHandlerFile(FullManifestSavePath);
                _manifestWebRequest.SendWebRequest();
                return true;
            } catch (Exception e) {
                Debug.LogError($"Create Web Request Failed.\n{e}");
                return false;
            }
        }

        public bool IsDownloadDone() {
            if (_webRequest == null || _manifestWebRequest == null) {
                throw new Exception("Please call StartDownload() before IsDownloadDone().");
            }

            return _webRequest.isDone && _manifestWebRequest.isDone;
        }

        public bool Load() {
            if (!IsDownloaded)
                return false;
            if (IsLoaded)
                return true;
            try {
                // Load Manifest

                // Load Bundle
                _assetBundle = AssetBundle.LoadFromFile(FullSavePath);
                string[] assetNames = _assetBundle.GetAllAssetNames();
                foreach (string assetName in assetNames) {
                    Debug.LogError($"{BundleName}: {assetName}");
                }

                _assetBundleManifest = _assetBundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest");
                Debug.LogError($"Manifest ({_assetBundleManifest.name}) 加载完成.");
                string[] assets = _assetBundleManifest.GetAllAssetBundles();
                Debug.LogErrorFormat("All AssetBundles:\n{0}", string.Join(",\n", assets));
                foreach (string assetName in assetNames) {
                    string[] deps = _assetBundleManifest.GetDirectDependencies(assetName);
                    Debug.LogErrorFormat("{0} -> {{{1}}}", assetName, string.Join(",", deps));
                }

                return true;
            } catch (Exception e) {
                Debug.LogError($"Error When Loading Bundle \"{BundleName}\".\n {e}");
                return false;
            }
        }

        public void UnloadAssetBundle(bool unloadAllLoadedObjects = true) {
            if (!IsDownloaded || IsLoaded || _assetBundle == null)
                return;
            _assetBundle.UnloadAsync(unloadAllLoadedObjects);
            _assetBundle = null;
        }

        public void Dispose() {
            UnloadAssetBundle();
            _webRequest?.Dispose();
        }

        private Color TotalProgressColorGetter(float value) {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (value >= 1f)
                return new Color(30f / 255f, 132f / 255f, 73 / 255f);
            else
                return new Color(212f / 255f, 172f / 255f, 13f / 255f);
        }
    }
}
