# TCP 客户端状态机重构 TODO

把 `Assets/Scripts/Net/Proto/GameTcpClient.cs` 拆成状态机 (`TcpClientStateMachine` + `TcpClientStateBase` 各状态) 的进度跟踪。

## 当前进度快照

| 文件 | 状态 |
|---|---|
| `TcpClientStateBase` / `TcpClientStateMachine` / `ITcpClientFSMCtx` / `TcpClientFSMCtx` / `TcpClientStateMachineMono` | 骨架已搭好 |
| `Init` | 真实实现（注册 handler → `Connecting`） |
| `Connecting` | 真实实现（成功 → `Connected`，失败/TLS/重试耗尽 → `Disconnected`，OCE → `null`） |
| `Connected` | 薄壳（~15 行）：调 `ctx.RunMessagePump(ct)`，OCE 透传，其他异常 → `Disconnecting` |
| `Handshaking` / `Reconnecting` / `Disconnecting` / `Disconnected` / `Closed` | 全是 `await UniTask.Yield()` 占位 |
| `Ready` | 已删除（合并到 `Connected`） |
| `GameTcpClient` 老代码 | 仍在跑（`GameClient.cs` 用），新 FSM 已具备同等 listen/parse/SendMessage 能力，等 #7 删除 |

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

### 2. ✅ 已修复 — `TcpClientStateBase` 的静态 `instanceDic` 与 `NextState` 字段冲突

**原问题**：

状态实例放在 **静态字典** 里，多个 `TcpClientStateMachine` 实例会共享同一组状态对象，而 `NextState` 是实例字段——两个 client 一起跑会互相把对方的 `NextState` 覆盖掉。同时这些状态对象永远不会被释放。

**修复方案**：状态无状态化（之前列的两条出路里的第一条）。

- 删掉 `NextState` 字段和 `SetNext<T>()` 帮助方法
- `RunAsyncInternal` 的签名从 `UniTask` 改成 **`UniTask<TcpClientStateBase>`**，返回值即下一个要执行的状态实例（用 `GetInstance<T>()` 取），返回 **`null`** 表示终止 FSM
- `GetInstance<T>()` 从 `private` 提到 `public static`，方便外部（启动器）取初始状态
- `TcpClientStateMachine.StartAsync` 消费新的返回值（`next = await _state.RunAsync(...)`），并新增泛型重载 `StartAsync<TInit>(token)` 当语法糖
- `TcpClientStateMachineMono` 用 `_stateMachine.StartAsync<Init>(_cts.Token)` 取代之前的 `new Init()`（解决 #9：不再绕过 instance 缓存）
- 各具体状态：
  - `Init` 改成 `return UniTask.FromResult<TcpClientStateBase>(GetInstance<Connecting>())`
  - `Connecting` 末尾 `return null`（成功 / 失败 / TLS 错误目前都终止 FSM；具体转移在 #3 里再写）
  - 7 个 stub 状态（`Handshaking` / `Connected` / `Ready` / `Reconnecting` / `Disconnecting` / `Disconnected` / `Closed`）：`await UniTask.Yield(); return null;`

**修复思路（4 点）**：

1. **状态变成"纯函数风格"**——`RunAsyncInternal(ctx, ct)` 只读取 `ctx` 和 `ct`，结果通过返回值给出，没有任何副作用写到状态对象自身上。共享 singleton 不再有竞争。
2. **下一状态在调用栈里流转**——`StartAsync` 用一个局部 `next` 变量保存返回值，紧接着赋给 `_state`。这把"下一状态"的生命周期严格限制在状态机自己的循环里，状态对象彻底不掺合。
3. **`null` = 终止 FSM**——比 enum / 哨兵对象都简单，类型系统直接表达"没有下一个状态"的语义；调用方只需 `while (_state != null)`。
4. **静态缓存继续保留**——状态对象现在真的可以共享，缓存只是为了省 `Activator.CreateInstance` 的反射开销。也连带解决了 #9（`new Init()` 绕过缓存）：入口现在统一走 `GetInstance<Init>()`。

**顺手解决了**：

- ✅ #9（`new Init()` 绕过 instanceDic）—— 入口换成 `GetInstance<Init>()`，整套调用全走单例。

**未变的事**：

- `TcpClientStateMachine.StartAsync` 异常/取消处理（#1 已修过）保持不动。
- 当时 #2 完成时各状态之间的具体转移语义还是占位（`return null`），后由 #3 把 `Connecting` 转移到 `Connected` / `Disconnected`；其余状态间转移仍空缺，归 #6（`Connecting` vs `Reconnecting` 分工）等后续 task。

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
// 成功路径（早 return，finally 仍解订阅事件）
if (client != null) {
    Debug.LogFormat("Connect to {0} success", ctx.TargetEndPoint);
    return GetInstance<Connected>();
}

// OCE 路径（catch 里 return，finally 仍解订阅）
} catch (OperationCanceledException) {
    Debug.LogFormat("Connect to {0} canceled", ctx.TargetEndPoint);
    return null;   // 干净终止 FSM
}

// 重试耗尽 / TLS 永久失败 → 走到底部
return GetInstance<Disconnected>();
```

**修复思路（4 点）**：

1. **成功用早 return，不再借标志位 + 底部 return**——靠 `try/finally` 自动确保事件解订阅，代码更紧凑、控制流一目了然。
2. **TLS 失败和重试耗尽合并到同一出口**——两者都属于"非异常的失败收尾"，落到底部 `return GetInstance<Disconnected>()`。状态机不需要区分原因，UI 通过 `OnConnectFailed` 事件已经拿到 `ConnectErrorKind`。
3. **OCE 在 catch 里 `return null`**——明确表示"FSM 干净终止"，不继续走任何后续状态；和 `StartAsync` 顶层的 OCE 处理形成两道防线。
4. **不拆 `Handshaking` 状态**——保持 `Connecting` 内部由 `ctx.Connect()` 一次性完成 TCP+SSL；等真的需要细分 SSL 握手 / 业务握手时再做。决策依据：状态机只代表 TCP 网络层生命周期。

**未做的**：把 `AuthenticateAsClientAsync` 拆到独立 `Handshaking` 状态——推迟到真有需求时再做。

### 4. ✅ 已修复（2026-05-08 联调通过）— `Connected` 是"稳态 + 事件驱动"的，但 FSM 是"线性 next 指针"模型

> **联调验证**：在 `TcpClientStateMachineMono` 上加临时 `[Button("Send HelloWorld")]`，发送 `HelloRequest` 给服务器，`TcpHelloWorldHandler` 收到 `HelloReply` 并输出日志。**收发 framing + listen/parse + handler dispatch 全链路通**。临时按钮代码归 #5（公共 `SendMessage` API 落地时一起替换）。

> **决策（2026-05-08）**：原计划 `Connected → Ready` 两个状态，`Ready` 留作"业务握手完成"的扩展槽位。当前代码没有任何业务层握手（TLS 一通就直接 listen/parse 全速跑），保留 `Ready` 是对未实现需求的预留，违反 CLAUDE.md "Don't design for hypothetical future requirements"。**砍掉 `Ready`，listen/parse 直接落到 `Connected`**；真有业务握手时再起 `Authenticating` / `Handshaking` 等具名状态。

**问题本质**：

现在的 FSM 是**"状态跑完一段有限工作 → return 下一个状态"**模型。这套模型对短命状态（`Init` / `Connecting` / `Disconnecting`）天然合适——它们都有清晰的开始、清晰的终点、清晰的下一站。

但 `Connected` 不一样。`Connected` 是连接的**稳态（steady-state）**：socket 活着的整段时间它都在跑，可能几小时；本质不是"做完一件事"而是"**持续运行两条循环，等出事再换状态**"。`Connected` 同时要做三件事：

1. **并发跑两条长时循环**：`ListenLoopAsync`（读 `_stream` → push 进 `RingBufferStream`）+ `ParseLoopAsync`（从 ring buffer 解帧 → 分发 handler）。
2. **阻塞等待任意一种事件发生**：
   - 哪条循环先抛异常 / 退出（socket EOF / `IOException` / 协议错误）
   - `ct` 被取消（应用退出 / `Mono.OnDisable` 调 `_cts.Cancel()`）
   - **未来**：外部主动请求转移（UI 点"登出" / 鉴权 token 过期 / 服务端推 kick 后业务层决定登出）
3. **根据"是谁先唤醒了我"决定下一状态**：socket 错 → `Reconnecting`（如果允许自动重连，否则 `Disconnecting`）；主动 close → `Disconnecting`；`ct` 触发 → `return null`（FSM 干净退出，由 `StartAsync` 顶层 OCE catch 处理）。

旧 `GameTcpClient` 的解法：`StartLoop()` 把 listen/parse 当 fire-and-forget 起来；listen 循环 catch 到 `IOException("Disconnected")` 就**直接在异常处理里调 `CloseInternalAsync()` 自己改状态**。这是典型的"在工作循环的异常处理里搞副作用式状态转移"——正是 FSM 重构想消除的乱麻。

**核心矛盾**：

FSM 模型要求 `RunAsyncInternal` 完整执行完才 return 下一个状态——意味着 `Connected.RunAsyncInternal` 必须在两条循环跑着的时候**自己内部阻塞**等它们出问题。这对状态本身可行（`UniTask.WhenAny` 一把搞定），但暴露了一个被忽视的问题：**外部代码（UI 登出、token 失效）想推动 `Connected → Disconnecting`，没有通道**。

目前 FSM 没有"邮箱"。状态一旦在 `await`，外部唯一的干扰手段是取消 `ct`——但按当前语义（#1 已固化）取消 `ct` 表示"FSM 整个退出"而不是"切到下一状态"。两个语义需要分开。

**两条出路**：

- **A. `Connected` 内部 `WhenAny`，所有转移都靠状态内部驱动**

  ```csharp
  protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct) {
      using var disconnectCts = new CancellationTokenSource();   // 专用"软中断"
      ctx.OnDisconnectRequested += disconnectCts.Cancel;          // 外部按钮 → 触发它
      var listen = ListenLoopAsync(ctx, ct);
      var parse  = ParseLoopAsync(ctx, ct);
      try {
          int idx = await UniTask.WhenAny(
              listen, parse,
              UniTask.WaitUntilCanceled(disconnectCts.Token)
          );
          // idx 0/1 = 循环出事 → Reconnecting/Disconnecting；idx 2 = 主动登出 → Disconnecting
      } finally {
          ctx.OnDisconnectRequested -= disconnectCts.Cancel;
      }
      return GetInstance<Disconnecting>(); // 简化：先一律 Disconnecting，自动重连归 #6
  }
  ```

  - 优点：FSM 引擎不动；`Connected` 自己内聚，所有转移逻辑集中
  - 缺点：每个长时状态都得自己起专用 CTS / 订阅事件；多了点样板

- **B. ctx 提供 `RequestTransition<T>()`，FSM 引擎实现"软中断 + 强切"**

  ctx 持有 `UniTaskCompletionSource<Type>`；状态用 `WhenAny(workTask, transitionTask)`；外部调 `RequestTransition<Disconnecting>()` 解析 CS，状态拿到目标类型直接 `return GetInstance<...>()`。

  - 优点：外部代码一行就能切；调用方不用知道状态内部 CTS 细节
  - 缺点：FSM 引擎复杂；状态实现者必须主动 await 这个 CS——容易漏；调用方还可能在错误状态请求错误转移（要不要校验？）

**建议先走 A**：

1. 当前真正要触发转移的事件全是**内部**（socket 错、IOException、ct 取消）——A 直接覆盖。
2. 唯一可能要外部触发的是"用户主动断开"，但这功能还没写。功能落地时再判断要不要升 B；A 写法本身已经留了 `ctx.OnDisconnectRequested` 接入点，从 A 升 B 不是大动。
3. A 几乎不动 FSM 引擎，风险低。

**实现 `Connected` 时要顺手定的子问题**（依赖 #5 的部分先列在这里，以免漏）：

- `RingBufferStream` 放哪？建议放 **ctx**——理由：Reconnecting 之后回到 `Connected` 时可复用同一个 ring（避免重新分配 + 短暂内存峰值）；同时统一 ctx 作为"所有 IO 资源宿主"。
- `_stream`（`SslStream`）目前在 `TcpClientFSMCtx` 里是 `private`，`Connected` 要读它——ctx 要么暴露 `Stream NetworkStream { get; }`，要么把 listen/parse pump 搬进 ctx，`Connected` 调 `await ctx.PumpAsync(ct)`。**前者更直接**，且和 #5 一起做（ctx 同时给外部 `SendMessage` 一个写入入口，写也走 ctx）。
- `Disconnecting` vs `Reconnecting` 怎么分——这是 #6。实现 #4 时先一律 `return Disconnecting`，自动重连留 #6 决定。

**老代码里这次要彻底丢掉的**：

- `_isClosing`、`OnClose` 那一坨布尔标志——FSM 本身就是状态，这些属于"在没有 FSM 时被迫造的状态机"，迁移完不要带过来。
- 状态对象上的 `_listenTask` / `_parseTask` 字段——这俩 Task 应该是 `Connected.RunAsyncInternal` 的局部变量，状态对象（singleton）不能持有。
- "在 listen 循环 catch 里调 close 切状态"的反模式——改成"循环退出 → WhenAny 唤醒 → `Connected` 决定下一状态"，单一职责。

**实际落地代码**（`Assets/Scripts/Net/Proto/State/Connected.cs`）：

```csharp
protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
    using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

    UniTask listen = ListenLoopAsync(ctx, linkedCts.Token).Preserve();
    UniTask parse  = ParseLoopAsync(ctx, linkedCts.Token).Preserve();

    try {
        int idx = await UniTask.WhenAny(listen, parse);
        if (idx == 0) await listen; else await parse;  // 让先完成的循环把异常抛出
    } catch (OperationCanceledException) {
        throw;                                          // 顶层 ct 取消 → FSM 整体退出
    } catch (Exception e) {
        Debug.LogFormat("Connection broken: {0}", e.Message);
    } finally {
        linkedCts.Cancel();                             // 把另一条循环也叫停
        try { await listen; } catch { /* expected */ }
        try { await parse; }  catch { /* expected */ }
    }
    return GetInstance<Disconnecting>();
}
```

ctx 同步加了两个属性（`Assets/Scripts/Net/Proto/ITcpClientFSMCtx.cs` + `TcpClientFSMCtx.cs`）：

```csharp
Stream NetworkStream { get; }       // = SslStream（SslStream : Stream）
RingBufferStream RingBuffer { get; } // 整个 ctx 生命周期复用一个
```

**修复思路（4 点）**：

1. **`linkedCts` 串两条循环 + 主 `ct`**——任一条循环死掉，进入 `finally` 后 `linkedCts.Cancel()` 把另一条也叫停，避免一个崩了另一个还在 `await` 的资源泄漏。`ct` 取消时也同步把循环带下。
2. **`Preserve()` 才能 await 两次**——`UniTask` 默认只能 await 一次（struct 语义）；要在 `WhenAny` 之后再 await 拿异常 + 在 `finally` 里 drain，必须先 `Preserve()`。漏写会 InvalidOperationException。
3. **OCE 透传，其他异常吞**——`ct.Cancel()` 进 catch OCE → `throw` → `StartAsync` 顶层 OCE 分支干净退出 FSM；`IOException` / 协议错误 / `ObjectDisposedException` 等被吞掉记日志，让出口走到 `Disconnecting` 状态做清理。这与 `Connecting` 的 OCE 处理对齐。
4. **资源宿主统一在 ctx**——`RingBuffer` 由 ctx 持有的好处：以后 #6 实现 `Reconnecting → Connected` 回路时，可复用同一个 ring（避免 8KB 内存峰值）；`NetworkStream` 暴露成 `Stream` 而不是 `SslStream`，让 state 不依赖 TLS 细节，未来换 PlainStream 也不用改 state。

**未做的（留给后续）**：

- **`OnDisconnectRequested` 软中断事件**：设计里讨论过，但当前还没有真正的外部触发方（UI 登出按钮）。等需求落地再加，避免给 FSM 引擎加无人调用的 hook。从 `linkedCts` 升级到"再额外串一个 disconnect token"代码改动 < 5 行。
- **socket 出错时区分 `Reconnecting` vs `Disconnecting`**：归 #6。当前一律 `Disconnecting`。
- **`SendMessage` 的对外 API**：归 #5。`NetworkStream` 已经在 ctx 上能写，但还没有 framing/加锁/线程安全的发送入口。
- **删除 `GameTcpClient` 老代码**：归 #7。当前 `GameTcpClient` 仍含 listen/parse 副本——让两套 FSM/老代码并存，便于回滚验证。

**后续瘦身（2026-05-08，#5 完成后顺手做）**：

把 listen/parse/magic-number 解析这一坨代码从 `Connected.cs` 搬回 `TcpClientFSMCtx`，Connected 缩成 ~15 行：

```csharp
protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
    try {
        await ctx.RunMessagePump(ct);
    } catch (OperationCanceledException) {
        throw;
    } catch (Exception e) {
        Debug.LogFormat("Connection broken: {0}", e.Message);
    }
    return GetInstance<Disconnecting>();
}
```

理由：

1. **对称性**：发送侧 `SendMessage` 已经在 ctx（framing + 写锁 + magic）；接收侧（listen + parse + magic 匹配 + dispatch）属于同类东西，应该住同一栋楼，`MAGIC_NUMBER` 只剩一份。
2. **职责**：状态应该回答"我在哪个生命周期阶段、下一步走哪"，不是"魔数怎么匹配、CRC 怎么校验"——后者是 wire 协议层的事。
3. **接口收紧**：`ITcpClientFSMCtx.NetworkStream` / `RingBuffer` 在 #5 完成后已无消费者，一并从接口删掉；`TcpClientFSMCtx` 内部仍保留 `_stream` / `_ringBuffer` 作为私有字段。

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

### 5. ✅ 已修复 — 公共 API 面：`Connect` 签名拧 + 没有 `SendMessage` 入口

**原问题**：

两件事捆在一起：

1. `ITcpClientFSMCtx.Connect` 返回 `UniTask<TcpClient>`，但 `_client` / `_stream` 实际写在 ctx 内部；调用方只用返回值做 null 检查，`TcpClient` 没必要泄漏。
2. 没有公共 `SendMessage` 入口。`Connected` 想发心跳没接口、外部 UI/`GameClient.cs` 也没法调；老代码靠 `GameTcpClient.SendMessage` 直写 `_stream`，没有写入串行化（SslStream 不允许并发写）。
3. `TcpClientStateMachineMono` 完全私有，外部拿不到 ctx，订阅不到 `OnConnectFailed`，也没法发消息（参见 #10 已知遗留）。

**修复方案**：

① `Connect` 返回类型改 `UniTask<bool>`，client/stream 通过 ctx 属性取：

```csharp
UniTask<bool> Connect(CancellationToken ct = default);  // 接口
```

② 接口加 `SendMessage`，实现走 `SemaphoreSlim` 串行化 + 大端 framing + `WriteAsync`：

```csharp
// ITcpClientFSMCtx.cs
UniTask SendMessage(MessageType messageType, IMessage message, CancellationToken ct = default);
```

```csharp
// TcpClientFSMCtx.cs
private readonly SemaphoreSlim _writeLock = new(1, 1);

public async UniTask SendMessage(MessageType messageType, IMessage message, CancellationToken ct = default) {
    Stream stream = _stream;
    if (stream == null) {
        throw new InvalidOperationException("SendMessage called while not connected (NetworkStream is null)");
    }
    byte[] payload = BuildFrame(messageType, message);

    await _writeLock.WaitAsync(ct).AsUniTask();
    try {
        await stream.WriteAsync(payload, 0, payload.Length, ct).AsUniTask();
    } finally {
        _writeLock.Release();
    }
}

private static byte[] BuildFrame(MessageType messageType, IMessage message) {
    byte[] body = message.ToByteArray();
    uint crc = Crc32.Compute(body, 0, body.Length);
    using MemoryStream mem = new MemoryStream(4 + 2 + 4 + body.Length + 4);
    mem.Write(sMagicNumberBytes, 0, 4);
    mem.Write(((ushort)messageType).GetBytesBigEndian(), 0, 2);
    mem.Write(((uint)body.Length).GetBytesBigEndian(), 0, 4);
    mem.Write(body, 0, body.Length);
    mem.Write(crc.GetBytesBigEndian(), 0, 4);
    return mem.ToArray();
}
```

③ `TcpClientStateMachineMono` 暴露 `public ITcpClientFSMCtx Context => _ctx`；调试 `[Button]` 改为一行 `_ctx.SendMessage(...).Forget()`，去掉之前 25 行的 framing helper。

④ `Connecting.RunAsyncInternal` 适配新签名：`bool ok = await ctx.Connect(ct)` 取代 `TcpClient client = ...`。

⑤ `TcpClientFSMCtx.Client` 公开属性删掉（之前 grep 也没人用）——彻底切断 `TcpClient` 对 state / 外部的泄漏。

**修复思路（4 点）**：

1. **`bool` 比 `TcpClient?` 表意更准**——调用方关心的是"连上了没"，不关心拿到啥具体类型。返回 `bool` 让 `if (ok)` 这种写法不再需要解释。
2. **写锁放 ctx 不放 state**——SslStream 的并发约束是 IO 边界的事，不是状态机的事；多个状态都可能调 `SendMessage`，锁集中在一处比每个状态自己加保险。`SemaphoreSlim` 比 `lock` 好的点是支持 `await` + `CancellationToken`。
3. **`SendMessage` 异步而不是同步**——同步 `Write` 在主线程上阻塞 SslStream 写出（虽然实际很快），换 `WriteAsync` 让发送不会卡帧、还能配合 ct 取消。返回 `UniTask` 让调用方自己选 await / Forget。
4. **暴露 `ITcpClientFSMCtx` 而不是 `TcpClientFSMCtx`**——外部代码看到的是接口，私有实现细节（`_writeLock`、`_client` 字段、`Dispose`）藏起来。换实现（mock / fake）也容易。

**未做的（不属于本步）**：

- DNS 同步阻塞构造函数（旧 #6，TODO 编号上现在轮空了，下次重排）
- 删 `GameTcpClient`（归 #7）
- `Connecting` vs `Reconnecting` 分工（归 #6 当前的"重排后"）

**已知遗留**：

- `TcpClientFSMCtx.SendMessage` 抛 `InvalidOperationException` 是有意为之——调用方未连接就发消息属于编程错误，应该崩出来便于发现。后续如果要支持"离线消息排队"，再加 `TryQueueMessage` 之类的非异常 API。
- ~~`MAGIC_NUMBER` 在两处~~——已通过 #4 的"后续瘦身"合并：parse/listen 搬回 ctx，`MAGIC_NUMBER` 只剩 ctx 一份。

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

### 9. ✅ 已修复（随 #2 一起） — `new Init()` 绕过了 instanceDic 缓存

`TcpClientStateMachineMono.OnEnable` 现在用 `_stateMachine.StartAsync<Init>(_cts.Token)`，内部调用 `TcpClientStateBase.GetInstance<Init>()` 取单例，整套调用都走缓存。

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

- ✅ ~~`TcpClientStateMachineMono` 暂未把 ctx / 事件暴露给外部~~——已通过 #5 暴露 `public ITcpClientFSMCtx Context`，UI 现在能订阅 `OnConnectFailed`。
- `TargetHost` 仍硬编码 `"localhost"`——证书装好后下一步可能撞 `CN_MISMATCH`。看证书 SAN 决定是否要把 host 名传进 ctx。

---

## 接下来的工作优先级

按这个顺序做：

- [x] **1. 重塑 `TcpClientStateBase` API（已落地 #1 + #2 + #9）**——`RunAsyncInternal` 改成 `UniTask<TcpClientStateBase>` 返回下一状态，删掉 `NextState` 字段和 `SetNext`。`GetInstance<T>` 提为 public，状态彻底无副作用。**注**：#5（`ITcpClientFSMCtx.Connect` 的返回签名 + ctx 不该泄漏 TcpClient 给 state）独立于基类 API，仍未解决。

- [x] **2. 修 `TcpClientStateMachine` 的异常/取消语义**——OCE 干净 `return`；其他异常打日志 + `return`；`OnExit` 用 `try/finally` 保证执行。FaultedState 还没引入。

- [x] **3. 把 `Connecting` 写完整**（部分完成）：
    - ✅ 成功 → `GetInstance<Connected>()`（暂不拆 `Handshaking`，`Connecting` 内部仍由 `ctx.Connect()` 一次性完成 TCP + SSL）
    - ✅ 重试耗尽 / TLS 永久失败 → `GetInstance<Disconnected>()`
    - ✅ OCE → `return null`（FSM 干净退出）
    - ⏸️ **未做的**：把 `AuthenticateAsClientAsync` 从 `ctx.Connect` 拆到独立 `Handshaking` 状态。这一步推迟到真的需要细分 SSL 握手 / 业务握手时再做。
    - 决策依据：状态机只代表 **TCP 网络层** 生命周期；游戏层（登录/选角/对局）单开 FSM 通过事件观察 TCP 状态，不混在一起。

- [x] **4. 写 `Connected` 状态——真正的硬骨头**（已落地）：
    - ✅ 删 `Assets/Scripts/Net/Proto/State/Ready.cs` + `.meta`
    - ✅ ctx 暴露 `Stream NetworkStream { get; }` 和 `RingBufferStream RingBuffer { get; }`
    - ✅ `Connected.RunAsyncInternal` 用 `linkedCts` + `UniTask.WhenAny(listen, parse)` + `Preserve()` + `finally` 中 drain
    - ✅ OCE 透传 → `StartAsync` 顶层 catch；其他异常 → `Disconnecting`
    - ⏸️ **未做**：外部"软中断"事件 `OnDisconnectRequested`——没有真正的调用方，等需求落地（UI 登出按钮）再加，从 `linkedCts` 升级 < 5 行
    - ⏸️ **未做**：socket 错时区分 `Reconnecting`/`Disconnecting`——归 #6，当前一律 `Disconnecting`

- [x] **5. 设计公共 API 面**（已落地）：
    - ✅ `Connect` 返回 `UniTask<bool>`，不再泄漏 `TcpClient`
    - ✅ ctx 加 `SendMessage(MessageType, IMessage, ct)`，内部 `SemaphoreSlim` 串行化 + 大端 framing + 异步 `WriteAsync`
    - ✅ `TcpClientStateMachineMono` 暴露 `public ITcpClientFSMCtx Context => _ctx`，外部按接口拿能力
    - ✅ `Connecting.cs` 适配新 `Connect` 签名（`bool ok = await ctx.Connect(ct)`）
    - ✅ 顺手解决 #10 的"Mono 不暴露 ctx" 遗留
    - ✅ **顺手瘦身 Connected**：把 listen/parse/magic 解析搬回 ctx，新增 `RunMessagePump`；接口删 `NetworkStream` / `RingBuffer`；Connected 从 ~105 行 → ~15 行（详见 #4 末尾"后续瘦身"小节）

- [ ] **6. 想清楚 `Connecting` vs `Reconnecting` 的分工**：是否让 `Connecting` 只跑一次（首次连接），重试逻辑挪到 `Reconnecting`？现在重试循环写在 `Connecting` 里，但又有个独立 `Reconnecting` 状态，语义重复。

- [ ] **7. 删除 `GameTcpClient` 旧实现**，把 `GameClient.cs` 改成用 `TcpClientFSMCtx` + `TcpClientStateMachineMono`。这一步要等 #4、#5 全跑通再做。

---

## 备注

- #1、#2、#5 是 API 形状决定，必须先做。
- #4 是工作量最大的一块，但要等 API 稳定才能下手。
- #6 是设计决策，可以在写 `Reconnecting` 之前再敲定。
