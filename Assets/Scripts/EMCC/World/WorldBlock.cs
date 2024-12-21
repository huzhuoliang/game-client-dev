using Sirenix.OdinInspector;
using UnityEngine;

namespace EMCC.World {
    public class WorldBlock : MonoBehaviour {
        [ShowInInspector]
        public Vector3Int RealIndex => _realIndex;

        [ShowInInspector]
        public Vector3Int LogicIndex => _logicIndex;

        public const float BlockSize = WorldConst.BlockSize;

        private Vector3Int _realIndex;

        private Vector3Int _logicIndex;

        public void Init(Vector3Int logicIndex, Vector3Int realIndex) {
            _logicIndex = logicIndex;
            _realIndex = realIndex;
            UpdatePosition(_realIndex);
        }

        private void UpdatePosition(Vector3Int index) {
            transform.position = new Vector3(index.x * BlockSize, index.y * BlockSize, index.z * BlockSize);
        }
    }
}
