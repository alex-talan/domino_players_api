# Domino Player API — Functional Specification

## 1. Objective

Implement a .NET Player API that participates in a four-player domino game orchestrated by the Master API. On each turn, the player must play the valid tile in its remaining hand with the highest total number of points.

For example, if both `(0,5)` and `(4,5)` can be played, the player must select `(4,5)` because its score is 9, compared with 5 for `(0,5)`.

Use the Clean Architecture conventions and development instructions provided by the repository template.

## 2. Scope and integration

The Master configures four Player API addresses, distributes all 28 tiles into four seven-tile hands, and starts the game when an external caller invokes its `POST /start` endpoint. The Player does not trigger the game or register itself in this version.

The Master calls the Player's `POST /play` endpoint with the current table, move histories, player identity, and remaining hand. The first player is always `p0`.

The Master owns game state, validates responses, removes accepted tiles from the remaining hands, skips disqualified players, detects blocked games and finishes, determines winners, and calls `POST /end` on all players.

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

The points of a tile are the sum of its two numbers. Doubles receive no special priority or bonus. Identifier 0 is a real tile, `(0,0)`; identifier -1 represents a pass.

## 4. Strategy

### 4.1 Determine valid tiles

Only identifiers in the current request's `your_tiles` may be selected. This field is the authoritative remaining hand.

- If the table is empty, every tile in the hand is valid.
- If the table is nonempty, a tile can be played at `head` if either of its numbers equals `head`.
- A tile can be played at `tail` if either of its numbers equals `tail`.
- A tile matching neither end is invalid for that turn, regardless of its points.

The player uses the supplied `head` and `tail` values. It does not infer orientation from tile identifiers in `table`.

### 4.2 Select a move

1. Find all valid tiles in the remaining hand.
2. Select the tile with the highest number of points.
3. If no tile is valid, pass by returning `tile: -1` and `position: ""`.
4. Otherwise, return the selected tile identifier and a valid position.

The following deterministic defaults complete the strategy:

- If several valid tiles have the same highest score, choose a tile randmly.
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
    "your_tiles": [3, 14, 22, 0, 7, 19, 11]
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

For this example, the highest-scoring tile is identifier 22, `(4,4)`, with eight points. Return HTTP 200:

```json
{
    "tile": 22,
    "position": "head"
}
```

The position is irrelevant on an empty table; `head` follows the deterministic default.

Subsequent-turn request example:

```json
{
    "table": [22, 4, 2],
    "head": 4,
    "tail": 2,
    "p0": [22],
    "p1": [4],
    "p2": [-1],
    "p3": [2],
    "to_play": "p0",
    "your_tiles": [3, 14, 0, 7, 19, 11]
}
```

The valid tiles are identifier 14, `(2,3)`, worth five points, and identifier 19, `(3,4)`, worth seven points. Return:

```json
{
    "tile": 19,
    "position": "head"
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

- All documented fields are required. `table` and `your_tiles` must be arrays.
- Tile identifiers must be integers from 0 to 27. `table` and `your_tiles` must each contain no duplicates and must not overlap.
- `your_tiles` must contain one to seven identifiers for a playable turn. The Master should end the game immediately after a player plays its last tile, rather than request another turn with an empty hand.
- For an empty table, `head` and `tail` must both be null. Otherwise both must be integers from 0 to 6.
- Each history must be an array of integer entries from -1 to 27, or null.
- `to_play` must be one of `p0`, `p1`, `p2`, or `p3`; its corresponding history must not be null.

Return HTTP 400 for an invalid request using the repository's error-response conventions. An invalid request is not a legitimate pass. Other players' null histories are valid and do not affect the strategy.

The Player relies on the Master's supplied ends and hand; it does not reconstruct the hand from histories or determine whether other players cheated.

### 5.3 Receive the result — `POST /end`

Request:

```json
{
    "win": true
}
```

Accept a required Boolean `win`, print a winning message, and return HTTP 200 with no response body. Return HTTP 400 for a malformed notification. Repeating the same notification must be harmless.

A true value means this player is a winner, including a shared win when several eligible players tie for the lowest remaining score in a blocked game. A false value means this player is not a winner. When a player plays its last tile, the Master declares that player the sole winner.

The Player does not calculate winners. The Master may also send an optional `your_tiles` array containing this player's authoritative final hand:

```json
{
  "win": false,
  "your_tiles": [0, 14, 19]
}
```

The final hand must contain zero to seven unique integer identifiers from 0 to 27. An empty array represents an empty final hand, including a win by playing the last tile. A blocked-game winner may have a nonempty final hand. Invalid final hands return HTTP 400. Omitted or null `your_tiles` is accepted for backward compatibility.

Each player logs its outcome and `Final Tiles:[...]` from the supplied final hand. When the hand is omitted or null, it logs `Final Tiles: unavailable`. The Player must not estimate the final hand from a proposed move, since only the Master knows whether the move was accepted.

## 6. State and lifecycle

Move selection must be stateless: calculate each response exclusively from the current `/play` request and the shared tile catalogue. Do not cache or decrement a local hand. The Master removes accepted tiles and supplies the updated `your_tiles` on subsequent turns.

Restarting the Player does not require redistributing tiles because the next turn contains the required hand and table information.

No session identifier exists in this contract. A retained end result is diagnostic only and must not prevent future `/play` requests: those requests may belong to a new game. Correlating retained results across concurrent games is outside this version's scope.

If a player makes an invalid move or times out, the Master disqualifies it and stops requesting turns from it. The Player must perform selection promptly and avoid external service calls in the move-selection path. No numerical timeout is specified; the Master owns its timeout policy.


---

## 7. Implementation responsibilities

Use the .NET template's Clean Architecture conventions:

- **Domain:** tile catalogue, points calculation, legal-move detection, and deterministic selection, independent of HTTP.
- **Application:** process a turn from the supplied state and handle the result notification.
- **API:** expose `POST /play` and `POST /end`, validate payloads, and map responses and errors.
- **Infrastructure:** use the template's logging and any required diagnostic result storage. No persistent game-state store is required for move selection.

No `GET /start`, `POST /start`, or `POST /tiles` endpoint is required on the Player. The Master's start endpoint belongs to the Master application.

## 8. Acceptance criteria

| Scenario | Expected behavior |
|---|---|
| First-turn example above | Return tile 22 at `head`. |
| Subsequent-turn example above | Return tile 19 at `head`. |
| Valid tiles 5 `(0,5)` and 23 `(4,5)` with `head=2`, `tail=5`; no higher-scoring valid tile | Return tile 23 at `tail`. |
| Highest-scoring hand tile matches neither end | Exclude it and choose the highest-scoring valid tile. |
| Selected tile matches only one end | Return that end. |
| Selected tile matches both ends | Return `head`. |
| Valid identifiers 6 `(0,6)` and 11 `(1,5)` tie for highest score | Return tile 6. |
| No hand tile matches either end | Return `{"tile": -1, "position": ""}`. |
| Tile 0 `(0,0)` is the only valid tile | Return tile 0 rather than passing. |
| Next request supplies an updated hand | Use exactly that hand; never select an identifier missing from it. |
| Identical turn request is repeated | Return the same response. |
| Another player's history is null | Continue selecting normally. |
| Current player's history is null, or hand is empty | Return HTTP 400. |
| Invalid identifier, duplicate hand identifier, or malformed ends | Return HTTP 400. |
| End notification contains true or false | Accept and record or log the supplied result. |
| Valid turn arrives after an end notification | Process it from the supplied state. |

Test strategy behavior with domain unit tests and API contracts with the repository's existing API testing conventions.

## Player Strategy Selection

The Player API must support multiple playing strategies using the **Strategy design pattern**.

A common interface must define the behavior required to select the tile to play. Each strategy must provide its own implementation of this interface.

The strategy used by a Player API instance must be selected through configuration when the application is launched.

The application configuration must allow at least the following information to be provided:

- the **player strategy name**;
- the **Player API server address / listening URL**;
- the **player name or identifier**, if required by the Master API registration mechanism.

For example, it must be possible to launch several instances of the same Player API code with different configurations:

```text
Player 1
Strategy = StrategyA
Address = http://localhost:5001

Player 2
Strategy = StrategyA
Address = http://localhost:5002

Player 3
Strategy = StrategyB
Address = http://localhost:5003

Player 4
Strategy = StrategyB
Address = http://localhost:5004
```

The Player API implementation must therefore be independent from the selected strategy. The API is responsible for communication with the Master API and game-state management, while the selected strategy is responsible only for deciding which tile to play.

### Required Strategies

Two player strategies must be implemented.

#### Strategy A

Strategy A corresponds to the playing algorithm already defined in this specification. This strategy is called **Greedy** strategy.

#### Strategy B

Strategy B is a simplified variant of Strategy A.

When the player must play a tile, the strategy must iterate through the player's tiles in their current order and select the **first tile that is compatible with the current board state**.

The strategy must not try to optimize the move or compare several possible compatible tiles.

Conceptually:

```text
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

For the initial Master API integration tests, Docker Compose must allow the following environment to be started:

```text
Master API

Player 1 → Strategy A
Player 2 → Strategy A
Player 3 → Strategy B
Player 4 → Strategy B
```

The four Player APIs must be independent application instances even when two of them use the same strategy.

A conceptual Docker Compose configuration is:

```yaml
services:

  master:
    # Master API

  player1:
    # Player API
    environment:
      Player__Name: Player1
      Player__Strategy: StrategyA

  player2:
    # Player API
    environment:
      Player__Name: Player2
      Player__Strategy: StrategyA

  player3:
    # Player API
    environment:
      Player__Name: Player3
      Player__Strategy: StrategyB

  player4:
    # Player API
    environment:
      Player__Name: Player4
      Player__Strategy: StrategyB
```

The exact ports, container names, environment variable names, and network configuration may be adapted to the implementation.

The Docker Compose configuration must allow the complete local test environment to be started with a single command, for example:

```bash
docker compose up --build
```

### Support for an External Fourth Player

The architecture must not require all four players to be instances of the Player API implemented in this project.

The final target scenario is:

```text
Master API
    |
    +-- Player 1 → local Player API → Strategy A
    +-- Player 2 → local Player API → Strategy A
    +-- Player 3 → local Player API → Strategy B
    +-- Player 4 → external Player API implemented independently
```

Therefore, it must be possible to start the Master API and only **three local Player API containers** through Docker Compose.

A fourth player may then register with the Master API from an independently implemented Player API, potentially running:

- in another Docker container;
- as a local process;
- on another machine;
- or on another server.

The Master API must interact with players exclusively through the Player API contract defined by the specifications. It must not depend on the internal implementation of a player, its strategy implementation, its programming language, or whether the player is part of the same Docker Compose environment.

This allows another developer to implement the fourth player independently, provided that their application respects the Player API protocol expected by the Master API.