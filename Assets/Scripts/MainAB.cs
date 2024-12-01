using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using Sirenix.OdinInspector;
using System.IO;
using System.Linq;
using TMPro;
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

        [ShowInInspector]
        [ValueDropdown(nameof(GetAllAssetBundleInfoUnit), AppendNextDrawer = true)]
        [HideLabel]
        [InlineButton(nameof(OdinLoadAB), "Load")]
        private string _loadABName;

        private UnityWebRequest _request;
        public ulong DownloadBytes => _request?.downloadedBytes ?? 0;
        public float DownloadProgress => _request?.downloadProgress ?? 0f;

        public MainAB(string url, string savePath) {
            _url = url;
            _savePath = savePath;
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

        private void OdinLoadAB() {
            Debug.LogError($"============ Load {_loadABName}");
        }

        private IEnumerable GetAllAssetBundleInfoUnit() {
            return _infos?.InfoList.Select(v => v.BundleName);

            //            foreach (AssetBundleInfoUnit info in _infos.InfoList) {
            //                yield return info;
            //            }
        }
    }
}
