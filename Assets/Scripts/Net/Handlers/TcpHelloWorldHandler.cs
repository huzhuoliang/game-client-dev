using GameServerServices.HelloWorld;
using GameServerServices.MessageType;
using Net.Proto;
using UnityEngine;

namespace Net.Tcp {
    // ReSharper disable once UnusedType.Global
    public class TcpHelloWorldHandler : IMessageHandler<HelloReply> {
        public MessageType MessageType => MessageType.MsgHelloworldReply;

        public void Handle(HelloReply message) {
            Debug.LogErrorFormat("HelloReply message: {0}", message.Message);
        }
    }
}
