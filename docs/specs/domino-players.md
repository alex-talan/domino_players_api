# Domino Player API — Functional Specification

## 1. Objective

Implement a .NET Player API that participates in a four-player, two-team domino game orchestrated by the external Master API. After the mandatory double-six opening, the Greedy player must play the valid tile in its remaining hand with the highest total number of points. A configurable Random strategy is also supported.

For example, if both `(0,5)` and `(4,5)` can be played, the player must select `(4,5)` because its score is 9, compared with 5 for `(0,5)`.

Use the Clean Architecture conventions and development instructions provided by the repository template.

## 2. Scope and integration

The Master configures four Player API addresses, distributes all 28 tiles into four seven-tile hands, and starts the game when an external caller invokes its `POST /start` endpoint. The Player does not trigger the game or register itself in this version.

The Master calls the Player's `POST /play` endpoint with the current table, move histories, player identity, and remaining hand. Team0 consists of Player1 (`p0`) and Player3 (`p2`); Team1 consists of Player2 (`p1`) and Player4 (`p3`). Turn order is `p0 -> p1 -> p2 -> p3 -> p0`; the next player is the player to the right.

The Master always deals tile 27, `[6|6]`, to `p0`. The first player is always `p0`, and its first move must be tile 27.

The Master owns game state, validates responses, removes accepted tiles from the remaining hands, skips disqualified players, detects blocked games and finishes, determines winners, and calls `POST /end` on all players.

### 2.1 Team winner calculation (Master responsibility)

When a player empties their hand, that player's entire team wins.

When the table is blocked, identify the blocker as the player who placed the last tile, not a player who subsequently passed. Compare **only** the sum of remaining pips of that player and of the next player to their right. The lower pip total wins for that player's team; equal totals mean the blocker and their team win. Do not compare all four players, sum team hands, or award a shared win to both teams.

For example, if `p2` blocks with 10 pips and `p3` has 12, Team0 wins. If both have 10, Team0 still wins. If `p3` has 9, Team1 wins. The hands of `p0` and `p1` do not affect any of these comparisons. If `p3` blocks, compare against `p0`.

The Master sends `win: true` to both members of the winning team and `win: false` to both members of the losing team. The Player API does not have the other hands or sufficient chronological history to calculate this result itself. Changes to the Master's dealing and winner calculation must be implemented in the external Master application.

## 3. Shared tile catalogue

All players and the Master must use the following catalogue in exactly this order. Tile identifiers are zero-based indices, from 0 to 27. API payloads contain these identifiers, rather than pairs of numbers.

```csharp
public static readonly (int, int)[] Tiles =
[
    (0, 0), (0, 1), (0, 2), (0, 3), (0, 4), (0, 5), (0, 6),
    (1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6),
    (2, 2), (2, 3), (2, 4), (2, 5), (2, 6),
    (3, 3), (3, 4), (3, 5), (3, 6),
    (4, 4), (4, 5), (4, 6),
    (5, 5), (5, 6),
    (6, 6)
];
```

The points of a tile are the sum of its two numbers. Except for the mandatory `[6|6]` opening, doubles receive no special priority or bonus. Identifier 0 is a real tile, `(0,0)`; identifier -1 represents a pass.

## 4. Strategy

### 4.1 Determine valid tiles

Only identifiers in the current request's `your_tiles` may be selected. This field is the authoritative remaining hand.

- If the table is empty, only `p0` may play, and it must play tile 27, `[6|6]`, from its hand.
- If the table is nonempty, a tile can be played at `head` if either of its numbers equals `head`.
- A tile can be played at `tail` if either of its numbers equals `tail`.
- A tile matching neither end is invalid for that turn, regardless of its points.

The player uses the supplied `head` and `tail` values. It does not infer orientation from tile identifiers in `table`.

### 4.2 Select a move

1. If the table is empty, return tile 27 at `head` before applying either strategy. A missing double-six or a different `to_play` is an invalid request, not a pass.
2. Otherwise, find all valid tiles in the remaining hand.
3. Select the tile with the highest number of points.
4. If no tile is valid, pass by returning `tile: -1` and `position: ""`.
5. Otherwise, return the selected tile identifier and a valid position.

The following defaults complete the strategy:

- If several valid tiles have the same highest score, choose a tile randomly.
- If the selected tile can be played at both ends, choose the tile leading to a configuration where you have other compatible tiles. Example: Imagine the current configuration, where `head:5` and `tail:6`, and you can play the tile 26-(5,6). You can play it eather in `head` or in `tail`. But if you play it in **head**, the new configuration is `head:6` and `tail:6`. If you findout that you have no other tile with 6, then play it otherwise: in `tail`. 
- If the table is empty, choose `head`; the Master ignores the position for this first move.

The player must not pass while a valid tile exists. It does not draw tiles, look ahead, or change its choice based on opponents' histories.

## 5. API contract

JSON property names must match the Master's contract exactly, including `to_play` and `your_tiles`.

### 5.1 Play a turn — `POST /play`

First-turn request example:

```json
{
    "table": [],
    "head": null,
    "tail": null,
    "p0": [],
    "p1": [],
    "p2": [],
    "p3": [],
    "to_play": "p0",
    "your_tiles": [3, 12, 22, 0, 27, 19, 11]
}
```

| Field | Meaning |
|---|---|
| `table` | Played tile identifiers in table order. |
| `head` | Available number at the front; null for an empty table. |
| `tail` | Available number at the back; null for an empty table. |
| `p0`–`p3` | Each player's move history; -1 means a pass, and null means disqualification. |
| `to_play` | Current player identity: `p0`, `p1`, `p2`, or `p3`. |
| `your_tiles` | Current player's remaining available tile identifiers. Initially seven; fewer after accepted plays. |

For this example, the mandatory opening tile is identifier 27, `(6,6)`. Both strategies return HTTP 200:

```json
{
    "tile": 27,
    "position": "head"
}
```

The position is irrelevant on an empty table; return `head`.

Subsequent-turn request example:

```json
{
    "table": [27, 26, 16, 14],
    "head": 6,
    "tail": 3,
    "p0": [27],
    "p1": [26],
    "p2": [16],
    "p3": [14],
    "to_play": "p0",
    "your_tiles": [3, 22, 0, 19, 11, 12]
}
```

The valid tiles are identifier 3, `(0,3)`, worth three points; identifier 19, `(3,4)`, worth seven points; and identifier 12, `(1,6)`, worth seven points. Greedy may return either tile 19 at `tail` or tile 12 at `head`. One valid response is:

```json
{
    "tile": 19,
    "position": "tail"
}
```

If no tile in `your_tiles` matches either end, return HTTP 200:

```json
{
    "tile": -1,
    "position": ""
}
```

Never pass if a valid tile exists: the Master treats an unjustified pass as cheating and disqualifies the player. Return only the selected identifier and position; the Master handles tile orientation and table updates.

### 5.2 Request validation

- All core fields are required. `table` and `your_tiles` must be arrays.
- Optional `turn` is a positive integer (1 through 2147483647) assigned by the Master, starting at 1 for each game and incremented for every turn, including passes. Omitted or null values are accepted for backward compatibility. Invalid values return HTTP 400.
- Tile identifiers must be integers from 0 to 27. `table` and `your_tiles` must each contain no duplicates and must not overlap.
- `your_tiles` must contain one to seven identifiers for a playable turn. The Master should end the game immediately after a player plays its last tile, rather than request another turn with an empty hand.
- For an empty table, `head` and `tail` must both be null. Otherwise both must be integers from 0 to 6.
- For an empty table, `to_play` must be `p0` and `your_tiles` must contain tile 27. Reject violations with HTTP 400; do not select a different opening.
- Each history must be an array of integer entries from -1 to 27, or null.
- `to_play` must be one of `p0`, `p1`, `p2`, or `p3`; its corresponding history must not be null.

Return HTTP 400 for an invalid request using the repository's error-response conventions. An invalid request is not a legitimate pass. Other players' null histories are valid and do not affect the strategy.

The Player relies on the Master's supplied ends and hand; it does not reconstruct the hand from histories or determine whether other players cheated.

Move logs include `TURN:<number>` from the supplied `turn` field, or `TURN:unavailable` when absent or null. The Player does not infer turn numbers from tile counts or maintain a local counter. Retries preserve the supplied number. This diagnostic metadata does not affect move selection or the response. Compose output can arrive out of order across containers; these numbers identify turns within one game but do not prove acceptance or identify different games. Authoritative statistics remain the Master's responsibility.

### 5.3 Receive the result — `POST /end`

Request:

```json
{
    "win": true
}
```

Accept a required Boolean `win`, print a winning message, and return HTTP 200 with no response body. Return HTTP 400 for a malformed notification. Repeating the same notification must be harmless.

A true value means this player's team won. Both teammates receive true, including a partner with tiles remaining. A false value means the other team won. Finishing a hand wins for the entire team. Blocked games use the blocker-versus-next-player comparison in section 2.1, with ties awarded to the blocker, never a shared win across teams.

The Player does not calculate winners. The Master may also send an optional `your_tiles` array containing this player's authoritative final hand:

```json
{
  "win": false,
  "your_tiles": [0, 14, 19]
}
```

The final hand must contain zero to seven unique integer identifiers from 0 to 27. An empty array represents an empty final hand, including a win by playing the last tile. A blocked-game winner may have a nonempty final hand. Invalid final hands return HTTP 400. Omitted or null `your_tiles` is accepted for backward compatibility.

Each player logs its configured name, player ID, team ID, winning team, and `Final Tiles:[...]` from the supplied final hand. For example: `[Player3] [p2/Team0] Winning team: Team0. My team wins.` A false result for the same seat logs `Winning team: Team1. My team loses.` When the hand is omitted or null, it logs `Final Tiles: unavailable`. The Player must not estimate the final hand from a proposed move, since only the Master knows whether the move was accepted.

## 6. State and lifecycle

Move selection must be stateless: calculate each response exclusively from the current `/play` request and the shared tile catalogue. Do not cache or decrement a local hand. The Master removes accepted tiles and supplies the updated `your_tiles` on subsequent turns.

Restarting the Player does not require redistributing tiles because the next turn contains the required hand and table information.

No session identifier exists in this contract. A retained end result is diagnostic only and must not prevent future `/play` requests: those requests may belong to a new game. Correlating retained results across concurrent games is outside this version's scope.

If a player makes an invalid move or times out, the Master disqualifies it and stops requesting turns from it. The Player must perform selection promptly and avoid external service calls in the move-selection path. No numerical timeout is specified; the Master owns its timeout policy.


---

## 7. Implementation responsibilities

Use the .NET template's Clean Architecture conventions:

- **Domain:** fixed team assignments, tile catalogue, points calculation, opening rules, legal-move detection, and strategy selection, independent of HTTP.
- **Application:** process a turn from the supplied state and handle the result notification.
- **API:** expose `POST /play` and `POST /end`, validate payloads, and map responses and errors.
- **Infrastructure:** use the template's logging and any required diagnostic result storage. No persistent game-state store is required for move selection.

No `GET /start`, `POST /start`, or `POST /tiles` endpoint is required on the Player. The Master's start endpoint belongs to the Master application.

## 8. Acceptance criteria

| Scenario | Expected behavior |
|---|---|
| First-turn example above, either strategy | Return tile 27 at `head`, irrespective of hand order. |
| Empty table without tile 27, or `to_play` other than `p0` | Return HTTP 400 with validation details, not a pass. |
| Subsequent-turn example above, Greedy | Return tile 19 at `tail` or tile 12 at `head`. |
| Valid tiles 5 `(0,5)` and 23 `(4,5)` with `head=2`, `tail=5`; no higher-scoring valid tile | Return tile 23 at `tail`. |
| Highest-scoring hand tile matches neither end | Exclude it and choose the highest-scoring valid tile. |
| Selected tile matches only one end | Return that end. |
| Selected tile matches both ends | Choose the end leaving more remaining tiles playable; prefer `head` on a tie. |
| Valid identifiers 6 `(0,6)` and 11 `(1,5)` tie for highest score after the opening | Return either tile randomly. |
| No hand tile matches either end | Return `{"tile": -1, "position": ""}`. |
| Tile 0 `(0,0)` is the only valid tile | Return tile 0 rather than passing. |
| Next request supplies an updated hand | Use exactly that hand; never select an identifier missing from it. |
| Identical turn request is repeated | Return a legal response; random equal-score choices may differ. |
| Another player's history is null | Continue selecting normally. |
| Current player's history is null, or hand is empty | Return HTTP 400. |
| Invalid identifier, duplicate hand identifier, or malformed ends | Return HTTP 400. |
| End notification contains true or false | Accept and record or log the supplied result. |
| A teammate wins while this player still has tiles | Accept `win: true` and log the winning team without requiring an empty hand. |
| Blocked game, equal pip totals for blocker and next player | Master awards the blocker's team the win and notifies both teammates with true. |
| Blocked game, next player has fewer pips | Master awards that next player's team the win, ignoring the other two hands. |
| Valid turn arrives after an end notification | Process it from the supplied state. |

Test strategy behavior with domain unit tests and API contracts with the repository's existing API testing conventions.

## Player Strategy Selection

The Player API must support multiple playing strategies using the **Strategy design pattern**.

A common interface must define the behavior required to select the tile to play. Each strategy must provide its own implementation of this interface.

The strategy used by a Player API instance must be selected through configuration when the application is launched.

The application configuration must allow at least the following information to be provided:

- the **player strategy name**;
- the **Player API server address / listening URL**;
- the **player display name** and **seat ID** (`Player:Name` and `Player:Id`). IDs must be `p0` through `p3`; invalid configuration fails startup. The default ID is `p0`. Configure each instance to match the Master's seat assignment so end-result logs identify the correct team.

For example, it must be possible to launch several instances of the same Player API code with different configurations:

```text
Player 1
Id = p0, Team = Team0
Strategy = StrategyA
Address = http://localhost:5001

Player 2
Id = p1, Team = Team1
Strategy = StrategyB
Address = http://localhost:5002

Player 3
Id = p2, Team = Team0
Strategy = StrategyA
Address = http://localhost:5003

Player 4
Id = p3, Team = Team1
Strategy = StrategyB
Address = http://localhost:5004
```

The Player API implementation must therefore be independent from the selected strategy. The API is responsible for communication with the Master API, while the selected strategy is responsible only for deciding which tile to play. The Master owns game-state management. Team membership does not introduce additional selection heuristics.

### Required Strategies

Two player strategies must be implemented.

#### Strategy A

Strategy A corresponds to the playing algorithm already defined in this specification. This strategy is called **Greedy** strategy.

#### Strategy B

Strategy B is a simplified variant of Strategy A.

After the mandatory tile-27 opening, the strategy must iterate through the player's tiles in their current order and select the **first tile that is compatible with the current board state**.

The strategy must not try to optimize the move or compare several possible compatible tiles.

Conceptually:

```text
if table is empty:
    play tile 27 at head
    stop searching
for each tile in player's hand:
    if tile can be legally played:
        play this tile
        stop searching
```

This strategy is called **Random** strategy (because the order of tiles is already random).

If no tile can be legally played, the strategy must return the corresponding "pass / no playable tile" result defined by the Player API contract.

The purpose of having these two strategies is to allow several Player API instances to run from the same codebase while exhibiting different playing behavior.

---

## Docker Compose Deployment

The solution must support running multiple independent instances of the Player API using **Docker Compose**.

There must be only **one Player API codebase and one Player API Docker image**.

Docker Compose must instantiate multiple containers from this same Player API image. Each container must receive its player-specific configuration through environment variables or an equivalent configuration mechanism.

Docker Compose supports exactly two normal launch modes:

```text
Two local players: Player1/p0 and Player3/p2 (Team0)
Four local players: Team0 plus Player2/p1 and Player4/p3 (Team1)
```

The Master API is external and is not included in this Compose project. The two-player launch is not a two-seat game; the Master must still configure Team1 using external Player APIs.

`TEAM0_STRATEGY` selects the same strategy for both Team0 containers (default `StrategyA`). `TEAM1_STRATEGY` selects the same strategy for both Team1 containers (default `StrategyB`). Either accepts `StrategyA`/`Greedy` or `StrategyB`/`Random`, supplied through environment variables or a local `.env` file. Each container also receives its fixed `Player__Id`.

Start Team0:

```powershell
$env:TEAM0_STRATEGY = "StrategyA"
docker compose up --build
```

Start both teams:

```powershell
$env:TEAM0_STRATEGY = "StrategyA"
$env:TEAM1_STRATEGY = "StrategyB"
docker compose --profile four-players up --build
```

Player1/p0 listens on host port 5001; Player2/p1 on 5002; Player3/p2 on 5003; Player4/p3 on 5004. All four containers use the same image and are independent application instances.

The `four-players` profile groups Player2 and Player4 so the supported launches start either two teammates or both complete teams. Compose cannot prohibit explicitly targeting individual services; arbitrary one- or three-container starts are not supported launch workflows.

Before switching modes, run `docker compose --profile four-players down` to remove containers from the previous launch, including Team1 containers. Otherwise a four-to-two switch can leave Team1 running.

External players must follow the same HTTP contract, team assignments, mandatory opening, and result semantics, regardless of implementation language or hosting location.