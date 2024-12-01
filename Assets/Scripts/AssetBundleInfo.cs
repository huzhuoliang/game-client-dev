using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable InconsistentNaming

namespace Game {
    [Serializable]
    public class AssetBundleInfos {
        [ListDrawerSettings(IsReadOnly = true)]
        [LabelText("All AssetBundle Names")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<AssetBundleInfoUnit> InfoList = new();

        public bool Contains(string bundleName) {
            foreach (AssetBundleInfoUnit info in InfoList) {
                if (info.BundleName == bundleName)
                    return true;
            }

            return false;
        }

        public bool TryGet(string bundleName, out AssetBundleInfoUnit unit) {
            unit = default;
            foreach (var info in InfoList) {
                if (info.BundleName != bundleName)
                    continue;
                unit = info;
                return true;
            }

            return false;
        }
    }

    [Serializable]
    [HideReferenceObjectPicker]
    public class AssetBundleInfoUnit : ISearchFilterable {
        [HideInInspector]
        public string BundleName;

        [HideInInspector]
        public ulong Bytes;

        [HideInInspector]
        public uint CRC;

        [HideInInspector]
        public Hash128 Hash;

        [ShowInInspector]
        [DisplayAsString]
        [HideLabel]
        private string displayStr => $"{BundleName} [ Size: {Bytes.FormatByte()} ] [ CRC: {CRC} ] [ Hash: {Hash} ]";

        public bool IsMatch(string searchString) {
            return displayStr.Contains(searchString);
        }

        public override string ToString() {
            return displayStr;
        }
    }
}
