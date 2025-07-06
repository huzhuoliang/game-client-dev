using GameServerServices.MessageType;
using Google.Protobuf;

namespace Net.Proto {
    public class MessageHandlerWrapper<T> : IMessageHandlerWrapper where T : IMessage, new() {
        private readonly IMessageHandler<T> _handler;
        public MessageType MessageType => _handler.MessageType;

        public IMessage CreateMessage() => new T();

        public MessageHandlerWrapper(IMessageHandler<T> handler) {
            _handler = handler;
        }

        public void Handle(IMessage message) {
            _handler.Handle((T)message);
        }
    }
}
