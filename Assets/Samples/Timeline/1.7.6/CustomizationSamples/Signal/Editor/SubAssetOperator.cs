using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace Timeline.Samples {
    public class SubAssetOperator : MonoBehaviour {
        public TimelineAsset timelineAsset;

        [SerializeField]
        private List<SubAssetReference> subAssets;


        [Button]
        private void Show() {
            subAssets.Clear();
            if (ReferenceEquals(timelineAsset, null)) {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(timelineAsset);
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach (Object asset in allAssets) {
                if (ReferenceEquals(asset, timelineAsset)) {
                    continue;
                }

//                ScriptableObject scriptableObject = asset as ScriptableObject;
//                if (ReferenceEquals(scriptableObject, null)) {
//                    continue;
//                }
                SignalAsset signalAsset = asset as SignalAsset;
                if (ReferenceEquals(signalAsset, null)) {
                    continue;
                }

                subAssets.Add(new SubAssetReference {
                        asset = signalAsset,
                });
            }
        }
    }

    [Serializable]
    internal class SubAssetReference {
        [HorizontalGroup("Group")]
        [HideLabel]
        [ReadOnly]
        public ScriptableObject asset;

        [HorizontalGroup("Group")]
        [Button]
        private void Delete() {
            if (ReferenceEquals(asset, null)) {
                return;
            }

            AssetDatabase.RemoveObjectFromAsset(asset);
            AssetDatabase.SaveAssets();
            asset = null;
        }
    }
}
