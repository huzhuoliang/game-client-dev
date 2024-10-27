using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Game {
    [Serializable]
    public class AssetBundleInfos {
        [ListDrawerSettings(IsReadOnly = true)]
        [LabelText("All AssetBundle Names")]
        public List<AssetBundleInfoUnit> InfoList = new();
    }

    [Serializable]
    [HideReferenceObjectPicker]
    public class AssetBundleInfoUnit {
        [HideInInspector]
        public string BundleName;

        [HideInInspector]
        public uint CRC;

        [ShowInInspector]
        [DisplayAsString]
        [HideLabel]
        private string displayStr => $"{BundleName} [CRC: {CRC}]";
    }
}
