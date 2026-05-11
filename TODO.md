# TCP 客户端状态机重构 TODO

把老的 `Assets/Scripts/Net/Legacy/GameTcpClient.cs` 拆成状态机模块（`Assets/Scripts/Net/Tcp/`）的进度跟踪。

历史决议（已完成的 #1-#10 结构问题、T2-T4 小清理）参见 git log。

---

## 当前进度快照

| 文件 | 状态 |
|---|---|
| `TcpClientStateBase` / `TcpClientStateMachine` / `ITcpClientFSMCtx` / `TcpClientFSMCtx` / `TcpClientStateMachineMono` | 骨架完成，T2-T4 清理完毕 |
| `Init` | 真实实现（注册 handler → `Connecting`） |
| `Connecting` | 真实实现（成功 → `Connected`，失败/TLS/重试耗尽 → `Disconnected`，OCE → `null`） |
| `Connected` | 薄壳（~15 行）：调 `ctx.RunMessagePump(ct)`，OCE 透传，其他异常 → `Disconnecting` |
| `Disconnecting` | 真实现：调 `ctx.CloseConnection()` 关 socket，转 `Disconnected` |
| `Handshaking` / `Disconnected` | 占位（`await UniTask.Yield()`） |
| `Closed` | 已删除（T1.2，没人转过去；`Disconnected` 返 null 已足够终止 FSM） |
| `Reconnecting` / `Ready` | 已删除（YAGNI） |
| `Net.Legacy.GameTcpClient` 老代码 | 已挪到 `Net/Legacy/` 文件夹 + `Net.Legacy` namespace；`Net.Mono.GameClient` 仍引用，与新模块并行，将来另行替换 |

---

## 待办：T1 — 把新 TCP Client 完善为独立、功能完整的模块

> **目标**：新 FSM 模块（`TcpClientFSMCtx` + `TcpClientStateMachine` + `TcpClientStateMachineMono` + 各 state）作为**自包含、可对外发布的模块**——为之后单独拆 asmdef 做准备。
>
> **范围**：**不动**老 `GameTcpClient` / `GameClient` / 登录状态机 / UI Mono / 场景资源——它们沿用旧路径直到将来另行替换。

### 模块当前状态盘点

**已有能力**：
- `Connect()` / `SendMessage()` / `RunMessagePump()` 公共 API（接口上）
- `OnConnectFailed` 事件（连接错误分类外推）
- TLS（`TargetHost` 可参数化）/ framing / handler dispatch / 写锁 / DNS 异步化

**功能性缺口**：

| 缺什么 | 当前症状 |
|---|---|
| `Disconnecting` 状态没真实现 | `await UniTask.Yield(); return null;` 占位；socket / stream 实际清理仅靠 ctx.Dispose 兜底 |
| `Mono.OnDisable` 没 dispose ctx | `TcpClient` / `SslStream` / `SemaphoreSlim` / `RingBufferStream` 全部 leak |
| ~~`Closed` 状态没人转过去~~ | 已删（T1.2） |
| 状态观察 API 缺位 | 外部代码无法问"当前哪状态"或订阅状态变化；只有 `OnConnectFailed` |
| `Mono` 缺公开 `Connect()` / `Disconnect()` / `IsConnected` | 只能 `OnEnable` 自动起；调用方没法手动控制 |

**模块化卫生**（asmdef 拆分时会撞）：

| 问题 | 影响 |
|---|---|
| `TcpClientStateMachineMono` 的 `[Button("Send HelloWorld")]` 引用 `HelloRequest`（proto-specific） | 模块依赖应用层 message 类型 |
| `RingBufferStream` 在全局命名空间（无 namespace） | 易撞名 |
| `Game.Util.Crc32` 跨 namespace 依赖 | asmdef 边界要么把 Crc32 拉进模块、要么模块 asmdef 引用 Game.Util |
| public 边界没复盘 | 多数类无脑 public，asmdef 拆时要分清 internal vs public |

### 子任务（按依赖顺序）

#### T1.1 — ✅ `Disconnecting` 状态真实现 + 软中断信号机制 + Mono 资源释放（2026-05-10 落地）

**前置：盘点出的设计问题（2026-05-10 review）**

写代码前发现这些问题必须先想清楚：

1. **🔴 主 ct 取消时不经过 Disconnecting**——`StartAsync` 顶层 catch OCE 后直接 `return`；`Connected` 收到 OCE 也透传。意味着 `Mono.OnDisable`（cancel cts）路径下 Disconnecting 永远跑不到，资源 leak。
2. **🔴 `ctx.Dispose()` 漏关 `_stream`**——`SslStream(stream, false, ...)` 第二个参数 false 表示不拥有底层 stream，所以 `_client.Dispose()` 不会顺带关 SslStream。独立 bug。
3. **🟡 Disconnecting 拿不到 `_client` / `_stream`**——它们在 ctx 里 `private`；state 看到的只是 `ITcpClientFSMCtx` 接口。需要 ctx 暴露 close 方法。
4. **🟡 close 操作不该被 ct 中断**——清理动作必须 best-effort 跑完；`ctx.CloseConnection()` 不接受 ct，或用独立短 timeout。
5. **🟡 `_cts.Cancel()` 紧跟 `_cts.Dispose()` 疑似重入**——FSM 还在跑（`Forget()` 起的），dispose 后 await ct 可能拿 `ObjectDisposedException`。改为先 cancel、等 FSM 跑完再 dispose（拆到 `OnDestroy`）。
6. **🟢 `Connecting → Disconnected`（不经 Disconnecting）这条不是 bug**——Connecting 阶段从未真的连上，没有 socket / stream 要清理。文档里写明语义即可。

**方案 B：软中断信号机制**（敲定）

ctx 暴露"主动断开请求"事件，与主 ct 的"FSM 紧急退出"语义严格分开：

| 触发方式 | 走 Disconnecting？ |
|---|---|
| socket 自然断（IOException 等） | ✅ |
| 主动调 `ctx.RequestDisconnect()`（软中断） | ✅ |
| 主 ct 取消（紧急退出） | ❌（直接死，靠 `ctx.Dispose()` 兜底） |

**子任务**

##### T1.1a — ctx 加 close API + 软中断事件

**文件**：`ITcpClientFSMCtx.cs` + `TcpClientFSMCtx.cs`

- 接口加 `event Action OnDisconnectRequested;`
- 接口加 `void RequestDisconnect();`（触发事件）
- 接口加 `UniTask CloseConnection();`（best-effort，不接受 ct）
- 实现：
  - 私有 `CloseConnectionInternal()` 同步关 `_stream` / `_client`，吞异常
  - `CloseConnection()` 调内部方法 + `return UniTask.CompletedTask`
  - `Dispose()` 也调 `CloseConnectionInternal()`，再 dispose ringBuffer / writeLock（修了漏关 stream 的 bug）
  - `RequestDisconnect()` 触发 `OnDisconnectRequested?.Invoke()`

##### T1.1b — `Connected` 处理软中断

**文件**：`State/Connected.cs`

- 起独立 `disconnectCts`，订阅 `ctx.OnDisconnectRequested` 触发它
- `linkedCts = ct + disconnectCts`，传给 `RunMessagePump`
- catch OCE 时 `when (ct.IsCancellationRequested) throw;`——主 ct 取消还是紧急退出
- 否则（软中断）→ 走到 `return GetInstance<Disconnecting>()`
- `finally` 解订阅事件

##### T1.1c — `Disconnecting` 实现

**文件**：`State/Disconnecting.cs`

```csharp
protected override async UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
    await ctx.CloseConnection();   // best-effort，不传 ct
    return GetInstance<Disconnected>();
}
```

##### T1.1d — Mono 调整生命周期

**文件**：`TcpClientStateMachineMono.cs`

- `OnDisable`：调 `_ctx?.RequestDisconnect()`，**不**取消 cts，让 FSM 跑完 Disconnecting
- `OnDestroy`：兜底——cancel + dispose cts，dispose ctx
- 顺手修：`_cts` 的 cancel/dispose 移到 OnDestroy，避免重入风险

##### T1.1e — Connecting 也要订阅 OnDisconnectRequested（联调中发现，2026-05-10）

**症状**：在 `Connecting` 重试期间 disable Mono 然后 re-enable，console 出现 `2/10 Connect to ... failed` 重复打印——OLD `Connecting` 的循环没被中止，与 NEW FSM 的 `Connecting` 同时跑。

**根因**：T1.1b 只让 `Connected` 订阅了 `OnDisconnectRequested`。`Connecting` 也是长跑状态（重试循环最多 `MaxAttempts × (TimeoutMs + RetryDelayMs)` 秒）；没订阅信号意味着它跑完才走人。

**结论**：**所有长跑状态都必须订阅 `OnDisconnectRequested`**——这是 T1.1 软中断设计的隐含契约。

**修复**（`State/Connecting.cs`）：

- 同样的 `disconnectCts` + `linkedCts(ct, disconnectCts.Token)` 模式
- `await ctx.Connect(linkedCts.Token)`（之前是 `ct`）
- `await UniTask.Delay(..., cancellationToken: linkedCts.Token)`（之前是 `ct`）
- OCE catch 用 `when (disconnectCts.IsCancellationRequested && !ct.IsCancellationRequested)` 区分软中断；落入此分支 → return `Disconnected`
- 主 ct / per-attempt 超时等其他 OCE → 保持原行为，`return null` FSM 退出
- `finally` 解订阅事件

**未解决的相关问题**（暂记不修）：

`Mono.OnEnable` 当前直接 `_cts = new CancellationTokenSource()`——如果 OLD FSM 还在退出途中（理论上微秒级，但极端情况下可能重叠），OLD `_cts` 被覆盖、不 dispose、内存 leak；OLD FSM 也仍持有 OLD ct 继续跑。 

实际场景下 OLD FSM 在收到软中断后**几个 async tick 内**完整退出，用户点 disable→re-enable 之间至少几帧间隔，足够 OLD 跑完。**当前不修**；如果将来发现 race，可在 OnEnable 加 `if (_cts != null) return;` 防御。

**总工作量**：~60 行（初始）+ ~15 行（T1.1e 补丁）

#### T1.2 — ✅ 砍 `Closed` 状态（2026-05-10 落地）

**文件**：`State/Closed.cs` + `.meta`

- 当前没人转过去；`Disconnected` 已经返回 null（FSM 自然终止），不需要单独的 Closed
- YAGNI 原则——真要"已显式关闭、不允许重连"语义，将来再加
- 删 `Closed.cs` + `.meta`

**工作量**：rm

#### T1.3 — 引擎层暴露状态观察

**文件**：`TcpClientStateMachine.cs`

- 加 `public TcpClientStateBase CurrentState => _state;`
- 加 `public event Action<TcpClientStateBase, TcpClientStateBase> OnStateChanged;`（`(prev, next)`）
- `StartAsync` 状态切换处 fire 事件

**工作量**：~10 行

#### T1.4 — `Mono` 加公开 `Connect()` / `Disconnect()` / `IsConnected` / `OnStateChanged`

**文件**：`TcpClientStateMachineMono.cs`

- `public bool IsConnected => _stateMachine?.CurrentState is Connected;`
- `public event Action<TcpClientStateBase, TcpClientStateBase> OnStateChanged;`（中转引擎事件）
- `public void Connect()`：幂等。`_cts` 为 null 就新建 cts + `StartAsync<Init>(_cts.Token).Forget()`；非 null 就 no-op
- `public void Disconnect()`：取消并 dispose `_cts`，置 null（`OnDisable` 复用此方法）
- `OnEnable` → `Connect()`；`OnDisable` → `Disconnect()` + `_ctx?.Dispose()`（T1.1 那条复用）

**工作量**：~30 行

#### T1.5 — 模块化卫生

**✅ RingBufferStream 已加 namespace**（随文件夹拆分一起做）：现已在 `namespace Net.Tcp` 中。

**调试按钮迁出 Mono**（仍待办）：

- `[Button("Send HelloWorld")]` + `DebugSendHelloWorld()` 引用了应用层 `HelloRequest`
- 选项 A：删掉调试按钮——验证收发的工作交给独立测试 Mono / 测试场景做
- 选项 B：拆出独立 `HelloWorldDebugMono`（在 `Net/Handlers/` 或 `Tests/` 下），调用 `tcpClient.Context.SendMessage(...)`
- **建议选 B**——保留调试便利但不让模块沾应用层依赖

**工作量**：~15 行

#### T1.6 — internal vs public 边界复盘（为 asmdef 拆分铺路）

走一遍模块所有类，标注真正"模块外"用得到的 vs 只是"实现细节"。当前 asmdef 还没拆，但提前把访问修饰符摆对，将来拆 asmdef 几乎零工作量：

候选 internal（外部不需要）：
- `TcpClientStateBase` 各 state 子类（`Init` / `Connecting` / `Connected` / `Disconnecting` / `Disconnected` / `Handshaking`）——外部只通过 Mono / 引擎接触状态，不直接 new
- `TcpClientFSMCtx`（concrete）——外部应通过 `ITcpClientFSMCtx` 接口
- `TcpClientStateMachine`（engine）——外部通过 Mono 控制
- `MessageHandlerWrapper`（implementation detail）

保留 public（模块对外 API）：
- `TcpClientStateMachineMono`（Mono entry）
- `ITcpClientFSMCtx`（接口）
- `TcpClientStateBase`（abstract base，`CurrentState` 类型 + `OnStateChanged` 事件参数类型）
- `IMessageHandler<T>` / `MessageHandlerRegistry`（应用层注册 handler 的入口）
- `ConnectErrorKind`（事件参数）
- `RingBufferStream`（如果将来要让外部 push/pop——多半不需要，可设 internal）

**注**：当前还在同 asmdef 里，public/internal 行为差异不可见；这步是 prep work，做完后将来拆 asmdef 时一行 asmdef 文件搞定。

**工作量**：~10 个类的访问修饰符改动 + 一遍 grep 确认外部消费者

#### T1.7 — 模块入口文档

**文件**：`Assets/Scripts/Net/Proto/README.md`（新增）或在 `TcpClientStateMachineMono` 顶部写大段 docstring

内容：

- 模块定位（"TCP + TLS + 自定义 framing 的客户端，FSM 驱动"）
- 公共 API 速查（`TcpClientStateMachineMono.Connect / Disconnect / IsConnected / OnStateChanged / Context`）
- 收发消息的最小用法示例
- 注册 handler 的最小用法示例
- 错误处理（`OnConnectFailed` 订阅）

**工作量**：写文档，~80 行 markdown / ~30 行 docstring

#### T1.8 — 烟囱测试（在测试场景做，不碰主场景）

- `GameClient2.unity`（或新建 `TcpClientTestScene.unity`）里挂 Mono + 测试 Helper Mono
- 验证：`Connect()` → 连上 → 发 HelloWorld → 收 HelloReply → `Disconnect()` → 资源释放（看 console 没泄漏警告 / scene 切换正常）
- 复测错误路径：故意填错地址 → `OnConnectFailed` 触发 → FSM 走到 `Disconnected`

---

## 延后（条件触发，目前无需做）

只在对应触发条件出现时再启动。

| 项 | 触发条件 |
|---|---|
| 拆 `Handshaking` 独立状态 | 业务层握手需求落地（版本协商 / 鉴权等） |
| `OnDisconnectRequested` 软中断事件 | 用户主动登出 / token 过期等"FSM 不退出但状态切回 Disconnected"的场景 |
| 自动重连 | 真要重连时让 `Disconnected → Connecting` |
| 业务消息排队（offline send queue） | 真要支持"未连接也能 SendMessage" |
| `FaultedState` 故障恢复 | 真要做"状态崩了走错误恢复路径" |
| `Init.RegisterAllByReflection` 在非 Editor build 跳过 | 真要出 player build 时（现在还没需求） |
| 拆独立 asmdef | 模块功能完整后再做（T1 是其前置） |
| 把 `GameTcpClient` / `GameClient` / 旧 `ETcpConnectionState` 替换掉 | 上面拆 asmdef 完成后或更晚 |

---

## 设计原则备注

跨步骤共享的架构约束，回头改代码时回来对一下。

- **TCP 状态机只覆盖网络层**：游戏层（登录 / 选角 / 对局）单开 FSM，通过 ctx 事件观察 TCP 状态，不混在一起。
- **状态无副作用**：`RunAsyncInternal(ctx, ct)` 只读 ctx + ct，结果通过返回值给出；`null` 终止 FSM。状态对象是 singleton，由 `TcpClientStateBase.GetInstance<T>()` 取。
- **wire 协议全在 ctx**：listen / parse / framing / magic / CRC / handler dispatch 都在 `TcpClientFSMCtx` 里；状态只做生命周期编排。
- **错误分类放 ctx 不放 state**：连接错误以 `ConnectErrorKind` 分类 + `OnConnectFailed` 事件外推；UI 订阅事件而不是 state catch 异常。
- **取消 vs 转移**：`ct` 取消 = "FSM 整体退出"（顶层 OCE catch 干净 return）；状态间转移靠 `RunAsyncInternal` 返回值。两个语义严格分开。
- **状态对象禁止持有任何字段**：所有运行期数据进 `RunAsyncInternal` 的局部或 ctx，singleton 不能有可写状态。
