using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace Game {
    [Serializable]
    public class AssetBundleInfos {
        public List<AssetBundleInfoUnit> InfoList = new();
    }

    [Serializable]
    public struct AssetBundleInfoUnit {
        public string BundleName;
        public uint CRC;
    }
}
