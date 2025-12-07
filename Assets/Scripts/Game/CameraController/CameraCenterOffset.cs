using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.CameraController {
    /// <summary>
    /// 斜视锥体
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [HideMonoScript]
    [InfoBox("必须进入运行模式才能生效")]
    public class CameraCenterOffset : MonoBehaviour {
        public enum ProjectionMatrixUpdateMode {
            /// <summary>
            /// 每帧更新（性能较差，但适用于Animation）
            /// </summary>
            [LabelText("每帧更新")]
            OnFrameUpdate,

            /// <summary>
            /// 数值变化时更新（性能较好，但不适用于Animation）
            /// </summary>
            [LabelText("偏移值变化时更新")]
            OnValueChange,
        }

        [LabelText("刷新模式")]
        [EnumToggleButtons]
        public ProjectionMatrixUpdateMode updateMode;

        [SerializeField]
        [DisableInEditorMode]
        [OnValueChanged(nameof(Test_SetObliqueness))]
        [LabelText("偏移值")]
        private Vector2 obliquenessValue;

        /// <summary>
        /// 水平和垂直偏移值。（范围 [-1, 1] 为屏幕内）
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public Vector2 Obliqueness {
            get => obliquenessValue;
            set {
                obliquenessValue = value;
                if (updateMode == ProjectionMatrixUpdateMode.OnValueChange) {
                    SetObliqueness(obliquenessValue);
                }
            }
        }

        /// <summary>
        /// 通过视口坐标修改偏移值。
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public Vector2 ViewPortPosition {
            get => (Vector2.one - Obliqueness) / 2f;
            set => Obliqueness = Vector2.one - value * 2f;
        }

        /// <summary>
        /// 通过屏幕坐标修改偏移值。
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public Vector2 ScreenPosition {
            get {
                Vector2 viewPortPos = ViewPortPosition;
                float screenPosX = viewPortPos.x * _camera.pixelWidth;
                float screenPosY = viewPortPos.y * _camera.pixelHeight;
                return new Vector2(screenPosX, screenPosY);
            }
            set {
                float viewPortX = value.x / _camera.pixelWidth;
                float viewPortY = value.y / _camera.pixelHeight;
                ViewPortPosition = new Vector2(viewPortX, viewPortY);
            }
        }

        private Camera _camera;

        /// <summary>
        /// 重置相机投影矩阵
        /// </summary>
        public void ResetProjectionMatrix() {
            _camera.ResetProjectionMatrix();
        }

        private void Awake() {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate() {
            if (updateMode == ProjectionMatrixUpdateMode.OnFrameUpdate) {
                SetObliqueness(obliquenessValue);
            }
        }

        private void OnDisable() {
            ResetProjectionMatrix();
        }

        private void Test_SetObliqueness() {
            SetObliqueness(obliquenessValue);
        }

        private void SetObliqueness(Vector2 obliqueness) {
            SetObliqueness(obliqueness[0], obliqueness[1]);
        }

        /// <summary>
        /// 通过更改摄像机的投影矩阵来快速实现斜视锥体，仅当游戏运行播放模式时才能看到脚本的效果。
        /// （参考 https://docs.unity3d.com/cn/2019.4/Manual/ObliqueFrustum.html）
        /// </summary>
        /// <param name="horizontalObliqueness"></param>
        /// <param name="verticalObliqueness"></param>
        private void SetObliqueness(float horizontalObliqueness, float verticalObliqueness) {
            if (!Application.isPlaying) {
                return;
            }

            Matrix4x4 mat = _camera.projectionMatrix;
            mat[0, 2] = horizontalObliqueness;
            mat[1, 2] = verticalObliqueness;
            _camera.projectionMatrix = mat;
        }
    }
}
