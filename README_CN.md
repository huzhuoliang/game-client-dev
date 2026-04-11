# Game Client

[English](README.md) | **中文**

---

基于 Unity (2022.3.12f1) 的游戏客户端，包含自研的 TCP 网络框架，使用 SSL/TLS 加密和 Protocol Buffers 序列化。

## 网络框架

### 概述

网络层提供了一个可靠的、类型安全的 TCP 客户端，具备以下特性：

- **SSL/TLS 加密** — 所有流量通过 `SslStream` 进行身份验证
- **Protocol Buffers 序列化** — 由 `.proto` 定义生成的强类型消息
- **CRC32 完整性校验** — 每条消息体在接收时进行校验
- **自动重连** — 可配置重试次数、超时时间和重试间隔
- **全异步** — 基于 [UniTask](https://github.com/Cysharp/UniTask) 实现零分配的 async/await

### 通信协议

所有多字节字段均为**大端序**。

```
+----------+----------+----------+--------------+----------+
|  Magic   |   Type   |  Length  |     Body     |  CRC32   |
|  4 字节  |  2 字节  |  4 字节  |    变长      |  4 字节  |
| CAFEBABE |  uint16  |  uint32  |  protobuf    |  uint32  |
+----------+----------+----------+--------------+----------+
```

| 字段   | 大小    | 说明                                             |
|--------|---------|--------------------------------------------------|
| Magic  | 4 字节  | `0xCAFEBABE` — 用于帧同步定位                     |
| Type   | 2 字节  | `MessageType` 枚举值（范围 0–65535）              |
| Length | 4 字节  | Body 字段的字节长度                               |
| Body   | 变长    | Protobuf 编码的消息载荷（最大 1 MB）              |
| CRC32  | 4 字节  | 对 Body 字节计算的 CRC32 校验和                   |

### 架构

```
GameClient (MonoBehaviour)
  +-- GameTcpClient
        |-- ListenLoopAsync        -->  从 SSL 流读取数据写入 RingBufferStream
        |-- ParseLoopAsync         -->  消费帧数据，校验 CRC，分发消息
        +-- MessageHandlerRegistry (单例)
              +-- IMessageHandler<T>  -->  每种消息类型对应一个处理器
```

**`GameClient`** (`Assets/Scripts/Net/Mono/GameClient.cs`)
MonoBehaviour 封装，通过 Odin Inspector 在编辑器中暴露服务器地址和端口，持有 `GameTcpClient` 实例。

**`GameTcpClient`** (`Assets/Scripts/Net/Proto/GameTcpClient.cs`)
核心 TCP 客户端。管理完整的连接生命周期：Init → Connecting → Connected → Disconnecting → Disconnected。连接成功后启动两个并发异步循环：

- **ListenLoop** — 从 `SslStream` 读取原始字节写入 `RingBufferStream`
- **ParseLoop** — 扫描 Magic Number，读取帧头，提取并校验消息体，然后分发给注册的处理器

**`RingBufferStream`** (`Assets/Scripts/Net/Proto/RingBufferStream.cs`)
基于信号量的异步环形缓冲区。将网络 I/O 与消息解析解耦，使两个循环可以独立运行。

**`MessageHandlerRegistry`** (`Assets/Scripts/Net/Proto/MessageHandlerRegistry.cs`)
单例模式，维护 `MessageType` → `IMessageHandlerWrapper` 的映射。在 Unity Editor 中，处理器通过反射自动发现注册；在正式构建中，需要手动注册。

### 添加新消息类型

1. 在服务端的 proto 仓库中**定义 `.proto` 文件**，生成 C# 代码到 `Assets/Scripts/proto/`。

2. 在 proto 定义中为 `MessageType` **添加枚举值**。

3. 在 `Assets/Scripts/Net/Tcp/` 中**创建处理器**：

```csharp
using GameServerServices.YourProto;
using GameServerServices.MessageType;
using Net.Proto;

public class YourMessageHandler : IMessageHandler<YourResponse> {
    public MessageType MessageType => MessageType.MsgYourResponse;

    public void Handle(YourResponse message) {
        // 处理消息
    }
}
```

处理器在 Editor 中会被自动发现。正式构建时，需在 `GameTcpClient.RegisterAll()` 中手动注册。

4. 在任何能访问 `GameTcpClient` 的地方**发送请求**：

```csharp
var request = new YourRequest { /* 字段 */ };
tcpClient.SendMessage(MessageType.MsgYourRequest, request);
```

### 关键源文件

| 文件 | 用途 |
|------|------|
| `Assets/Scripts/Net/Mono/GameClient.cs` | MonoBehaviour 入口，Inspector 中的连接/断开按钮 |
| `Assets/Scripts/Net/Proto/GameTcpClient.cs` | TCP 客户端 — 连接、发送、接收、帧解析 |
| `Assets/Scripts/Net/Proto/RingBufferStream.cs` | 监听与解析之间的异步环形缓冲区 |
| `Assets/Scripts/Net/Proto/MessageHandlerRegistry.cs` | 单例消息类型 → 处理器分发 |
| `Assets/Scripts/Net/Proto/IMessageHandler.cs` | 处理器接口：`IMessageHandler<T>` |
| `Assets/Scripts/Net/Proto/IMessageHandlerWrapper.cs` | 泛型处理器的类型擦除封装 |
| `Assets/Scripts/Net/Proto/BitConverterExtension.cs` | 大端序字节转换辅助方法 |
| `Assets/Scripts/Util/Crc32.cs` | CRC32 校验和实现 |
| `Assets/Scripts/proto/` | 自动生成的 protobuf C# 文件（请勿手动编辑） |

## 依赖

| 包 | 用途 |
|----|------|
| [UniTask](https://github.com/Cysharp/UniTask) | Unity 零分配 async/await |
| [Google.Protobuf](https://github.com/protocolbuffers/protobuf) | Protocol Buffers 运行时（`Assets/Plugins/protobuf/`） |
| [Odin Inspector](https://odininspector.com/) | 增强的 Unity Inspector |
| [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/) | 资源管理 |
| [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@1.7/) | 过场动画序列 |

## 项目结构

```
Assets/Scripts/
|-- Game/               # 应用生命周期、状态机、UI、相机
|   |-- StateMachine/   # 通用异步状态机框架
|   |-- Login/          # 登录流程状态
|   +-- UI/             # 窗口管理 (UIManager, GameWindowBase)
|-- Net/
|   |-- Proto/          # TCP 客户端、环形缓冲区、处理器注册表、扩展方法
|   |-- Tcp/            # 具体消息处理器
|   +-- Mono/           # MonoBehaviour 封装 (GameClient)
|-- proto/              # 生成的 protobuf C# 文件
+-- Util/               # CRC32、日志、AssetBundle 工具
```
