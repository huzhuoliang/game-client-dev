using GameServerServices.MessageType;
using Google.Protobuf;

namespace Net.Tcp {
    public interface IMessageHandlerWrapper {
        MessageType MessageType { get; }
        IMessage CreateMessage();
        void Handle(IMessage message);
    }
}
