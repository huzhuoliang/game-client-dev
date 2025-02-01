using Game;
using UnityEngine;

namespace DefaultNamespace {
    public class SubABUnit : ABUnit {
        public SubABUnit() {
        }

        public SubABUnit(string url, Hash128 hash, string name, uint crc, string savePath, string saveFileName = "") :
                base(url, hash, name, crc, savePath, saveFileName) {
        }

        public SubABUnit(string url, AssetBundleInfoUnit unit, string savePath, string saveFileName = "") :
                base(url, unit.Hash, unit.BundleName, unit.CRC, savePath, saveFileName) {
        }
    }
}
