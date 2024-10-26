using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using Game.Util;

[Serializable]
public class TestDownloader : MonoBehaviour {
    [SerializeField]
    [LabelText("URL")]
    private string url = "";

    private string FullURL {
        get {
            if (url.EndsWith("/") || url.EndsWith("\\")) {
                return url + buildTarget;
            }

            return url + "/" + buildTarget;
        }
    }

    [LabelText("Target Platform")]
    public BuildTarget buildTarget;

    [SerializeField]
    [DisplayAsString]
    private string savePath = "";

    private string FullSavePath => Path.Combine(savePath, "bundle", buildTarget.ToString());

    [SerializeField]
    [LabelText("AssetBundle Name List")]
    private List<string> bundleNameList = new();


    [SerializeField]
    [ListDrawerSettings(IsReadOnly = true)]
    [Searchable]
    [DisplayAsString]
    [LabelText("AssetBundles")]
    private List<string> bundleNames = new();

    [ShowInInspector]
    [DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.OneLine)]
    [LabelText("Downloaded Bytes")]
    [NonSerialized]
    private Dictionary<string, DownloadBytes> _downloadedBytes = new();

    [NonSerialized]
    private readonly List<UnityWebRequest> _requests = new();

    private void Awake() {
        savePath = Application.persistentDataPath;
    }

    private void OnDestroy() {
        StopAllWebRequest();
    }

    [Button]
    private void OpenSavePath() {
        Util.OpenFolder(savePath);
    }

    private void StopAllWebRequest() {
        foreach (UnityWebRequest request in _requests) {
            if (!request.isDone) {
                request.Abort();
            }

            request.Dispose();
        }

        _requests.Clear();
    }

    [DisableInEditorMode]
    [Button("下载")]
    private void StartDownload() {
        bundleNames.Clear();
        _downloadedBytes.Clear();
        StopAllWebRequest();
        Debug.Log($"Start download \"{FullURL}\"");
        foreach (string bundleName in bundleNameList) {
            _downloadedBytes.Add(bundleName, new DownloadBytes {
                    BundleName = bundleName,
                    ByteCount = ""
            });
            StartCoroutine(CoDownload(FullURL, bundleName));
        }
    }

    private void UpdateDownloadedBytes(string bundleName, string byteCount, float progress) {
        if (!_downloadedBytes.TryGetValue(bundleName, out var c))
            return;
        c.ByteCount = byteCount;
        c.Progress = progress;
        _downloadedBytes[bundleName] = c;
    }

    private IEnumerator CoDownload(string bundleURL, string bundleName) {
        UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(Path.Combine(bundleURL, bundleName));
        _requests.Add(webRequest);
        webRequest.downloadHandler = new DownloadHandlerFile(Path.Combine(FullSavePath, bundleName));
        webRequest.SendWebRequest();
        while (!webRequest.isDone) {
            UpdateDownloadedBytes(bundleName, webRequest.downloadedBytes.FormatByte(), webRequest.downloadProgress);
            yield return null;
        }

        UpdateDownloadedBytes(bundleName, webRequest.downloadedBytes.FormatByte(), webRequest.downloadProgress);

        Debug.Log($"Bundle \"{bundleName}\" download finished.");
        if (webRequest.result != UnityWebRequest.Result.Success) {
            Debug.LogError(webRequest.error);
            yield break;
        }

        try {
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(FullSavePath, bundleName));
            string[] assetNames = bundle.GetAllAssetNames();
            foreach (string assetName in assetNames) {
                bundleNames.Add(bundle.name + ": " + assetName);
            }
        } catch (Exception e) {
            Debug.LogError($"加载AB时出错：{e}");
        }
    }

    public enum BuildTarget {
        StandaloneWindows64,
        IOS,
        Android,
    }

    public struct DownloadBytes {
        [DisplayAsString]
        public string BundleName;

        [DisplayAsString]
        public string ByteCount;

        [DisplayAsString]
        public float Progress;
    }
}
