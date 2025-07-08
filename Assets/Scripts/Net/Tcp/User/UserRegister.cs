using GameServerServices.MessageType;
using GameServerServices.User.Register;
using Net.Proto;
using UnityEngine;

namespace Net.Tcp.User {
    // ReSharper disable once UnusedType.Global
    public class UserRegister : IMessageHandler<UserRegisterRes> {
        public MessageType MessageType => MessageType.MsgUserRegisterRes;

        public void Handle(UserRegisterRes message) {
            Debug.LogErrorFormat("UserRegister message: success={0} message={1}", message.Success, message.Message);
        }
    }
}
