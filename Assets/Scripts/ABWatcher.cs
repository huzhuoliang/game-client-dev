using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Game {
    public class ABWatcher : MonoBehaviour {
        [NonSerialized]
        [ShowInInspector]
        [HideInEditorMode]
        [LabelText("Loaded AssetBundles")]
        [ListDrawerSettings(IsReadOnly = true)]
        private List<ABWatcherData> _dataList = new();

        [Button]
        [HideInEditorMode]
        private void GetAll() {
            _dataList.Clear();
            IEnumerable<AssetBundle> bundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (AssetBundle bundle in bundles) {
                _dataList.Add(new ABWatcherData {
                        BundleName = bundle.name,
                        AssetNames = bundle.GetAllAssetNames().ToList(),
                });
            }
        }
    }

    [Serializable]
    [HideReferenceObjectPicker]
    public sealed class ABWatcherData {
        [DisplayAsString]
        [LabelText("Bundle Name")]
        public string BundleName;

        [DisplayAsString]
        [ListDrawerSettings(IsReadOnly = true)]
        [LabelText("Asset Names")]
        public List<string> AssetNames;
    }
}
