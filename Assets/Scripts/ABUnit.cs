using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

namespace Game {
    [Serializable]
    public class ABUnit : IDisposable {
        [ShowInInspector]
        [DisplayAsString]
        [PropertyOrder(1)]
        public string BundleName => _bundleName;

        [ShowInInspector]
        [DisplayAsString]
        [PropertyOrder(2)]
        public string ManifestName => _bundleName + ".manifest";

        [ShowInInspector]
        [DisplayAsString]
        [LabelText("Download Status")]
        [PropertyOrder(100)]
        public string DownloadedBytesStr => $"{DownloadedBytes.FormatByte()} ({DownloadedProgress * 100f:0.00}%)";

        [ShowInInspector]
        [DisplayAsString]
        [LabelText("Manifest Download Status")]
        [PropertyOrder(101)]
        public string ManifestDownloadedBytesStr =>
                $"{ManifestDownloadedBytes.FormatByte()} ({ManifestDownloadedProgress * 100f:0.00}%)";

        [ShowInInspector]
        [PropertyOrder(200)]
        public bool IsDownloaded => GetIsDownloadedInternal();

        [ProgressBar(0f, 1f, DrawValueLabel = false, ColorGetter = nameof(TotalProgressColorGetter))]
        [ShowInInspector]
        [HideLabel]
        [PropertyOrder(299)]
        public float TotalProgress => GetTotalProgressInternal();

        [ShowInInspector]
        [PropertyOrder(300)]
        public bool IsLoaded => _isLoaded;

        public ulong DownloadedBytes => _webRequest?.downloadedBytes ?? 0;

        public float DownloadedProgress => _webRequest?.downloadProgress ?? 0f;

        public ulong ManifestDownloadedBytes => _manifestWebRequest?.downloadedBytes ?? 0;

        public float ManifestDownloadedProgress => _manifestWebRequest?.downloadProgress ?? 0f;

        public UnityWebRequest.Result? DownloadResult => _webRequest?.result;

        public string DownloadError => _webRequest?.error ?? "";

        public string FullURL => Path.Combine(URL, BundleName);
        public string FullManifestURL => Path.Combine(URL, ManifestName);
        public string FullSavePath => Path.Combine(SavePath, BundleName);
        public string FullManifestSavePath => Path.Combine(SavePath, ManifestName);

        private string _bundleName;
        protected string URL;
        protected string SavePath;
        private UnityWebRequest _webRequest;
        private UnityWebRequest _manifestWebRequest;
        private bool _isLoaded;


        private AssetBundle _assetBundle;
        private AssetBundleManifest _assetBundleManifest;

        public ABUnit() {
            URL = "";
            _bundleName = "";
            SavePath = "";
        }

        public ABUnit(string url, string name, string savePath, string saveFileName = "") {
            URL = url;
            _bundleName = name;
            SavePath = savePath;
        }

        protected virtual bool GetIsDownloadedInternal() {
            return DownloadedProgress >= 1f && ManifestDownloadedProgress >= 1f;
        }

        protected virtual float GetTotalProgressInternal() {
            return Mathf.Min(DownloadedProgress, ManifestDownloadedProgress);
        }

        public bool StartDownload() {
            try {
                _webRequest = UnityWebRequestAssetBundle.GetAssetBundle(FullURL);
                _webRequest.downloadHandler = new DownloadHandlerFile(FullSavePath);
                _webRequest.SendWebRequest();
                _manifestWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(FullManifestURL);
                _manifestWebRequest.downloadHandler = new DownloadHandlerFile(FullManifestSavePath);
                _manifestWebRequest.SendWebRequest();
                return OnStartDownload();
            } catch (Exception e) {
                Debug.LogError($"Create Web Request Failed.\n{e}");
                return false;
            }
        }

        protected virtual bool OnStartDownload() {
            return true;
        }

        public bool IsDownloadDone() {
            if (_webRequest == null || _manifestWebRequest == null) {
                throw new Exception("Please call StartDownload() before IsDownloadDone().");
            }

            return _webRequest.isDone && _manifestWebRequest.isDone && IsSubClassDownloadDone();
        }

        public virtual bool IsSubClassDownloadDone() {
            return true;
        }

        public bool Load() {
            if (!IsDownloaded)
                return false;
            if (IsLoaded)
                return true;
            try {
                _assetBundle = AssetBundle.LoadFromFile(FullSavePath);

                //                _assetBundleManifest = _assetBundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest");
                //                Debug.LogError($"Manifest ({_assetBundleManifest.name}) 加载完成.");
                //                string[] assets = _assetBundleManifest.GetAllAssetBundles();
                //                Debug.LogErrorFormat("All AssetBundles:\n{0}", string.Join(",\n", assets));
                //                foreach (string assetName in assetNames) {
                //                    string[] deps = _assetBundleManifest.GetDirectDependencies(assetName);
                //                    Debug.LogErrorFormat("{0} -> {{{1}}}", assetName, string.Join(",", deps));
                //                }

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

        protected Color TotalProgressColorGetter(float value) {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (value >= 1f)
                return new Color(30f / 255f, 132f / 255f, 73 / 255f);
            else
                return new Color(212f / 255f, 172f / 255f, 13f / 255f);
        }
    }
}
