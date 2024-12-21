using System;
using EMCC;
using Sirenix.OdinInspector;
using UnityEngine;

public class EmccObject : MonoBehaviour {
    [NonSerialized]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Velocity")]
    private Vector3 _velocity = Vector3.zero;

    private Vector3 _currPosition;

    private void Awake() {
        _currPosition = transform.position;
    }

    private void OnValidate() {
        transform.localScale = Vector3.one;
    }

    private void FixedUpdate() {
        _velocity = (transform.position - _currPosition) / Time.fixedTime;
        transform.localScale = LengthContraction(_velocity);

        _currPosition = transform.position;
    }

    /// <summary>
    /// 尺缩效应
    /// </summary>
    /// <param name="v">速度</param>
    /// <returns>缩放</returns>
    private static Vector3 LengthContraction(Vector3 v) {
        float sx = LengthContraction(v.x);
        float sy = LengthContraction(v.y);
        float sz = LengthContraction(v.z);
        return new Vector3(sx, sy, sz);
    }

    private static float LengthContraction(float v) {
        float d = 1f - v * v / EmccConst.C2;
        return d > 0f ? Mathf.Sqrt(d) : 0f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (_velocity == Vector3.zero) {
            return;
        }

        UnityEditor.Handles.color = Color.cyan;
        Gizmos.color = Color.cyan;
        float l = 1f - 1f / (_velocity.magnitude + 1f);
        Vector3 endPos = _currPosition + _velocity.normalized * l * 5f;
        Gizmos.DrawSphere(_currPosition, 0.1f);
        UnityEditor.Handles.DrawLine(_currPosition, endPos);
    }
#endif
}
