using Game;
using UnityEngine;

namespace DefaultNamespace {
    public class SubABUnit : ABUnit {
        public SubABUnit() : base() {
        }

        public SubABUnit(string url, Hash128 hash, string name, uint crc, string savePath, string saveFileName = "") :
                base(url, hash, name, crc, savePath, saveFileName) {
        }
    }
}
