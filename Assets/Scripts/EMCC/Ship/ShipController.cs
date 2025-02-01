using System;
using UnityEngine;

namespace EMCC.Ship {
    [RequireComponent(typeof(Rigidbody))]
    public class ShipController : MonoBehaviour {
        private Rigidbody _rigidbody;

        private void Awake() {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate() {
            if (Input.GetKey(KeyCode.W)) {
                _rigidbody.AddRelativeForce(Vector3.forward);
            }

            if (Input.GetKey(KeyCode.S)) {
                _rigidbody.AddRelativeForce(Vector3.back);
            }

            if (Input.GetKey(KeyCode.A)) {
                _rigidbody.AddRelativeTorque(Vector3.down * 0.1f);
            }

            if (Input.GetKey(KeyCode.D)) {
                _rigidbody.AddRelativeTorque(Vector3.up * 0.1f);
            }
        }
    }
}
