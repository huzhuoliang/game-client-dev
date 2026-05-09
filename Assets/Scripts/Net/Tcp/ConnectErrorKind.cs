namespace Net.Proto {
    /// <summary>
    /// 连接失败的分类。便于 UI / 上层做差异化提示与重试决策。
    /// </summary>
    public enum ConnectErrorKind {
        /// <summary>
        /// 未分类异常
        /// </summary>
        Unknown,

        /// <summary>
        /// TCP 层失败：连接被拒绝、网络不可达、超时等。重试通常有意义。
        /// </summary>
        SocketError,

        /// <summary>
        /// TLS 握手失败：证书不被信任、主机名不匹配、协议错误等。重试通常无效，需要用户检查配置。
        /// </summary>
        TlsAuthFailed,
    }
}
