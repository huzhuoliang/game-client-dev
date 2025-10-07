using System.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Net.Mono {
    public class ServerStatusMono : MonoBehaviour {
        private enum ServerStatus {
            Online,
            Connecting,
            Offline,
        }

        [SerializeField]
        [Required]
        private GameClient gameClient;

        [SerializeField]
        private GameObject statusOnline;

        [SerializeField]
        private GameObject statusOffline;

        [SerializeField]
        private GameObject statusConnecting;

        [SerializeField]
        private TMP_Text statusText;

        [SerializeField]
        private Button refreshButton;

        private void Start() {
            Init();
            refreshButton.onClick.AddListener(OnClickRefresh);
        }

        private void OnEnable() {
            StartCoroutine(CoUpdateServerStatus());
        }

        private void Init() {
            statusText.text = "";
            SwitchStatus(ServerStatus.Offline);
        }

        private IEnumerator CoUpdateServerStatus() {
            if (gameClient == null) {
                yield break;
            }

            while (gameClient != null) {
                Refresh();
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void Refresh() {
            statusText.text = string.Format("{0}:{1}", gameClient.Addr, gameClient.Port);
            SwitchStatus(GetServerStatus(gameClient));
        }

        private void OnClickRefresh() {
            Refresh();
        }

        private static ServerStatus GetServerStatus(GameClient client) {
            if (client == null) {
                return ServerStatus.Offline;
            }

            if (!client.IsClientConnected) {
                return ServerStatus.Connecting;
            }

            return ServerStatus.Online;
        }

        private void SwitchStatus(ServerStatus status) {
            statusOnline.SetActive(status == ServerStatus.Online);
            statusOffline.SetActive(status == ServerStatus.Offline);
            statusConnecting.SetActive(status == ServerStatus.Connecting);
        }
    }
}
