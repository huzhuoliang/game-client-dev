using GameServerServices.MessageType;
using GameServerServices.User.Register;
using Net.Tcp;
using UnityEngine;

namespace Net.Handlers.User {
    // ReSharper disable once UnusedType.Global
    public class UserRegister : IMessageHandler<UserRegisterRes> {
        public MessageType MessageType => MessageType.MsgUserRegisterRes;

        public void Handle(UserRegisterRes message) {
            Debug.LogErrorFormat("UserRegister message: success={0} id={1} errorCode={2} message={3}",
                    message.Success, message.Id, message.Errorcode, message.Message);
        }
    }
}
