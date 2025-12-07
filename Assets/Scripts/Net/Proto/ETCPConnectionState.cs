namespace Net.Proto {
    public enum ETcpConnectionState {
        /// <summary>
        /// 初始状态
        /// </summary>
        None,

        /// <summary>
        /// 初始化中
        /// </summary>
        Init,

        /// <summary>
        /// 建立 TCP 连接中
        /// </summary>
        Connecting,

        /// <summary>
        /// TCP 连接已建立
        /// </summary>
        Connected,

        /// <summary>
        /// 握手中
        /// </summary>
        Handshaking,

        /// <summary>
        /// 连接可用
        /// </summary>
        Ready,

        /// <summary>
        /// 主动断开连接中
        /// </summary>
        Disconnecting,

        /// <summary>
        /// 主动/被动断开连接完成
        /// </summary>
        Disconnected,

        /// <summary>
        /// 自动重连中
        /// </summary>
        Reconnecting,

        /// <summary>
        /// 已关闭
        /// </summary>
        Close,
    }
}
