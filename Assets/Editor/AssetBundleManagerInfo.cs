using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEngine;

[Serializable]
public class AssetBundleManagerInfo {

    private AssetBundle assetBundle = null;

    [LabelText("@this.GetAssetName()")]
    [DisplayAsString]
    [ListDrawerSettings(
        IsReadOnly = true,
        HideAddButton = true,
        HideRemoveButton = true,
        OnTitleBarGUI = "OnTitleBarGUI"
    )]
    public string[] objects;

    public AssetBundleManagerInfo() { }

    public AssetBundleManagerInfo(AssetBundle assetBundle) {
        this.assetBundle = assetBundle;
        objects = assetBundle.GetAllAssetNames();
    }

    private string GetAssetName() {
        if (assetBundle == null)
            return "null";
        return assetBundle.name;
    }

    private void OnTitleBarGUI() {
        if (SirenixEditorGUI.ToolbarButton(SdfIconType.XCircle)) {
            UnLoad();
        }
    }

    public void UnLoad() {
        if (assetBundle != null) {
            assetBundle.Unload(true);
        }
        assetBundle = null;
        objects = null;
    }
}
