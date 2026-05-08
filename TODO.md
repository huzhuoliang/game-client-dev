# TCP 客户端状态机重构 TODO

把 `Assets/Scripts/Net/Proto/GameTcpClient.cs` 拆成状态机 (`TcpClientStateMachine` + `TcpClientStateBase` 各状态) 的进度跟踪。

---

## 当前进度快照

| 文件 | 状态 |
|---|---|
| `TcpClientStateBase` / `TcpClientStateMachine` / `ITcpClientFSMCtx` / `TcpClientFSMCtx` / `TcpClientStateMachineMono` | 骨架完成 |
| `Init` | 真实实现（注册 handler → `Connecting`） |
| `Connecting` | 真实实现（成功 → `Connected`，失败/TLS/重试耗尽 → `Disconnected`，OCE → `null`） |
| `Connected` | 薄壳（~15 行）：调 `ctx.RunMessagePump(ct)`，OCE 透传，其他异常 → `Disconnecting` |
| `Handshaking` / `Disconnecting` / `Disconnected` / `Closed` | 占位（`await UniTask.Yield()`） |
| `Reconnecting` / `Ready` | 已删除（YAGNI） |
| `GameTcpClient` 老代码 | 仍在跑（`GameClient.cs` 用），新 FSM 已具备同等能力，等 T1 删除 |

---

## 待办清单（按优先级）

> 命名约定：开放工作用 `T*` 编号；旧的"结构问题清单"#1-#10 是历史记录（详见下面"历史决议"），不再用 # 编号指代待办，避免歧义。

### 🔴 主线 — T1. 删 GameTcpClient 老实现 + 迁移 GameClient.cs

工作量最大的一块，要碰 UI 端的引用。

**要做的**：

- 把 `Assets/Scripts/Net/Mono/GameClient.cs` 改成用 `TcpClientStateMachineMono` + `ITcpClientFSMCtx`（不再持有 `GameTcpClient`）
- 删 `Assets/Scripts/Net/Proto/GameTcpClient.cs`
- 删 `Assets/Scripts/Net/Proto/ETCPConnectionState.cs`（含旧的 `Reconnecting` 枚举值）
- 同时清理：
  - `GameTcpClient.RegisterAllByReflection`（与 `Init.cs` 里的重复，删 `GameTcpClient` 那份）—— 历史 #7
  - `ServerStatusMono.cs` 里 `gameClient.IsClientConnected` / `Addr` / `Port` 的引用——改成订阅 ctx 状态变化或读 `Context.RemoteAddress`
- 验证：进 `GameClient.unity` 场景，确认连接、发送、断开都和迁移前一致

**依赖**：T2-T4 不阻塞（可以并行 / 插队 / 先做）。

### 🟡 独立小清理（可插队）

#### T2. ✅ DNS 异步化（历史 #6，2026-05-08 落地）

**原问题**：`TcpClientFSMCtx(host, port)` 构造时同步阻塞主线程做 DNS：

```csharp
ip = Dns.GetHostAddressesAsync(host).AsUniTask().GetAwaiter().GetResult()[0];
```

主机不存在直接抛在 `new TcpClientFSMCtx(...)` 里，调用方很难处理。

**修复方案**：

- 构造函数只存 `_host` / `_port`（字面 IP 也走同一路径，不在构造里 parse）
- 给 ctx 加私有方法 `ResolveEndPoint(ct)`：先 `IPAddress.TryParse` 试字面量，失败再走 `Dns.GetHostAddressesAsync` + `AttachExternalCancellation(ct)`
- `Connect(ct)` 第一步：若 `_targetEndPoint == null` 就调 `ResolveEndPoint(ct)`；解析失败返 false（同时触发 `OnConnectFailed` 事件，DNS 失败归到 `SocketError` kind）；解析成功缓存到 `_targetEndPoint`，后续 `ConnectOnce` 用
- 接口删 `IPEndPoint TargetEndPoint`，加 `string RemoteAddress`——所有外部消费者只用它做日志/UI 显示，没人需要 `IPEndPoint` 对象
- `RemoteAddress` 实现：`_targetEndPoint?.ToString() ?? $"{_host}:{_port}"`，无论是否解析过都可用
- `Connecting.cs` 4 处 `ctx.TargetEndPoint` → `ctx.RemoteAddress`

**修复思路（4 点）**：

1. **DNS 失败归到 `SocketError` 而非新 kind**——`Dns.GetHostAddressesAsync` 抛 `SocketException`，自然归到 `ConnectErrorKind.SocketError`。新加 `DnsResolutionFailed` 是 over-engineering，UI 想区分原因可以看 `Exception` 详情。
2. **缓存解析结果**——`_targetEndPoint` 第一次解析后缓存，后续 `Connect()`（如重试）不再重复解析。将来加自动重连若想强制刷新 DNS，提供独立 API（YAGNI，先不做）。
3. **`AttachExternalCancellation` 而不是 ct 重载**——Unity 的 .NET 没有 `Dns.GetHostAddressesAsync(host, ct)` 重载；UniTask 提供的 `AttachExternalCancellation` 让我们能在 ct 触发时立刻 throw OCE，底下 DNS 查询任由它在后台跑完（孤儿 task，无资源泄漏）。
4. **接口收紧**——之前 `TargetEndPoint` 有外部消费者全是日志用途；换成显式只为日志的 `RemoteAddress`，类型从 `IPEndPoint?` 变 `string`，调用方不必处理 null。

#### T3. `OnEnter` / `OnExit` 利用 + 日志位置（历史 #8）

`TcpClientStateBase.RunAsync` 里的 `Debug.LogErrorFormat("============ RunAsync ...")` 按语义应在 `OnEnter`——"进入状态" 比 "开始 run" 更准。

**改动**：日志挪到 `OnEnter`；`RunAsync` 那条删掉；保持 `OnEnter` / `OnExit` 的"配对调用"语义。

工作量：~5 行内。

#### T4. `TargetHost` 硬编码 `"localhost"`（历史 #10 遗留）

`TcpClientFSMCtx.ConnectOnce` 里：

```csharp
SslClientAuthenticationOptions options = new() { TargetHost = "localhost" };
```

证书装好后下一步可能撞 `CN_MISMATCH`。看证书 SAN 决定是否要把 host 名传进 ctx。

**改动**：`TargetHost` 从 ctx 配置传入；构造函数加可选参数 `string targetHost`，默认 `"localhost"` 保持向后兼容。

工作量：~10 行内。

---

### ⏸️ 延后（条件触发，目前无需做）

只在对应触发条件出现时再启动。当前一律不做。

| 项 | 触发条件 | 出处 |
|---|---|---|
| 拆 `Handshaking` 独立状态 | 业务层握手需求落地（版本协商 / 鉴权等） | 历史 #3 末尾 |
| `OnDisconnectRequested` 软中断事件 | UI 登出按钮 / token 过期等"外部主动断开"需求落地 | 历史 #4 末尾 |
| 自动重连 | 真要重连时让 `Disconnected → Connecting` | 历史 #6 |
| 业务消息排队（offline send queue） | 真要支持"未连接也能 SendMessage" | 历史 #5 已知遗留 |
| `FaultedState` 故障恢复 | 真要做"状态崩了走错误恢复路径" | 历史 #1 末尾 |
| `Init.RegisterAllByReflection` 在非 Editor build 跳过 | 真要出 player build 时（现在还没需求） | `Init.cs` `#if UNITY_EDITOR` 注释 |

---

## 设计原则备注

跨步骤共享的架构约束，回头改代码时回来对一下。

- **TCP 状态机只覆盖网络层**：游戏层（登录 / 选角 / 对局）单开 FSM，通过 ctx 事件观察 TCP 状态，不混在一起。
- **状态无副作用**：`RunAsyncInternal(ctx, ct)` 只读 ctx + ct，结果通过返回值给出；`null` 终止 FSM。状态对象是 singleton，由 `TcpClientStateBase.GetInstance<T>()` 取。
- **wire 协议全在 ctx**：listen / parse / framing / magic / CRC / handler dispatch 都在 `TcpClientFSMCtx` 里；状态只做生命周期编排。
- **错误分类放 ctx 不放 state**：连接错误以 `ConnectErrorKind` 分类 + `OnConnectFailed` 事件外推；UI 订阅事件而不是 state catch 异常。
- **取消 vs 转移**：`ct` 取消 = "FSM 整体退出"（顶层 OCE catch 干净 return）；状态间转移靠 `RunAsyncInternal` 返回值。两个语义严格分开。
- **状态对象禁止持有任何字段**：所有运行期数据进 `RunAsyncInternal` 的局部或 ctx，singleton 不能有可写状态。

---

## 历史决议（结构问题清单）

按当时识别的严重程度排序，编号保留。**这部分是"修复决策记录"**，作为参考；新的待办用上面的 `T*` 编号。

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
3. **其他异常 `return` 而不是继续循环**——目前没有 `FaultedState` 可以转过去，再循环就是同样的状态再炸一次。先以"日志 + 退出 FSM"的方式止血；以后真有了 fault 恢复策略，再回来改这里（参见上面延后清单）。日志带上状态类名方便定位。
4. **`OnExit` 包到内层 `try/finally` 里**——保证状态正常结束或 `RunAsync` 抛异常时 `OnExit` 都会执行（清理职责）。`OnEnter` 故意留在外层 try 里：进入失败就不该出，符合"配对调用"原则。

**注意**：这是过渡修复。后续做 #2（API 重塑成"`RunAsync` 直接返回下一个状态"）时，`NextState` 字段和这段 `next` 推进代码会一起被改写。

### 2. ✅ 已修复 — `TcpClientStateBase` 的静态 `instanceDic` 与 `NextState` 字段冲突

**原问题**：

状态实例放在 **静态字典** 里，多个 `TcpClientStateMachine` 实例会共享同一组状态对象，而 `NextState` 是实例字段——两个 client 一起跑会互相把对方的 `NextState` 覆盖掉。同时这些状态对象永远不会被释放。

**修复方案**：状态无状态化（之前列的两条出路里的第一条）。

- 删掉 `NextState` 字段和 `SetNext<T>()` 帮助方法
- `RunAsyncInternal` 的签名从 `UniTask` 改成 **`UniTask<TcpClientStateBase>`**，返回值即下一个要执行的状态实例（用 `GetInstance<T>()` 取），返回 **`null`** 表示终止 FSM
- `GetInstance<T>()` 从 `private` 提到 `public static`，方便外部（启动器）取初始状态
- `TcpClientStateMachine.StartAsync` 消费新的返回值（`next = await _state.RunAsync(...)`），并新增泛型重载 `StartAsync<TInit>(token)` 当语法糖
- `TcpClientStateMachineMono` 用 `_stateMachine.StartAsync<Init>(_cts.Token)` 取代之前的 `new Init()`（解决 #9：不再绕过 instance 缓存）

**修复思路（4 点）**：

1. **状态变成"纯函数风格"**——`RunAsyncInternal(ctx, ct)` 只读取 `ctx` 和 `ct`，结果通过返回值给出，没有任何副作用写到状态对象自身上。共享 singleton 不再有竞争。
2. **下一状态在调用栈里流转**——`StartAsync` 用一个局部 `next` 变量保存返回值，紧接着赋给 `_state`。这把"下一状态"的生命周期严格限制在状态机自己的循环里，状态对象彻底不掺合。
3. **`null` = 终止 FSM**——比 enum / 哨兵对象都简单，类型系统直接表达"没有下一个状态"的语义；调用方只需 `while (_state != null)`。
4. **静态缓存继续保留**——状态对象现在真的可以共享，缓存只是为了省 `Activator.CreateInstance` 的反射开销。也连带解决了 #9（`new Init()` 绕过缓存）：入口现在统一走 `GetInstance<Init>()`。

**顺手解决了**：

- ✅ #9（`new Init()` 绕过 instanceDic）—— 入口换成 `GetInstance<Init>()`，整套调用全走单例。

### 3. ✅ 已修复 — `Connecting` 没有转移到任何下一状态

**原问题**：

```csharp
TcpClient client = await ctx.Connect(ct);
if (client != null) {
    Debug.LogFormat("Connect to {0} success", ctx.TargetEndPoint);
    break;       // ← break 出 for，但 NextState 一直是 null
}
```

循环结束后无论成功失败 `NextState == null`，`StartAsync` 直接退出。

**修复方案**（在 #2 状态无副作用化的基础上）：

```csharp
if (client != null) {
    return GetInstance<Connected>();
}
} catch (OperationCanceledException) {
    return null;
}
return GetInstance<Disconnected>();   // 重试耗尽 / TLS 永久失败
```

**修复思路（4 点）**：

1. **成功用早 return，不再借标志位 + 底部 return**——靠 `try/finally` 自动确保事件解订阅，代码更紧凑。
2. **TLS 失败和重试耗尽合并到同一出口**——两者都属于"非异常的失败收尾"，落到底部 `return GetInstance<Disconnected>()`。状态机不需要区分原因，UI 通过 `OnConnectFailed` 事件已经拿到 `ConnectErrorKind`。
3. **OCE 在 catch 里 `return null`**——明确表示"FSM 干净终止"。
4. **不拆 `Handshaking` 状态**——保持 `Connecting` 内部由 `ctx.Connect()` 一次性完成 TCP+SSL；等真的需要细分 SSL 握手 / 业务握手时再做（参见延后清单）。

### 4. ✅ 已修复（2026-05-08 联调通过）— `Connected` 是"稳态 + 事件驱动"的，但 FSM 是"线性 next 指针"模型

> **联调验证**：在 `TcpClientStateMachineMono` 上加临时 `[Button("Send HelloWorld")]`，发送 `HelloRequest` 给服务器，`TcpHelloWorldHandler` 收到 `HelloReply` 并输出日志。**收发 framing + listen/parse + handler dispatch 全链路通**。临时按钮代码归 #5（公共 `SendMessage` API 落地时一起替换）。

> **决策（2026-05-08）**：原计划 `Connected → Ready` 两个状态，`Ready` 留作"业务握手完成"的扩展槽位。当前代码没有任何业务层握手，保留 `Ready` 是对未实现需求的预留，违反 CLAUDE.md "Don't design for hypothetical future requirements"。**砍掉 `Ready`，listen/parse 直接落到 `Connected`**。

**问题本质**：

FSM 是"状态跑完一段有限工作 → return 下一个状态"模型。`Init` / `Connecting` / `Disconnecting` 这种短命状态适用，但 `Connected` 是**稳态**——socket 活着的整段时间都在跑，需要：

1. **并发跑两条长时循环**：`ListenLoopAsync`（读 `_stream` → push 进 `RingBufferStream`）+ `ParseLoopAsync`（从 ring buffer 解帧 → 分发 handler）。
2. **阻塞等待任意一种事件发生**：循环抛异常 / `ct` 取消 / 未来的"主动断开请求"。
3. **根据"是谁先唤醒了我"决定下一状态**。

**实际落地代码**（`Assets/Scripts/Net/Proto/State/Connected.cs`）：

```csharp
protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
    try {
        await ctx.RunMessagePump(ct);
    } catch (OperationCanceledException) {
        throw;                                          // 顶层 ct 取消 → FSM 整体退出
    } catch (Exception e) {
        Debug.LogFormat("Connection broken: {0}", e.Message);
    }
    return GetInstance<Disconnecting>();
}
```

> 注：listen/parse 双循环 + `WhenAny` + drain 的逻辑在 #5 完成后搬到了 `ctx.RunMessagePump`；`Connected` 缩成 ~15 行（详见 #5）。

**修复思路**：

1. **`linkedCts` 串两条循环**——任一条循环死掉，把另一条也叫停后 drain，避免泄漏（实现已挪到 ctx.RunMessagePump）。
2. **OCE 透传，其他异常吞**——`ct.Cancel()` 进 catch OCE → `throw` → `StartAsync` 顶层 OCE 分支干净退出 FSM；`IOException` / 协议错走到 `Disconnecting` 状态做清理。
3. **资源宿主统一在 ctx**——`RingBuffer` 由 ctx 持有，将来加自动重连也能复用。

### 5. ✅ 已修复 — 公共 API 面：`Connect` 签名拧 + 没有 `SendMessage` 入口

**原问题**：

1. `ITcpClientFSMCtx.Connect` 返回 `UniTask<TcpClient>`，但 `_client` / `_stream` 实际写在 ctx 内部；调用方只用返回值做 null 检查。
2. 没有公共 `SendMessage` 入口。`Connected` 想发心跳没接口、外部 UI 也没法调；老代码靠 `GameTcpClient.SendMessage` 直写 `_stream`，没有写入串行化（SslStream 不允许并发写）。
3. `TcpClientStateMachineMono` 完全私有，外部拿不到 ctx，订阅不到 `OnConnectFailed`。

**修复方案**：

① `Connect` 返回类型改 `UniTask<bool>`，client/stream 通过 ctx 属性取。

② 接口加 `SendMessage`，实现走 `SemaphoreSlim` 串行化 + 大端 framing + `WriteAsync`：

```csharp
UniTask SendMessage(MessageType messageType, IMessage message, CancellationToken ct = default);
```

③ `TcpClientStateMachineMono` 暴露 `public ITcpClientFSMCtx Context => _ctx`；调试 `[Button]` 改为一行 `_ctx.SendMessage(...).Forget()`。

④ `TcpClientFSMCtx.Client` 公开属性删掉——彻底切断 `TcpClient` 对 state / 外部的泄漏。

**修复思路（4 点）**：

1. **`bool` 比 `TcpClient?` 表意更准**——调用方关心的是"连上了没"。
2. **写锁放 ctx 不放 state**——SslStream 的并发约束是 IO 边界的事；多个状态都可能调 `SendMessage`，锁集中在一处。`SemaphoreSlim` 比 `lock` 好的点是支持 `await` + ct。
3. **`SendMessage` 异步而不是同步**——配合 ct 取消；返回 `UniTask` 让调用方自己选 await / Forget。
4. **暴露 `ITcpClientFSMCtx` 而不是 `TcpClientFSMCtx`**——外部代码看到接口，私有实现细节藏起来。

**顺手瘦身 Connected**（同次提交）：

把 listen / parse / magic-number 解析这一坨代码从 `Connected.cs` 搬回 `TcpClientFSMCtx`，新增 `RunMessagePump`；接口删 `NetworkStream` / `RingBuffer`（已无消费者）；`Connected` 从 ~105 行 → ~15 行。

接口最终长这样：

```csharp
public interface ITcpClientFSMCtx : IDisposable {
    IPEndPoint TargetEndPoint { get; }
    int MaxAttempts { get; }
    int RetryDelayMs { get; }
    event Action<ConnectErrorKind, Exception> OnConnectFailed;
    UniTask<bool> Connect(CancellationToken ct = default);
    UniTask SendMessage(MessageType messageType, IMessage message, CancellationToken ct = default);
    UniTask RunMessagePump(CancellationToken ct);
}
```

**已知遗留**：

- `TcpClientFSMCtx.SendMessage` 抛 `InvalidOperationException` 是有意为之——调用方未连接就发消息属于编程错误，应该崩出来便于发现。后续如果要支持"离线消息排队"再加 `TryQueueMessage`（参见延后清单）。

### 6. ✅ 已修复 — 删除 `Reconnecting` 状态（与 `Connecting` 职责重复）

**问题**：`Connecting` 已自带重试循环，`Reconnecting` 与之职责重复；当前没有自动重连功能，留个空状态属于 YAGNI。

**修复**：

- 删 `Assets/Scripts/Net/Proto/State/Reconnecting.cs` + `.meta`
- `CLAUDE.md` 状态列表更新（`Reconnecting` 不再"reserved"）
- 旧 `ETcpConnectionState.Reconnecting` 枚举值跟 `GameTcpClient` 一起删（归 T1）

**决策依据**：FSM 应按"行为不同"分状态，不按"何时被调用"。首次连接和重连本质都是"调 `ctx.Connect()` 直到成功或放弃"；UI 想区分"重连中" vs "连接中"应自己用会话历史推断，不该让 FSM 多养一个状态。

**未来真要加自动重连**：让 `Disconnected.RunAsyncInternal` 根据 `ctx.AutoReconnect` 标志直接 `return GetInstance<Connecting>()` 即可。

### 7. （拆分）— 老 `RegisterAllByReflection` 在两处重复

`Init.cs` 和 `GameTcpClient.cs` 各有一份。删 `GameTcpClient` 那份归 **T1** 做。

### 8. （未做）— `OnEnter` / `OnExit` 设计了但没人用

归 **T3**。

### 9. ✅ 已修复（随 #2 一起） — `new Init()` 绕过了 instanceDic 缓存

`TcpClientStateMachineMono.OnEnable` 现在用 `_stateMachine.StartAsync<Init>(_cts.Token)`，内部调用 `TcpClientStateBase.GetInstance<Init>()` 取单例，整套调用都走缓存。

### 10. ✅ 已修复 — 证书/握手异常未被捕获，会让 FSM 直接挂掉

**触发场景**：服务器自签名证书未在 Win11 客户端可信根中安装，`SslStream.AuthenticateAsClientAsync` 抛 `AuthenticationException`，一路穿透到 `StartAsync` 兜底 catch，FSM 直接结束。

**修复方案**：在 ctx 层做"分类捕获 + 事件外抛"。

新增 `ConnectErrorKind` 枚举 + `ITcpClientFSMCtx.OnConnectFailed` 事件；`TcpClientFSMCtx.ConnectOnce` 拆成 TCP / TLS 两段各自带分类 catch：

- `OperationCanceledException` → 透传（取消由 FSM 顶层处理）
- `SocketException` → `SocketError` 事件，返回 false
- `AuthenticationException` → `TlsAuthFailed` 事件，返回 false
- 其他 → `Unknown` 事件，返回 false

`Connecting` 订阅事件 + 检查最后错误类型，遇到 `TlsAuthFailed` 直接 break，避免重试刷屏。

**修复思路（4 点）**：

1. **错误分类放 ctx 不放 state**——ctx 是 IO 边界，最了解异常类型。
2. **事件而不是返回值**——错误细节通过事件外推，主返回值保持简单。
3. **OCE 单独处理且透传**——取消不是错误，不应触发 `OnConnectFailed`。
4. **TLS 失败不再盲目重试**——`Connecting` 订阅事件、知道是 `TlsAuthFailed` 就 break。"是否重试"是状态职责，不是 ctx 职责。

**遗留**：

- `TargetHost` 仍硬编码 `"localhost"`——归 **T4**。
