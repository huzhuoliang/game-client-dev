namespace Net.Tcp {
    /// <summary>
    /// TCP 客户端 FSM 的对外状态枚举。
    /// 内部状态实现（<see cref="State.TcpClientStateBase"/> 子类）通过 <c>Kind</c> 属性映射到此枚举；
    /// 外部代码只通过本枚举观察状态，不直接接触 state 类。
    /// </summary>
    public enum ETcpState {
        /// <summary>FSM 未启动 / 已退出</summary>
        None,
        Init,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected,
    }
}
