# TCP 客户端状态机重构 TODO

把 `Assets/Scripts/Net/Proto/GameTcpClient.cs` 拆成状态机 (`TcpClientStateMachine` + `TcpClientStateBase` 各状态) 的进度跟踪。

## 当前进度快照

| 文件 | 状态 |
|---|---|
| `TcpClientStateBase` / `TcpClientStateMachine` / `ITcpClientFSMCtx` / `TcpClientFSMCtx` / `TcpClientStateMachineMono` | 骨架已搭好 |
| `Init` | 真实实现（注册 handler → `SetNext<Connecting>`） |
| `Connecting` | 部分实现（重试循环写了，但**没有 `SetNext`**） |
| `Handshaking` / `Connected` / `Ready` / `Reconnecting` / `Disconnecting` / `Disconnected` / `Closed` | 全是 `await UniTask.Yield()` 占位 |
| `GameTcpClient` 老代码 | 仍在跑，没人删，握手/收发/dispatch 还都在这边 |

---

## 结构问题清单（按严重程度排序）

### 1. ✅ 已修复 — `TcpClientStateMachine.StartAsync` 异常处理有 bug

**原问题**：

```csharp
while (_state != null) {
    try {
        _state.OnEnter(_context);
        await _state.RunAsync(_context, token);
        _state.OnExit(_context);
        _state = _state.NextState;   // ← 在 try 里
    } catch (Exception e) {
        Debug.LogErrorFormat(...);
    }
}
```

任何状态抛异常（包括 `OnDisable` 里 `_cts.Cancel()` 触发的 `OperationCanceledException`），`_state = _state.NextState` 这一行被跳过，外层 `while` 用同一个 `_state` 死循环。

**修复后代码**（`Assets/Scripts/Net/Proto/TcpClientStateMachine.cs`）：

```csharp
while (_state != null) {
    TcpClientStateBase next;
    try {
        _state.OnEnter(_context);
        try {
            await _state.RunAsync(_context, token);
        } finally {
            _state.OnExit(_context);
        }
        next = _state.NextState;
    } catch (OperationCanceledException) {
        return;
    } catch (Exception e) {
        Debug.LogErrorFormat("TcpClientState \"{0}\" run error.\n{1}", _state.GetType().Name, e);
        return;
    }
    _state = next;
}
```

**修复思路（4 点）**：

1. **`next` 推进挪出 try 块**——异常时不再尝试用旧 `_state` 继续循环，避免死循环。改成只有 `try` 块完整跑完才赋值给 `_state`。
2. **`OperationCanceledException` 单独捕获，直接 `return`**——取消是正常退出路径，不该当作"错误"打日志，也不该试图转到下一个状态。`Mono.OnDisable` 里 `_cts.Cancel()` 会进这条分支。
3. **其他异常 `return` 而不是继续循环**——目前没有 `FaultedState` 可以转过去，再循环就是同样的状态再炸一次。先以"日志 + 退出 FSM"的方式止血；以后真有了 fault 恢复策略，再回来改这里。日志带上状态类名方便定位。
4. **`OnExit` 包到内层 `try/finally` 里**——保证状态正常结束或 `RunAsync` 抛异常时 `OnExit` 都会执行（清理职责）。`OnEnter` 故意留在外层 try 里：进入失败就不该出，符合"配对调用"原则。

**注意**：这是过渡修复。后续做 #2（API 重塑成"`RunAsync` 直接返回下一个状态"）时，`NextState` 字段和这段 `next` 推进代码会一起被改写。但本步骤独立可上、可测，先解掉死循环的阻塞性问题。

### 2. `TcpClientStateBase` 的静态 `instanceDic` 是个隐患

状态实例放在 **静态字典** 里，多个 `TcpClientStateMachine` 实例会共享同一组状态对象，而 `NextState` 是实例字段——两个 client 一起跑会互相把对方的 `NextState` 覆盖掉。同时这些状态对象永远不会被释放。

**两条出路（推荐第一种）**：

- **状态无状态化**：把 `NextState` 字段去掉，让 `RunAsyncInternal` **返回** 下一个状态类型（或 enum）。状态对象真的可以做 singleton。
- 把 `instanceDic` 从 static 改成 FSM 实例字段。

第一种和 `Game.StateMachine` 那套（`State.GetNext()`）行为一致，但更纯粹。

### 3. `Connecting` 没有 `SetNext` — 下一步会立刻撞到的问题

```csharp
TcpClient client = await ctx.Connect(ct);
if (client != null) {
    Debug.LogFormat("Connect to {0} success", ctx.TargetEndPoint);
    break;       // ← break 出 for，但 NextState 一直是 null
}
```

循环结束后无论成功失败 `NextState == null`，`StartAsync` 直接退出。

**修复**：
- 成功时 `SetNext<Handshaking>()`（或直接 `Connected`，看你怎么定 SSL 阶段归属）
- 重试耗尽时 `SetNext<Disconnected>()` 或 `SetNext<Closed>()`

### 4. 现在还是纯 "线性 next 指针" 模型，但 TCP 生命周期是事件驱动的

老 `GameTcpClient` 的 `ListenLoopAsync` 一抛 `IOException("Disconnected")` 就会把对象切到 `Disconnecting`。新 FSM 还没有任何 "外部事件触发状态切换" 的通道。在写 `Ready` 之前必须决定：

- **A.** Ready 状态自己持有 listen/parse 循环，循环里 socket 断开就 `SetNext<Reconnecting>` 然后 return。`RunAsync` 直到断线才结束。
- **B.** Ctx 暴露一个 `RequestTransition<T>()`，外部 loop 触发它，状态用 linked CTS 监听。

**建议**：先选 A，等以后真的需要外部强切（比如用户主动断开）再加 B。无论选哪个，老代码里 `_isClosing`、`OnClose` 那一坨布尔状态都不应该再带过来——FSM 本身就是状态。

### 5. `ITcpClientFSMCtx.Connect` 接口设计有点拧

`TcpClientFSMCtx.Connect` 内部把 `_client` 和 `_stream` 写进了 ctx，但还把 `TcpClient` 当返回值，调用方只用它做 null 检查。

**修复**：二选一：
- 改成 `UniTask<bool> ConnectAsync(ct)`，client/stream 通过 ctx 取
- ctx 不持有，由 state 持有（不推荐，state 想做无状态的）

另外 `TcpClient` 不该泄漏给 state——state 要的是一个能读写的 `Stream`，把 `SslStream` 暴露出来就够了。

### 6. `TcpClientFSMCtx` 构造函数里同步阻塞 DNS

```csharp
ip = Dns.GetHostAddressesAsync(host).AsUniTask().GetAwaiter().GetResult()[0];
```

构造时阻塞主线程做 DNS，主机不存在还会直接抛在构造里。

**修复**：把 DNS 解析挪进 `Connect()`，构造函数只存 host:port 字符串。

### 7. 老的 `RegisterAllByReflection` 在两处重复

`Init.cs` 和 `GameTcpClient.cs` 各有一份。迁移完之后删 `GameTcpClient` 那份。

### 8. `OnEnter` / `OnExit` 设计了但没人用

`Debug.LogErrorFormat("============ RunAsync ...")` 这条日志按语义应该挪到 `OnEnter`——"进入状态" 比 "开始 run" 更准。RunAsync 那条可以删掉。

### 9. `new Init()` 绕过了 instanceDic 缓存

`TcpClientStateMachineMono.OnEnable` 里 `new Init()` 创建了一个不在缓存里的实例，但后续 `SetNext<X>` 又会把别的状态丢进缓存。如果按 #2 走 "状态无状态化"，这个问题自然消失；不然得统一入口。

### 10. ✅ 已修复 — 证书/握手异常未被捕获，会让 FSM 直接挂掉

**触发场景**：服务器自签名证书未在 Win11 客户端可信根中安装，运行后 `SslStream.AuthenticateAsClientAsync` 抛出 `System.Security.Authentication.AuthenticationException`，一路穿透 `TcpClientFSMCtx.ConnectOnce` → `Connecting.RunAsyncInternal`，最终被 `TcpClientStateMachine.StartAsync` 的兜底 catch 打出长堆栈，FSM 直接结束。这种"配置/环境类"错误必须能优雅地汇报给 UI 让用户处理。

**修复方案**：在 ctx 层做"分类捕获 + 事件外抛"。

新增 `Net.Proto.ConnectErrorKind`（`Assets/Scripts/Net/Proto/ConnectErrorKind.cs`）：

```csharp
public enum ConnectErrorKind {
    Unknown,
    SocketError,    // TCP 层失败，重试通常有意义
    TlsAuthFailed,  // 证书/CN/协议问题，重试无效
}
```

`ITcpClientFSMCtx` 加事件：

```csharp
event Action<ConnectErrorKind, Exception> OnConnectFailed;
```

`TcpClientFSMCtx.ConnectOnce` 拆成 **TCP / TLS 两段** 各自带分类 catch：

- `OperationCanceledException` → 透传（取消不是失败，由 FSM 顶层处理）
- `SocketException` → 触发事件 `SocketError`，返回 null
- `AuthenticationException` → 触发事件 `TlsAuthFailed`，返回 null
- 其他 → 触发事件 `Unknown`，返回 null

`Connecting` 状态订阅事件 + 重试循环里检查最后错误类型，遇到 `TlsAuthFailed` 直接 break，避免 10 次重试刷屏。`finally` 里反订阅。

**修复思路（4 点）**：

1. **错误分类放 ctx 不放 state**——ctx 是网络 IO 边界，最了解异常类型；state 不该自己去 try/catch `AuthenticationException`，那会让每个状态都重复异常知识。
2. **事件而不是返回值**——`Connect` 仍返回 `TcpClient?`（成功 / 失败 / 取消三态），错误细节通过事件外推。这样老的 "if client != null" 调用不需要改写，新功能（UI 提示）通过订阅加进来。
3. **OCE 单独处理且透传**——取消不是错误，不应触发 `OnConnectFailed`；让它继续以异常形式上抛，由 #1 已修过的 `StartAsync` 顶层 catch 干净退出。
4. **TLS 失败不再盲目重试**——`Connecting` 自己订阅事件，知道是 `TlsAuthFailed` 就 break。这条逻辑不放 ctx 是因为"是否重试"是状态职责，不是 ctx 职责。

**已知遗留**：

- `TcpClientStateMachineMono` 暂未把 ctx / 事件暴露给外部，UI 还订阅不到。等 #5（公共 API 面）一起处理；现在用 `Debug.Log` 先看到。
- `TargetHost` 仍硬编码 `"localhost"`——证书装好后下一步可能撞 `CN_MISMATCH`。看证书 SAN 决定是否要把 host 名传进 ctx。

---

## 接下来的工作优先级

按这个顺序做：

- [ ] **1. 重塑 `TcpClientStateBase` API（解决 #1 + #2 + #5）**——这三个会决定基类 API 形状，越往后改越伤。建议改成：

    ```csharp
    protected abstract UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct);
    ```

    返回 `null` = 终止 FSM，返回自己 = 自循环（不太需要）。彻底干掉 `NextState` 字段、`SetNext`、静态字典。

- [ ] **2. 修 `TcpClientStateMachine` 的异常/取消语义**——OCE 干净退出，其他异常按策略要么转到一个 `FaultedState` 要么直接 break。

- [ ] **3. 把 `Connecting` 写完整**：成功 → `Handshaking`（先把 SSL 拆出去），失败 → `Closed`。同时把 `Handshaking` 写出来（搬 `AuthenticateAsClientAsync` 那段）。

- [ ] **4. 写 `Ready` 状态——真正的硬骨头**：
    - 把 `RingBufferStream`、`ListenLoopAsync`、`ParseLoopAsync` 从 `GameTcpClient` 搬过来
    - 在 `Ready.RunAsync` 里 `WhenAny(listen, parse, ct)`
    - 任何一个返回 / 抛 `IOException` → 状态切到 `Disconnecting` 或 `Reconnecting`（看是不是要自动重连）

- [ ] **5. 设计公共 API 面**：`SendMessage` 现在没地方放。建议给 ctx 加一个 `IMessageSender`（持有 stream + 写入锁），任何状态阶段都能调；外部代码（`GameClient.cs`）也通过 ctx 调用，而不是直接调 state。

- [ ] **6. 想清楚 `Connecting` vs `Reconnecting` 的分工**：是否让 `Connecting` 只跑一次（首次连接），重试逻辑挪到 `Reconnecting`？现在重试循环写在 `Connecting` 里，但又有个独立 `Reconnecting` 状态，语义重复。

- [ ] **7. 删除 `GameTcpClient` 旧实现**，把 `GameClient.cs` 改成用 `TcpClientFSMCtx` + `TcpClientStateMachineMono`。这一步要等 #4、#5 全跑通再做。

---

## 备注

- #1、#2、#5 是 API 形状决定，必须先做。
- #4 是工作量最大的一块，但要等 API 稳定才能下手。
- #6 是设计决策，可以在写 `Reconnecting` 之前再敲定。
