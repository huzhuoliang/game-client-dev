using System;
using Cysharp.Threading.Tasks;
using Game.UI;
using Net.Mono;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Login {
    [Serializable]
    public sealed class ShowAppLoadingUIState : LoginStateBase {
        [ShowInInspector]
        private const float CONNECT_TIMEOUT_INTERVAL = 5f;

        protected override async UniTask StateStart() {
            LoginStateMachineContext context = StateMachine.Context;
            Debug.LogError("============ ShowAppLoadingUIState: 显示加载UI ...");
            AppLoadingWindow window = context.UIManager.ShowWindow<AppLoadingWindow>();
            while (!context.GameClient.IsClientConnected) {
                await TryConnect(window);
                if (context.GameClient.IsClientConnected) {
                    SetNext<ShowLoginUIState>();
                    break;
                }

                CommonMessageWindow messageWindow = context.UIManager.ShowWindow<CommonMessageWindow>(window);
                bool cancel = false;
                messageWindow
                        .Init()
                        .SetTitle("Connect Message")
                        .SetMessage("Connect failed.")
                        .SetShowCancelButton(true)
                        .SetShowConfirmButton(true)
                        .SetOnCancel(() => cancel = true);
                await UniTask.WaitUntil(() => messageWindow.IsShow == false);
                if (cancel) {
                    SetNext<AppQuitState>();
                    break;
                }
            }

            context.UIManager.HideWindow(window);
        }

        private async UniTask TryConnect(AppLoadingWindow window) {
            LoginStateMachineContext context = StateMachine.Context;
            GameClient gameClient = context.GameClient;
            gameClient.Connect(); // 开始连接服务器
            window.SetInfoText("Connecting to server...");
            float startTime = Time.time;
            float pastTime = 0f;
            while (!gameClient.IsClientConnected && pastTime <= CONNECT_TIMEOUT_INTERVAL) {
                window.SetInfoText(string.Format("Connecting to server... {0:0.00}s", pastTime));
                float progress = Mathf.Clamp(pastTime / CONNECT_TIMEOUT_INTERVAL, 0f, 1f);
                window.SetProgress(progress);
                await UniTask.Delay(16); // 大约每秒更新 60 次
                pastTime = Time.time - startTime;
            }

            window.SetProgress(1f);

            if (!gameClient.IsClientConnected) {
                gameClient.Disconnect();
            }
        }
    }
}
