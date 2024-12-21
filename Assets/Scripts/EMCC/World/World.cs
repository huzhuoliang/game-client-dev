using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EMCC.World {
    public class World : MonoBehaviour {
        [ShowInInspector]
        public IReadOnlyDictionary<Vector3Int, WorldBlock> WorldBlocks => _worldBlocks;

        private readonly Dictionary<Vector3Int, WorldBlock> _worldBlocks = new();

        private void OnValidate() {
            ResetTransform();
            transform.hideFlags = HideFlags.NotEditable;
        }

        public void Awake() {
            ResetTransform();
            InitWorldBlocks(Vector3Int.zero);
        }

        private void InitWorldBlocks(Vector3Int centerIndex) {
            for (int ix = -1; ix <= 1; ix++) {
                for (int iy = -1; iy <= 1; iy++) {
                    for (int iz = -1; iz <= 1; iz++) {
                        Vector3Int index = new Vector3Int(ix, iy, iz);
                        GenerateWorldBlocks(index, index);
                    }
                }
            }
        }

        private void GenerateWorldBlocks(Vector3Int logicIndex, Vector3Int realIndex) {
            if (TryGetWorldBlockLogic(logicIndex, out WorldBlock _)) {
                Debug.LogError($"World (LogicIndex={logicIndex}) already exist.");
                return;
            }

            // ReSharper disable once UseObjectOrCollectionInitializer
            GameObject obj = new($"B_{logicIndex}");
            obj.transform.parent = transform;
            WorldBlock wb = obj.AddComponent<WorldBlock>();
            try {
                wb.Init(logicIndex, realIndex);
            } catch (Exception e) {
                Debug.LogError($"WorldBlock \"{obj.name}\" init failed.");
            }

            _worldBlocks[logicIndex] = wb;
        }

        private bool TryGetWorldBlockLogic(Vector3Int logicIndex, out WorldBlock worldBlock) {
            worldBlock = null;
            if (_worldBlocks.TryGetValue(logicIndex, out WorldBlock block)) {
                worldBlock = block;
            }

            return worldBlock != null;
        }

        private bool TryGetWorldBlockReal(Vector3Int realIndex, out WorldBlock worldBlock) {
            worldBlock = null;
            foreach (KeyValuePair<Vector3Int, WorldBlock> kv in _worldBlocks) {
                WorldBlock wb = kv.Value;
                // ReSharper disable once InvertIf
                if (wb != null && wb.RealIndex == realIndex) {
                    worldBlock = wb;
                    break;
                }
            }

            return worldBlock != null;
        }

        private void ResetTransform() {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
