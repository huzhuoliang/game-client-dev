# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity **2022.3.12f1** sandbox project that started as an AssetBundle loading experiment and has since accumulated:

- AssetBundle build pipeline + remote download/cache loader
- A TCP game-client networking stack (TLS + custom binary framing + Protobuf payloads)
- Two state-machine frameworks (a generic `Game.StateMachine` and a TCP-connection-specific FSM)
- A login flow that drives UI windows via the generic state machine

The project is built and run from the Unity Editor — there is no CLI test runner script in the repo.

## Code Style

Defined in `.editorconfig`:

- 4-space indentation for C#
- K&R brace style (opening brace on same line, `csharp_new_line_before_open_brace = none`)
- No separate lines for `else`, `catch`, `finally`

## Build / Run

- **Open the project**: launch Unity Hub and open this folder with Unity **2022.3.12f1** (other versions will trigger reimport churn).
- **Build all AssetBundles**: Unity menu → `AssetBundle/BuildAll` (defined in `Assets/Editor/AssetBundleTools.cs`). It wipes and rebuilds the `AssetBundles/` folder targeting `StandaloneWindows64` with `BuildAssetBundleOptions.None`.
- **AssetBundle output for runtime**: `Assets/StreamingAssets/StandaloneWindows64/` ships pre-built bundles + `AssetBundleInfo.json` used by `MainAB`.
- **Run gameplay**: open `Assets/Scenes/GameClient.unity` (the wired-up `GameLauncher` scene). Other scenes under `Assets/Scenes/` are isolated tests (`ABTestScene`, `ClientTest`, `UniTaskTest`, `TimelineTest`, etc.) — pick the one matching what you're working on.
- **Tests**: Unity Test Framework is installed (`com.unity.test-framework` 1.1.33) but there are no test assemblies in `Assets/`. Run via Unity → Window → General → Test Runner if/when tests get added.

## Protobuf Sync

C# proto stubs in `Assets/Scripts/proto/` are **generated on a Go server, not locally**. To refresh them:

```
Assets/Scripts/proto/scp_sync.bat
```

This pulls from `server:/home/huzhuoliang/go-workspace/game-server/proto_csharp/` via `scp`. Don't hand-edit files in `Assets/Scripts/proto/` (`Helloworld.cs`, `MessageType.cs`, `UserRegister.cs`, `ErrorCode.cs`) — they'll be overwritten on the next sync. Add new wire types on the server side and re-sync.

## Architecture

### Boot path

`GameLauncher` (`Assets/Scripts/Game/GameLauncher.cs`) is the entry point. It `[RequireComponent]`s three siblings on the same GameObject:

1. `GameClient` (Net.Mono) — owns a singleton `GameTcpClient`
2. `UIManager` (Game.UI) — instantiates/destroys windows from a template list
3. `LoginStateMachineMono` (Game.Login) — the FSM that drives the login flow

`Start()` wires these into `LoginStateMachine.Context` then calls `StartStateMachine(GetCancellationTokenOnDestroy())`. The FSM begins at `AppStartState` and chains forward via `SetNext<T>()`.

### State-machine frameworks (two of them — they are NOT interchangeable)

**`Game.StateMachine`** (`Assets/Scripts/Game/StateMachine/`, also has its own asmdef): a linear "next-state" runner. `StateMachineController.Start(initial, ct)` loops `state.Start() → state.GetNext()` until null. Used by the login flow. State subclasses live under `Game.Login.State` and call `SetNext<TNextState>()` from inside `StateStart`.

`LoginStateMachine.Init()` uses reflection to instantiate every concrete `LoginStateBase` subclass once and stores them in a dictionary, so `SetNext<T>()` is essentially a typed lookup against pre-built instances. New login states are auto-discovered — just subclass `LoginStateBase` and they will be registered.

**`Net.Proto.TcpClientStateMachine`** (`Assets/Scripts/Net/Proto/`): a separate FSM specifically for the TCP connection lifecycle. States live under `Net.Proto.State` (`Init → Connecting → Connected → Disconnecting → Disconnected → Closed`, with `Reconnecting` / `Handshaking` reserved as stubs for future use). Each state is a singleton cached in a static dictionary (`TcpClientStateBase.GetInstance<T>()`), operates on an `ITcpClientFSMCtx`, and `RunAsyncInternal` returns the next state (`null` ends the FSM). The FSM runs end-to-end through `Connected` (which owns the listen/parse loops via `UniTask.WhenAny`); `GameTcpClient` is still alive in parallel — `GameClient.cs` uses the old class, while `TcpClientStateMachineMono` runs the new FSM. Don't run both against the same server. Outstanding refactor work (deleting old `GameTcpClient`, designing the public `SendMessage` API, splitting `Connecting` vs `Reconnecting`) is tracked in `TODO.md`.

### Networking (`Assets/Scripts/Net/`)

- **Wire format**: `[Magic 4B = 0xCAFEBABE][Type 2B][Length 4B][Body N B][CRC32 4B]`, all big-endian. `MAX_BODY_SIZE = 1 MB`. Defined in `GameTcpClient.SendMessage` / `ParseOther`, and in the new FSM in `Connected.ParseOther` (parser side) + `TcpClientStateMachineMono.DebugSend` (temp send-side, replaced once the public `SendMessage` API lands).
- **Transport**: `TcpClient` + `SslStream` with `TargetHost = "localhost"` and a permissive cert validator (only accepts `SslPolicyErrors.None`, but the server's hostname is hardcoded to `localhost`).
- **Async model**: everything is **UniTask**, not `Task`. Two long-running loops run concurrently inside the `Connected` state: `ListenLoopAsync` (read socket → push into `RingBufferStream`) and `ParseLoopAsync` (consume frames → dispatch). `Connected.RunAsyncInternal` uses `UniTask.WhenAny` over both loops with a linked CTS; whichever loop dies first triggers the transition out. (Old `GameTcpClient` has the same loops as a parallel copy; will be removed in TODO #7.)
- **Handler dispatch**: implement `IMessageHandler<TProto>`; in editor builds, `RegisterAllByReflection()` walks `Assembly.GetExecutingAssembly().GetTypes()` and registers every concrete handler with `MessageHandlerRegistry`. **In non-editor builds, the reflection path is `#if`'d out — production builds need an explicit registration list (currently a `// TODO`)**. If you add a handler and it works in the editor but is missing on a player build, this is why.
- **Buffer reuse**: the parser rents body buffers from `ArrayPool<byte>.Shared`; payloads are deserialized via `CodedInputStream` with an explicit length so the pooled buffer can be larger than the message.
- **MessageType enum**: comes from the proto file; do NOT add new values by editing `MessageType.cs` directly (see Protobuf Sync above).
- **Connection error reporting**: `TcpClientFSMCtx.OnConnectFailed` fires per-attempt with a `ConnectErrorKind` (`SocketError` / `TlsAuthFailed` / `Unknown`); UI / upper layers should subscribe instead of catching exceptions out of states.

**To add a new message type**:

1. Generate protobuf C# code into `Assets/Scripts/proto/` (server-side, then `scp_sync.bat`).
2. Create a handler implementing `IMessageHandler<T>` in `Assets/Scripts/Net/Tcp/`.
3. Handler is auto-discovered in Editor; for builds, register manually in the production registration list (see TODO in `GameTcpClient.RegisterAll()`).

### AssetBundle layer (`Assets/Scripts/`, top-level)

`MainAB` represents one remote bundle root. It downloads `AssetBundleInfo.json` (manifest of bundle units) into a save path, then loads sub-bundles on demand via `SubABUnit` / `ABDownloader`. CRC32 validation runs through the `Crc32` utility. The Editor window `Assets/Editor/AssetBundleManagerWindow.cs` plus `AssetBundleWindowData` (a serialized `ScriptableObject` instance kept under `Assets/Editor/`) is the dev-time UI for browsing this state.

### UI (`Assets/Scripts/Game/UI/`)

`UIManager` (its own asmdef `GameUI`) instantiates windows from a serialized `windowTemplateList`. Each window inherits `GameWindowBase` and gets a unique `ulong WindowInstanceID`. Windows form a parent/child tree — hiding a parent recursively hides children (`HideWindow`). `ShowWindow<T>(parentWindowInstanceID)` is the typical call from a state.

### Assembly definitions

Most game code lives in the default `Assembly-CSharp` (no asmdef). The carve-outs are:

- `StateMachine` (generic FSM base) + `StateMachine.Editor`
- `GameUI` (UIManager + GameWindowBase)
- `CoroutineExtensions`

If you add cross-asmdef references, remember the Sirenix and Addressables modules in `Assets/Plugins/Sirenix/Odin Inspector/Modules/` have their own asmdefs and only the specific projects that depend on them should reference them.

## Conventions / Things to Know

- **Async**: prefer `UniTask` over `Task` everywhere in gameplay code. Coroutines are still used in the older `MainAB`/`SubABUnit` AssetBundle code path — don't rewrite those unless asked.
- **Inspector**: Sirenix Odin attributes (`[ShowInInspector]`, `[Button]`, `[LabelText]`, `[HorizontalGroup]`, etc.) are used pervasively; treat them as load-bearing for editor workflows, not decorative.
- **Logging in `GameTcpClient`**: state changes are emitted as `Debug.LogError` (`"============ ChangeState ..."`) — this is intentional debug noise, not an actual error. Don't "fix" it by downgrading to `LogFormat` without checking why.
- **Reflection-based registration**: both `LoginStateMachine.Init()` and `GameTcpClient.RegisterAllByReflection()` rely on `Assembly.GetExecutingAssembly().GetTypes()`. If state/handler classes ever move into a different assembly definition, the reflection scan won't see them.
- **`b1.b1_1` / `b1.b1_2` / `b1.b1_3`** under `StreamingAssets/StandaloneWindows64/` are checked-in pre-built bundles used by AssetBundle tests; deleting them will break those scenes.

## Key Dependencies

- **UniTask** (Cysharp) — async/await for Unity, replaces coroutines
- **Google.Protobuf** — serialization (runtime DLLs in `Assets/Plugins/protobuf/`)
- **Sirenix OdinInspector** — editor inspector customization (used extensively)
- **Addressables** 1.21.21 — asset management
- **Timeline** 1.7.7 — cinematic/animation

## Namespaces

- `Game` — launcher, state machines, UI
- `Game.Login` — login flow states
- `Game.UI` — UI windows
- `Net.Proto` — TCP client, message handlers, ring buffer, state machine
- `Net.Mono` — MonoBehaviour network wrappers
- `Net.Tcp` — concrete message handlers
- `GameServerServices.*` — generated protobuf types
