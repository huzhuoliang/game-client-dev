using Sirenix.OdinInspector;
using UnityEngine;

namespace EMCC {
    public class EmccTestScript : MonoBehaviour {
        public Rigidbody rb;


        public Vector3 force;

        [Button]
        [DisableInEditorMode]
        public void Push() {
            rb.AddForce(force);
        }
    }
}
