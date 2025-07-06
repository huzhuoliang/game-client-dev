using GameServerServices.MessageType;
using Google.Protobuf;

namespace Net.Proto {
    public interface IMessageHandler<in T> where T : IMessage, new() {
        MessageType MessageType { get;  }
        void Handle(T message);
    }
}
