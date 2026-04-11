# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity game client (2022.3.12f1) focused on TCP networking with SSL/TLS, Protocol Buffers serialization, and state machine-driven architecture. Uses UniTask for async/await throughout.

## Code Style

Defined in `.editorconfig`:
- 4-space indentation for C#
- K&R brace style (opening brace on same line, `csharp_new_line_before_open_brace = none`)
- No separate lines for `else`, `catch`, `finally`

## Architecture

### Entry Point & Lifecycle

`GameLauncher` (Assets/Scripts/Game/GameLauncher.cs) initializes three core systems via `GetComponent`: `GameClient`, `UIManager`, `LoginStateMachineMono`. It starts the login state machine with `CancellationTokenOnDestroy` for async lifecycle.

### State Machine Framework

Generic state machine in Assets/Scripts/Game/StateMachine/:
- `State` base class with async `Start()` returning UniTask
- `StateMachineController` drives transitions; each state sets its successor via `SetNext()`
- CancellationToken propagated through all states

Two concrete state machines:
1. **Login flow** (Assets/Scripts/Game/Login/) - AppStart -> ShowLoadingUI -> ShowLoginUI -> ShowAppLoadingUI -> AppQuit. States auto-discovered via reflection in `LoginStateMachine.Init()`.
2. **TCP connection** (Assets/Scripts/Net/Proto/State/) - Init -> Connecting -> Connected -> Handshaking -> Ready -> Reconnecting -> Disconnecting -> Closed.

### Network Protocol

`GameTcpClient` (Assets/Scripts/Net/Proto/GameTcpClient.cs) implements the TCP client:
- **Wire format**: `[Magic 4B: 0xCAFEBABE] [Type 2B] [Length 4B] [Body varlen] [CRC32 4B]` — all big-endian
- Dual async loops: `ListenLoopAsync` reads SSL stream into `RingBufferStream`; `ParseLoopAsync` consumes and dispatches messages
- Connection retry with configurable timeout, delay, and max attempts

### Message Handler Registry

`MessageHandlerRegistry` (singleton) maps `MessageType` enum to `IMessageHandler<T>` implementations. Handlers are auto-registered via reflection in Editor mode; in builds, manual registration is required (see `RegisterAll()` TODO).

To add a new message type:
1. Generate protobuf C# code into Assets/Scripts/proto/
2. Create a handler implementing `IMessageHandler<T>` in Assets/Scripts/Net/Tcp/
3. Handler is auto-discovered in Editor; for builds, register manually in `GameTcpClient.RegisterAll()`

### UI System

`UIManager` manages window lifecycle with template-based instantiation, instance ID tracking, and parent-child hierarchies. Windows extend `GameWindowBase`.

### Asset Bundle System

`ABUnit` tracks individual bundle metadata (hash, CRC, dependencies). `ABDownloader` coordinates bulk downloads with progress tracking and manifest validation. CRC32 validation via `Crc32` utility.

## Key Dependencies

- **UniTask** (Cysharp) — async/await for Unity, replaces coroutines
- **Google.Protobuf** — serialization (runtime DLLs in Assets/Plugins/protobuf/)
- **Sirenix OdinInspector** — editor inspector customization (used extensively)
- **Addressables** 1.21.21 — asset management
- **Timeline** 1.7.7 — cinematic/animation

## Protobuf

Generated files live in Assets/Scripts/proto/ (namespace `GameServerServices.*`). These are auto-generated from `.proto` definitions in an external repo — do not edit by hand.

## Namespaces

- `Game` — launcher, state machines, UI
- `Game.Login` — login flow states
- `Game.UI` — UI windows
- `Net.Proto` — TCP client, message handlers, ring buffer, state machine
- `Net.Mono` — MonoBehaviour network wrappers
- `Net.Tcp` — concrete message handlers
- `GameServerServices.*` — generated protobuf types
