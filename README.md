# Master Domino Player API

A stateless ASP.NET Core player for the Master Domino game. The Master owns the board, hands, turn order, validation of moves, and winner calculation. This service chooses a move from the current request and reports the game result it receives.

## Team Rules

- **Team0:** Player1 (`p0`) and Player3 (`p2`).
- **Team1:** Player2 (`p1`) and Player4 (`p3`).
- The Master always deals `[6|6]` (tile ID `27`) to `p0`. Player1 must open with that tile, regardless of strategy or hand order.
- When a player empties their hand, their team wins, including their partner.
- When the table is blocked, the Master compares only the remaining pip totals of the blocker (the last player to place a tile) and the next player to their right. The lower total wins for that player's team; a tie goes to the blocker and their team. Neither partner's hand is included in the comparison.
- Turn order is `p0 -> p1 -> p2 -> p3 -> p0`. The Master must send `win: true` to both winning teammates and `win: false` to both losing teammates.

The Master application is external to this repository. Its dealing, blocked-game comparison, and team-result notifications must follow these rules; the Player cannot determine the winner from its own hand alone.

## Run Locally

The solution targets .NET 10. Start the API with the default Greedy strategy:

```powershell
dotnet run --project src/WebApi/WebApi.csproj
```

Configure an instance with `Player__Name`, `Player__Id`, `Player__Strategy`, and `ASPNETCORE_URLS`. `Player__Id` must be `p0`, `p1`, `p2`, or `p3` and defaults to `p0`. It determines the team identity used in logs; configure it to match the seat assigned by the Master. Unsupported IDs fail startup. Supported strategy values are `Greedy` or `StrategyA`, and `Random` or `StrategyB`.

```powershell
$env:Player__Name = "Player1"
$env:Player__Id = "p0"
$env:Player__Strategy = "StrategyA"
$env:ASPNETCORE_URLS = "http://localhost:5001"
dotnet run --no-launch-profile --project src/WebApi/WebApi.csproj
```

The player does not register with the Master or start games. Configure its listening address in the Master, which calls the endpoints below.

## Docker Compose

Build one Player API image and start **two players in Team0**, using the same selectable strategy:

```powershell
$env:TEAM0_STRATEGY = "StrategyA"
docker compose up --build
```

Start **four players in two teams**, choosing one shared strategy per team:

```powershell
$env:TEAM0_STRATEGY = "StrategyA"
$env:TEAM1_STRATEGY = "StrategyB"
docker compose --profile four-players up --build
```

Use `StrategyA`/`Greedy` or `StrategyB`/`Random` for either variable. Defaults are StrategyA for Team0 and StrategyB for Team1. Compose also reads these variables from a local `.env` file.

| Service | Player ID | Team | Host address | Strategy setting |
|---|---|---|---|---|
| player1 | p0 | Team0 | http://localhost:5001 | TEAM0_STRATEGY |
| player3 | p2 | Team0 | http://localhost:5003 | TEAM0_STRATEGY |
| player2 | p1 | Team1 | http://localhost:5002 | TEAM1_STRATEGY |
| player4 | p3 | Team1 | http://localhost:5004 | TEAM1_STRATEGY |

These are the two supported launch modes: the default starts Team0, and the `four-players` profile adds both Team1 members together. The two-player mode is two **local teammates**, not a two-seat game: the external Master must still supply all four seats, with Team1 hosted elsewhere. The Master is not part of this Compose project.

Before switching modes, stop the previous set with `docker compose --profile four-players down`; otherwise previously started Team1 containers can remain running. Compose profiles group normal launches, but cannot prevent an operator from explicitly targeting individual services.

## Strategies

- **Greedy / StrategyA** selects a playable tile with the highest pip total. Equal-score choices are random. When the selected tile fits both ends, it chooses the end that leaves more of its remaining hand playable, preferring `head` on a tie.
- **Random / StrategyB** selects the first playable tile in the supplied hand order.

The shared tile catalogue contains IDs `0` through `27`; ID `0` is the real `(0,0)` tile. Both strategies override normal selection on an empty table and play ID `27`, `[6|6]`, at `head`. After the opening, both pass with tile `-1` only when no hand tile is playable. Team membership does not otherwise change the selection heuristics.

## HTTP Contract

### `POST /play`

The request includes `table`, nullable `head` and `tail`, histories `p0` through `p3`, `to_play`, and the authoritative `your_tiles` hand. Property names match the Master contract, including `to_play` and `your_tiles`.

Successful responses are HTTP 200:

```json
{"tile":19,"position":"head"}
```

When no tile is playable:

```json
{"tile":-1,"position":""}
```

Invalid requests return HTTP 400 with `ValidationProblemDetails`.

An empty table requires `to_play: "p0"` and tile `27` in `your_tiles`; otherwise the request returns HTTP 400 rather than a pass or a different opening tile.

### `POST /end`

Accepts `{"win":true}` or `{"win":false}` and returns HTTP 200 with an empty body. `win` is the **team result**, not whether this individual played the deciding tile. Both winning teammates receive true, including a partner with tiles remaining. Both losing teammates receive false. The supplied result is written to structured logs; it does not affect later turns.

To log each player's exact final hand, the Master must include that player's remaining tile identifiers in an optional `your_tiles` field:

```json
{"win":false,"your_tiles":[0,14,19]}
```

The final hand may contain zero to seven unique IDs from 0 to 27. Send `[]` when the hand is empty, including when a player wins by playing its last tile. A blocked-game winner can still have remaining tiles. Invalid hands return HTTP 400. Omitted or null `your_tiles` remains valid for compatibility, but the Player cannot determine the exact final hand from `win` alone.

## Player Logs

Each valid `/play` request logs the supplied table, remaining hand, and selected move using the configured `Player__Name`. Tile values are catalogue identifiers, not pip pairs:

```text
[Player1] [p0/Team0] TURN:5 - TABLE:[27, 26, 16, 14] - My Tiles:[3, 22, 0, 19, 11, 12] - My Move:(19, tail)
```

The Master should include optional `turn` in each `/play` request: a positive integer starting at 1 for each game, incremented for every turn including passes. Players log that number unchanged; they do not generate counters or infer turns from the table. Omitted or null `turn` logs `TURN:unavailable` for backward compatibility. Non-positive or malformed numbers return HTTP 400. Repeated requests retain the supplied number.

For example, add `"turn":26` alongside `table`, `head`, `tail`, histories, `to_play`, and `your_tiles`. This field does not change the response or strategy.

Docker Compose may display different containers' log lines out of order. Parsers can order numbered moves within a single game by `TURN`, but Player logs still describe proposed moves, not Master acceptance. Turn numbers restart between games, so multiple games require a Master game identifier and authoritative accepted-turn events for reliable statistics.

A pass is logged as `My Move:(-1, )`. When `/end` receives `win: true`, the player logs:

```text
[Player1] [p0/Team0] Winning team: Team0. My team wins.
```

A false result for Player1 logs `[Player1] [p0/Team0] Winning team: Team1. My team loses.` For Team1 players, true identifies Team1 as the winner and false identifies Team0. The winning team is derived from the configured seat and the Master's team-result Boolean, never from the final hand. Console output is single-line text with the standard logging prefix; Docker Compose also prefixes each line with its container name.

Each end notification also logs the supplied final hand:

```text
[Player1] [p0/Team0] Final Tiles:[0, 14, 19]
```

An empty hand logs `Final Tiles:[]`. An omitted or null final hand logs `Final Tiles: unavailable`; the Player never estimates it by assuming its last proposed move was accepted.

## Verify

```powershell
dotnet test
```

# Troubleshooting 

## WSL Docker socket permissions issue
```bash
$ docker compose up --build
permission denied while trying to connect to the docker API at unix:///var/run/docker.sock
```
**Solution**:
```bash
sudo usermod -aG docker "$USER"
newgrp docker
docker info
docker compose up --build
```

# Stop  the application

Press **Ctrl+C** in that terminal to stop all players.

To also remove the stopped containers and Compose network, run:

```bash
docker compose --profile four-players down
```

If you detach instead, run `docker compose --profile four-players down` from another terminal in the project directory to stop and remove both teams.