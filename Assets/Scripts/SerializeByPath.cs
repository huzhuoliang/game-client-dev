using System;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DefaultNamespace {
    [Serializable]
    public class SerializeByPath<T> where T : Object {
        public T Asset {
            get {
                if (TryLoadAs(AssetPath, out T asset)) {
                    return asset;
                }

                return null;
            }
        }

        public string AssetPath {
            get => assetPath;
            set => assetPath = value;
        }

        [SerializeField]
        private string assetPath;

        private static bool TryLoadAs<TLoad>(string assetPath, out TLoad result) where TLoad : Object {
            result = null;
            if (string.IsNullOrEmpty(assetPath)) {
                return false;
            }

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath))) {
                return false;
            }

            result = AssetDatabase.LoadAssetAtPath<TLoad>(assetPath);
#else
            TryLoadAsRuntime(assetPath, out result);
#endif
            return result != null;
        }

        private static bool TryLoadAsRuntime<TLoad>(string assetPath, out TLoad result) where TLoad : Object {
            // TODO Load runtime
            result = null;
            return false;
        }
    }
}
