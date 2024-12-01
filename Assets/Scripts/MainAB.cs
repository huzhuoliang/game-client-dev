using System;
using System.Collections;
using DefaultNamespace;
using Sirenix.OdinInspector;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable MemberCanBePrivate.Global

namespace Game {
    [Serializable]
    public class MainAB : IDisposable {
        [ShowInInspector]
        [LabelText("URL")]
        [DisplayAsString]
        public string URL => _url;

        [ShowInInspector]
        [LabelText("SavePath")]
        [DisplayAsString]
        public string SavePath => _savePath;

        public const string ABInfoFileName = "AssetBundleInfo.json";

        [ShowInInspector]
        [LabelText("FullABInfosURL")]
        [DisplayAsString]
        public string FullABInfosURL {
            get {
                string url = URL;
                if (!url.EndsWith("/")) {
                    url += "/";
                }

                return url + ABInfoFileName;
            }
        }

        [ShowInInspector]
        [LabelText("FullABInfosSavePath")]
        [DisplayAsString]
        public string FullABInfosSavePath => Path.Combine(SavePath, ABInfoFileName);

        private string _url;
        private string _savePath;

        [NonSerialized]
        [ShowInInspector]
        [HideLabel]
        [HideReferenceObjectPicker]
        [PropertyOrder(100)]
        [HideIf("@this._infos == null")]
        private AssetBundleInfos _infos;

        public AssetBundleInfos Infos => _infos;

        [NonSerialized]
        [ShowInInspector]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [HideLabel]
        [HideIf("@this._infos == null")]
        [PropertyOrder(101)]
        private SubABUnitList _subABUnitList = new();

        [ShowInInspector]
        [DisplayAsString]
        [LabelText("Download Status")]
        public string DownloadBytesStr => $"{DownloadBytes.FormatByte()} ({DownloadProgress * 100f:0.00}%)";

        private UnityWebRequest _request;
        public ulong DownloadBytes => _request?.downloadedBytes ?? 0;
        public float DownloadProgress => _request?.downloadProgress ?? 0f;

        public MainAB(string url, string savePath) {
            _url = url;
            _savePath = savePath;
        }

        public IEnumerator LoadABAsync(string abName) {
            if (!_infos.TryGet(abName, out AssetBundleInfoUnit unit)) {
                Debug.LogError($"Unknown AssetBundle name \"{abName}\"");
                yield break;
            }

            Debug.LogError($"============ 加载 {unit}");
        }

        public IEnumerator LoadInfosAsync() {
            if (_infos == null) {
                if (!StartDownloadABInfos()) {
                    Debug.LogError("Start Download Failed.");
                    yield break;
                }

                WaitForEndOfFrame waitForEndOfFrame = new();
                while (!_request.isDone) {
                    yield return waitForEndOfFrame;
                }
            }

            if (!LoadABInfos()) {
                Debug.LogError("LoadABInfos Failed.");
            }

            Debug.Log("Load success.");
        }

        public bool StartDownloadABInfos() {
            try {
                _request = UnityWebRequestAssetBundle.GetAssetBundle(FullABInfosURL);
                _request.downloadHandler = new DownloadHandlerFile(FullABInfosSavePath);
                _request.SendWebRequest();
                return true;
            } catch (Exception e) {
                Debug.LogError($"Create web request for AssetBundleInfos failed.\n{e}");
                return false;
            }
        }

        public bool LoadABInfos() {
            if (File.Exists(FullABInfosSavePath)) {
                string json = File.ReadAllText(FullABInfosSavePath);
                _infos = JsonUtility.FromJson<AssetBundleInfos>(json);
                return true;
            } else {
                Debug.LogError($"Load AssetBundleInfos failed. File not exist. path={FullABInfosSavePath}");
                _infos = null;
                return false;
            }
        }

        public void Dispose() {
            _subABUnitList.Dispose();
        }
    }
}
