# ChopChop Online (Unity client)

Online layer for ChopChop, built on **Zenject** (DI) + **R3** (reactive) and the **SCV** pattern.
Talks to the `Backend/ChopChop.Api` ASP.NET server.

## SCV — Service / Controller / View

| Layer | Type | Responsibility | Knows about |
|-------|------|----------------|-------------|
| **Service** | plain C# (`IService`) | business logic & side effects: networking, persistence, audio… | nothing UI |
| **Controller** | plain C# (`ScvController`) | reactive **state** (`ReactiveProperty`) + **intent** (`ReactiveCommand`); orchestrates services | services only |
| **View** | MonoBehaviour (`ScvView<T>`) | binds visual elements ↔ controller members | its controller only |

The controller is a non-MonoBehaviour created by Zenject (`IInitializable`/`IDisposable` lifecycle).
The view is injected with its controller and binds in `Bind()`. Subscriptions are tied to `Bag`
(view) or `Disposables` (controller) and disposed automatically.

## Folders

```
Scripts/Online/
  Core/          IService, ScvController, ScvView<T>     (pattern primitives)
  Config/        BackendConfigSO                          (designer config)
  Networking/    IApiClient, HttpApiClient, ApiResult, ITokenProvider
  Auth/          ISessionService/SessionService, IAuthService/AuthService, DTOs
  UI/Auth/       AuthController, AuthView                 (first SCV screen)
  Installers/    OnlineInstaller (project), AuthScreenInstaller (scene)
```

## One-time Editor setup

1. **Backend config asset** — `Create → ChopChop → Online → Backend Config`.
   Set *Base Url* to the server (default `http://localhost:5080`).

2. **ProjectContext** (app-wide DI root) —
   `Assets → Create → Zenject → Project Context Prefab` (creates `Assets/Resources/ProjectContext.prefab`).
   Add the **OnlineInstaller** component to it, assign the BackendConfig asset, and add the
   installer to the context's *Mono Installers* list. These services now live for the whole session.

3. **Auth screen** —
   - Build a Canvas panel with: email `TMP_InputField`, password `TMP_InputField` (Content Type =
     *Password*), display-name `TMP_InputField` (+ a group GameObject), a submit `Button` (+ label),
     a toggle-mode `Button` (+ label), a status `TMP_Text`, and a busy-indicator GameObject.
   - Add **AuthView** to the panel root and wire all serialized fields.
   - Add a **SceneContext** to the scene (`Create → Zenject → Scene Context`), add an
     **AuthScreenInstaller** component, and register it in the context's *Mono Installers*.
   - On play, the SceneContext injects `AuthView`, builds & initializes `AuthController`, and the
     bindings come alive.

> Tip: subscribe to `AuthController.SignedIn` (or `ISessionService.Status`) from your menu flow to
> route the player into the game once authenticated.

## Threading

`HttpClient` continuations resume on Unity's main thread (the UnitySynchronizationContext is
installed there), so controllers can touch `ReactiveProperty` values straight after `await`.

## Extending the pattern

`IService` is general, not UI-only. To migrate, say, audio: define `IAudioService` wrapping the
existing `AudioManager`, bind it in an installer, and inject it into controllers/services instead of
referencing the manager directly. New screens follow the same Controller + View + (existing)
services recipe.
