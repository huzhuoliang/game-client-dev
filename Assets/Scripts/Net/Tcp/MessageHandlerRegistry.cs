using System.Collections.Generic;
using GameServerServices.MessageType;
using Google.Protobuf;
using UnityEngine;

namespace Net.Tcp {
    public class MessageHandlerRegistry {
        public static MessageHandlerRegistry Instance { get; } = new();

        private readonly Dictionary<MessageType, IMessageHandlerWrapper> _handlersDic = new();

        public void Register<T>(IMessageHandler<T> handler) where T : IMessage, new() {
            if (_handlersDic.ContainsKey(handler.MessageType)) {
                Debug.LogErrorFormat("Duplicate MessageType registration: {0}", handler.MessageType);
                return;
            }

            _handlersDic[handler.MessageType] = new MessageHandlerWrapper<T>(handler);
        }

        public void UnRegisterAll() {
            _handlersDic.Clear();
        }

        public bool TryGetHandler(MessageType messageType, out IMessageHandlerWrapper wrapper) {
            return _handlersDic.TryGetValue(messageType, out wrapper);
        }
    }
}
