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
| `Handshaking` / `Disconnecting` / `Disconnected` / `Closed` | 占位（`await UniTask.Yield()`） |
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
| `Closed` 状态没人转过去 | 死代码 |
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

#### T1.1 — `Disconnecting` 状态真实现 + Mono dispose ctx

**文件**：`State/Disconnecting.cs` + `TcpClientStateMachineMono.cs` + `TcpClientFSMCtx.cs`

**Disconnecting 真实实现**：

- 关 `_stream`（SslStream）：`Close()` + `DisposeAsync()`
- 关 `_client`（TcpClient）：`Close()` + `Dispose()`
- 这俩资源目前在 ctx 里 `private`——倾向方案：ctx 加 `public void CloseConnection()`，把"关闭顺序 + 异常吞掉"封在一处；状态调用即可
- 转移：→ `Disconnected`

**Mono 的 OnDisable**：

- 在 cancel cts 之后、置 null 之前，调 `_ctx?.Dispose()`
- 防止场景切换 / 应用退出时 socket / stream / semaphore 泄漏

**工作量**：~30 行

#### T1.2 — 砍 `Closed` 状态

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
