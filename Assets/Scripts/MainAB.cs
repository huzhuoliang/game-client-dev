using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using DefaultNamespace;
using Sirenix.OdinInspector;
using System.IO;
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
            if (_infos == null) {
                Debug.LogError("Infos is null. Please load AssetBundleInfos before Load AssetBundle.");
                yield break;
            }

            if (!_infos.TryGet(abName, out AssetBundleInfoUnit unit)) {
                Debug.LogError($"Unknown AssetBundle name \"{abName}\"");
                yield break;
            }

            if (unit == null) {
                Debug.LogError($"AssetBundleInfoUnit is null. name \"{abName}\"");
                yield break;
            }

            yield return LoadAB(unit);
        }

        private IEnumerator LoadAB([NotNull] AssetBundleInfoUnit unit) {
            if (unit == null) {
                throw new ArgumentNullException(nameof(unit));
            }

            if (_subABUnitList.Contains(unit)) {
                yield break;
            }

            SubABUnit subABUnit = new SubABUnit(URL, unit, SavePath);
            _subABUnitList.Add(subABUnit);
            yield return subABUnit.StartDownloadCoroutine();

            // TODO Load
        }

        public IEnumerator LoadInfosAsync() {
            if (_infos == null) {
                _request = UnityWebRequestAssetBundle.GetAssetBundle(FullABInfosURL);
                _request.downloadHandler = new DownloadHandlerFile(FullABInfosSavePath);
                yield return _request.SendWebRequest();
                if (!_request.isDone) {
                    Debug.LogError($"Download Failed. url:{FullABInfosURL} savePath:{FullABInfosSavePath}");
                }
            }

            if (!LoadABInfos()) {
                Debug.LogError("LoadABInfos Failed.");
            }
        }

        public bool LoadABInfos() {
            if (!File.Exists(FullABInfosSavePath)) {
                Debug.LogError($"Load AssetBundleInfos failed. File not exist. path={FullABInfosSavePath}");
                _infos = null;
                return false;
            }

            string json = File.ReadAllText(FullABInfosSavePath);
            if (json.Length <= 0) {
                Debug.LogError($"AssetBundle info json file is empty. ({FullABInfosSavePath})");
                return false;
            }

            try {
                _infos = JsonUtility.FromJson<AssetBundleInfos>(json);
                return true;
            } catch (Exception e) {
                _infos = null;
                Debug.LogException(e);
                return false;
            }
        }

        public void Dispose() {
            _subABUnitList.Dispose();
        }
    }
}
