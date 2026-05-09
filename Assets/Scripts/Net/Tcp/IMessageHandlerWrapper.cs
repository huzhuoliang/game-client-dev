using GameServerServices.MessageType;
using Google.Protobuf;

namespace Net.Proto {
    public interface IMessageHandlerWrapper {
        MessageType MessageType { get; }
        IMessage CreateMessage();
        void Handle(IMessage message);
    }
}
