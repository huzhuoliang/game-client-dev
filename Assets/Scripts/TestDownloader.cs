using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using System;

[Serializable]
public class TestDownloader : MonoBehaviour {
    [SerializeField]
    private string url = "";

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
    [DisplayAsString]
    private Dictionary<string, string> _downloadedBytesDic = new();

    private void Awake() {
        savePath = Application.persistentDataPath;
    }

    [DisableInEditorMode]
    [Button("下载")]
    private void StartDownload() {
        bundleNames.Clear();
        _downloadedBytesDic.Clear();
        Debug.Log("Start download");
        foreach (string bundleName in bundleNameList) {
            _downloadedBytesDic.Add(bundleName, "");
            StartCoroutine(CoDownload(url, bundleName));
        }
    }

    private IEnumerator CoDownload(string bundleURL, string bundleName) {
        UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(bundleURL + bundleName);
        webRequest.downloadHandler = new DownloadHandlerFile(savePath + bundleName);
        webRequest.SendWebRequest();
        while (!webRequest.isDone) {
            _downloadedBytesDic[bundleName] = webRequest.downloadedBytes.FormatByte();
            yield return null;
        }

        _downloadedBytesDic[bundleName] = webRequest.downloadedBytes.FormatByte();

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
}