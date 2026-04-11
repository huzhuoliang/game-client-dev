# Game Client

A Unity (2022.3.12f1) game client with a custom TCP networking framework built on SSL/TLS and Protocol Buffers.

## Networking Framework

### Overview

The networking layer provides a reliable, type-safe TCP client with:

- **SSL/TLS encryption** — all traffic is authenticated via `SslStream`
- **Protocol Buffers serialization** — strongly-typed messages generated from `.proto` definitions
- **CRC32 integrity checks** — every message body is validated on receive
- **Automatic reconnection** — configurable retry count, timeout, and backoff delay
- **Async throughout** — built on [UniTask](https://github.com/Cysharp/UniTask) for allocation-free async/await

### Wire Protocol

All multi-byte fields are **big-endian**.

```
┌──────────┬──────────┬──────────┬──────────────┬──────────┐
│  Magic   │   Type   │  Length  │     Body     │  CRC32   │
│  4 bytes │  2 bytes │  4 bytes │  var length  │  4 bytes │
│ CAFEBABE │  uint16  │  uint32  │  protobuf    │  uint32  │
└──────────┴──────────┴──────────┴──────────────┴──────────┘
```

| Field  | Size    | Description                                      |
|--------|---------|--------------------------------------------------|
| Magic  | 4 bytes | `0xCAFEBABE` — used for frame synchronization    |
| Type   | 2 bytes | `MessageType` enum value (range 0–65535)         |
| Length | 4 bytes | Byte length of the Body field                    |
| Body   | varies  | Protobuf-encoded message payload (max 1 MB)      |
| CRC32  | 4 bytes | CRC32 checksum computed over the Body bytes      |

### Architecture

```
GameClient (MonoBehaviour)
  └── GameTcpClient
        ├── ListenLoopAsync   ──▶  reads SSL stream into RingBufferStream
        ├── ParseLoopAsync    ──▶  consumes frames, validates CRC, dispatches
        └── MessageHandlerRegistry (singleton)
              └── IMessageHandler<T>  ──▶  one per message type
```

**`GameClient`** (`Assets/Scripts/Net/Mono/GameClient.cs`)
MonoBehaviour wrapper that exposes server address/port in the Inspector (via Odin) and owns the `GameTcpClient` instance.

**`GameTcpClient`** (`Assets/Scripts/Net/Proto/GameTcpClient.cs`)
Core TCP client. Manages the full connection lifecycle: Init → Connecting → Connected → Disconnecting → Disconnected. Runs two concurrent async loops after a successful connection:

- **ListenLoop** — reads raw bytes from the `SslStream` into a `RingBufferStream`
- **ParseLoop** — scans for the magic number, reads the header, extracts and validates the body, then dispatches to the registered handler

**`RingBufferStream`** (`Assets/Scripts/Net/Proto/RingBufferStream.cs`)
Lock-free circular buffer with semaphore-based async reads. Decouples network I/O from message parsing so the two loops can run independently.

**`MessageHandlerRegistry`** (`Assets/Scripts/Net/Proto/MessageHandlerRegistry.cs`)
Singleton that maps `MessageType` → `IMessageHandlerWrapper`. In the Unity Editor, handlers are discovered automatically via reflection. In builds, they must be registered manually.

### Adding a New Message Type

1. **Define the `.proto` file** in the server-side proto repository and generate C# code into `Assets/Scripts/proto/`.

2. **Add an enum value** to `MessageType` in the proto definition.

3. **Create a handler** in `Assets/Scripts/Net/Tcp/`:

```csharp
using GameServerServices.YourProto;
using GameServerServices.MessageType;
using Net.Proto;

public class YourMessageHandler : IMessageHandler<YourResponse> {
    public MessageType MessageType => MessageType.MsgYourResponse;

    public void Handle(YourResponse message) {
        // handle the message
    }
}
```

The handler is auto-discovered in the Editor. For release builds, register it in `GameTcpClient.RegisterAll()`.

4. **Send a request** from anywhere with access to the `GameTcpClient`:

```csharp
var request = new YourRequest { /* fields */ };
tcpClient.SendMessage(MessageType.MsgYourRequest, request);
```

### Key Source Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Net/Mono/GameClient.cs` | MonoBehaviour entry point, Inspector buttons for connect/disconnect |
| `Assets/Scripts/Net/Proto/GameTcpClient.cs` | TCP client — connection, send, receive, frame parsing |
| `Assets/Scripts/Net/Proto/RingBufferStream.cs` | Async circular buffer between listener and parser |
| `Assets/Scripts/Net/Proto/MessageHandlerRegistry.cs` | Singleton message type → handler dispatch |
| `Assets/Scripts/Net/Proto/IMessageHandler.cs` | Handler interface: `IMessageHandler<T>` |
| `Assets/Scripts/Net/Proto/IMessageHandlerWrapper.cs` | Type-erasure wrapper for generic handlers |
| `Assets/Scripts/Net/Proto/BitConverterExtension.cs` | Big-endian byte conversion helpers |
| `Assets/Scripts/Util/Crc32.cs` | CRC32 checksum implementation |
| `Assets/Scripts/proto/` | Auto-generated protobuf C# files (do not edit) |

## Dependencies

| Package | Purpose |
|---------|---------|
| [UniTask](https://github.com/Cysharp/UniTask) | Allocation-free async/await for Unity |
| [Google.Protobuf](https://github.com/protocolbuffers/protobuf) | Protocol Buffers runtime (`Assets/Plugins/protobuf/`) |
| [Odin Inspector](https://odininspector.com/) | Enhanced Unity Inspector |
| [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/) | Asset management |
| [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@1.7/) | Cinematic sequencing |

## Project Structure

```
Assets/Scripts/
├── Game/               # App lifecycle, state machines, UI, camera
│   ├── StateMachine/   # Generic async state machine framework
│   ├── Login/          # Login flow states
│   └── UI/             # Window management (UIManager, GameWindowBase)
├── Net/
│   ├── Proto/          # TCP client, ring buffer, handler registry, extensions
│   ├── Tcp/            # Concrete message handlers
│   └── Mono/           # MonoBehaviour wrappers (GameClient)
├── proto/              # Generated protobuf C# files
└── Util/               # CRC32, logging, asset bundle utilities
```
