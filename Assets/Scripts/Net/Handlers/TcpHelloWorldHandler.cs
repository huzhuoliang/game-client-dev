using GameServerServices.HelloWorld;
using GameServerServices.MessageType;
using Net.Tcp;
using UnityEngine;

namespace Net.Handlers {
    // ReSharper disable once UnusedType.Global
    public class TcpHelloWorldHandler : IMessageHandler<HelloReply> {
        public MessageType MessageType => MessageType.MsgHelloworldReply;

        public void Handle(HelloReply message) {
            Debug.LogErrorFormat("HelloReply message: {0}", message.Message);
        }
    }
}
