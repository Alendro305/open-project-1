# Online Mechanics — Rooms, Trading, Farming

Built on the SCV + Zenject + R3 foundation (see [README](README.md)). Real-time uses **SignalR**
(one `GameHub` connection, Newtonsoft protocol). The backend (`Backend/ChopChop.Api`) is authoritative
and persists every state + a full audit trail in SQLite.

## Transport

- **REST** (`HttpApiClient`) for commands/queries.
- **SignalR** (`RealtimeService` → `IRealtimeService`) for live pushes, surfaced as R3 observables
  already marshalled to the main thread (`ObserveOnMainThread`). The JWT is forwarded via
  `AccessTokenProvider` (and accepted by the server from the `?access_token=` query for WebSockets).

## Services (app-wide, ProjectContext)

| Service | REST | Realtime in | Reactive state |
|---------|------|-------------|----------------|
| `IRealtimeService` | — | `RoomUpdated`, `PlotsUpdated`, `TradeUpdated`, `TradeInvited` | `IsConnected` |
| `IRoomService` | `/api/rooms*` | `RoomUpdated` | `CurrentRoom` |
| `ITradeService` | `/api/trades*` | `TradeUpdated`, `TradeInvited` | `CurrentTrade`, `Invitations` |
| `IFarmService` | `/api/farm*` | `PlotsUpdated` | `Plots` |

## SCV screens

- **Rooms** (complete worked example): `RoomController` + `RoomView` (+ `RoomRowView`). Lobby list,
  create/join/leave; binds `CurrentRoom` presence live. `RoomScreenInstaller` on the lobby SceneContext.
- **Trading**: `TradeController` — projects the server trade into my-side / their-side, edits the local
  offer, confirm/cancel; auto-adopts invitations. *(View pending — prefab-bound.)*
- **Farming**: `FarmController` — exposes room `Plots` + seed catalog, place/plant/water/harvest/remove
  commands; auto-refreshes on entering a room. *(World-space view pending.)*

## How each mechanic works

**Rooms** — logical lobby/session (membership + presence, no transform netcode). Join via REST, then
`JoinRoomGroup` on the hub for presence + room/farm broadcasts. Host reassigns on leave; room closes
when empty.

**Trading** — server-authoritative state machine (`Active → Completed | Cancelled`). Each side stakes
coins + items and confirms; any offer edit clears both confirmations. When both confirm, the server
validates ownership and swaps everything in a single DB transaction. Pushed to both parties live.

**Farming** — owner-private plots placed in a room's world. `plant` consumes a seed; `water` starts an
**offline, timestamp-based** grow timer (`GrowthStartUtc + GrowSeconds`); state is recomputed from the
clock on read, so crops finish while you're away. `harvest` (the "spade") yields the plant to your
inventory. Grow times live server-side in `SeedCatalog` (tamper-resistant).

## Inventory & persistence

- Item ownership lives in `InventoryItems` (itemId + quantity, itemId matches the client ItemSO catalog);
  currency stays on `PlayerProfile.Coins`.
- Every transition (room join/leave, trade offer/confirm/complete, plot plant/water/harvest, inventory
  add/remove) is appended to the `EventLog` audit table → "store all previous states".

## Remaining Editor / dependency steps

1. **NuGetForUnity restore**: `Microsoft.AspNetCore.SignalR.Client` +
   `…SignalR.Protocols.NewtonsoftJson` were added to `Assets/packages.config`; open the NuGetForUnity
   window (or let it auto-restore) to download the closure. `System.Text.Json` / `System.Threading.Channels`
   are already present.
2. **Wire scenes/prefabs**: lobby Canvas + `RoomView` + `RoomScreenInstaller` on a SceneContext; room
   row prefab. Trade/Farm views once their prefabs exist.
3. Bind `TradeController` / `FarmController` in their screen installers like `RoomScreenInstaller`.
