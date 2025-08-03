using System;
using System.Collections;
using Game.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class ShowAppLoadingUIState : LoginStateBase {
        [ShowInInspector]
        private const float CONNECT_TIMEOUT_INTERVAL = 5f;

        protected override IEnumerator StateStart() {
            Debug.LogError("============ ShowAppLoadingUIState: 显示加载UI ...");
            AppLoadingWindow window = StateMachine.UIManager.ShowWindow<AppLoadingWindow>();
            while (!StateMachine.GameClient.IsClientConnected) {
                yield return TryConnect(window);
                if (StateMachine.GameClient.IsClientConnected) {
                    SetNext<ShowLoginUIState>();
                    break;
                }

                CommonMessageWindow messageWindow = StateMachine.UIManager.ShowWindow<CommonMessageWindow>(window);
                bool cancel = false;
                messageWindow
                        .Init()
                        .SetTitle("Connect Message")
                        .SetMessage("Connect failed.")
                        .SetShowCancelButton(true)
                        .SetShowConfirmButton(true)
                        .SetOnCancel(() => cancel = true);
                yield return new WaitUntil(() => messageWindow.IsShow == false);
                if (cancel) {
                    SetNext<AppQuitState>();
                    break;
                }
            }

            StateMachine.UIManager.HideWindow(window);
        }

        private IEnumerator TryConnect(AppLoadingWindow window) {
            StateMachine.GameClient.Connect(); // 开始连接服务器
            window.SetInfoText("Connecting to server...");
            float startTime = Time.time;
            float pastTime = 0f;
            while (!StateMachine.GameClient.IsClientConnected && pastTime <= CONNECT_TIMEOUT_INTERVAL) {
                window.SetInfoText(string.Format("Connecting to server... {0:0.00}s", pastTime));
                float progress = Mathf.Clamp(pastTime / CONNECT_TIMEOUT_INTERVAL, 0f, 1f);
                window.SetProgress(progress);
                yield return new WaitForSeconds(0.016f); // 大约每秒更新 60 次
                pastTime = Time.time - startTime;
            }

            window.SetProgress(1f);

            if (!StateMachine.GameClient.IsClientConnected) {
                StateMachine.GameClient.Disconnect();
            }
        }
    }
}
