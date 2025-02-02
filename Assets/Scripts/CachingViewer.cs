using Sirenix.OdinInspector;
using UnityEngine;

namespace DefaultNamespace {
    public class CachingViewer : MonoBehaviour {
        [ShowInInspector]
        [DisplayAsString]
        private int Count => Caching.cacheCount;

        [ShowInInspector]
        [BoxGroup("Current")]
        [DisplayAsString]
        private string Index => Caching.cacheCount <= 0 ? "" : Caching.currentCacheForWriting.index.ToString();

        [ShowInInspector]
        [BoxGroup("Current")]
        [DisplayAsString]
        private string Path => Caching.cacheCount <= 0 ? "" : Caching.currentCacheForWriting.path;


        [ShowInInspector]
        [DisplayAsString]
        [BoxGroup("Current")]
        private string Space {
            get {
                if (Caching.cacheCount <= 0) {
                    return "";
                }

                string spaceOccupied = Caching.currentCacheForWriting.spaceOccupied.FormatByte();
                string spaceTotal = Caching.currentCacheForWriting.maximumAvailableStorageSpace.FormatByte();

                return $"{spaceOccupied}/{spaceTotal}";
            }
        }

        [Button]
        private void ClearCache() {
            Caching.ClearCache();
        }

        private void ClearCachedVersion(string abName, Hash128 hash) {
            Caching.ClearCachedVersion(abName, hash);
        }

        [Button]
        private void ClearAllCachedVersions(string abName) {
            Caching.ClearAllCachedVersions(abName);
        }
    }
}
