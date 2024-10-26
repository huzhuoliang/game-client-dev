using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using System;

[Serializable]
public class TestDownloader : MonoBehaviour {
    [SerializeField]
    [LabelText("URL")]
    private string url = "";

    private string FullURL {
        get {
            string t = buildTarget.ToString();
            if (!url.EndsWith("/")) {
                t = "/" + t;
            }

            return url + t;
        }
    }

    [LabelText("Target Platform")]
    public BuildTarget buildTarget = BuildTarget.StandaloneWindows64;

    [SerializeField]
    [DisplayAsString]
    private string savePath = "";

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

    private void Awake() {
        savePath = Application.persistentDataPath;
    }

    [DisableInEditorMode]
    [Button("下载")]
    private void StartDownload() {
        bundleNames.Clear();
        _downloadedBytes.Clear();
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
        string dir = bundleURL.EndsWith("/") ? bundleURL : bundleURL + "/";
        UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(dir + bundleName);
        webRequest.downloadHandler = new DownloadHandlerFile(savePath + bundleName);
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
            AssetBundle bundle = AssetBundle.LoadFromFile(savePath + bundleName);
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
