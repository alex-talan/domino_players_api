# Master Domino Player API

A stateless ASP.NET Core player for the Master Domino game. The Master owns the board, hands, turn order, validation of moves, and winner calculation. This service chooses a move from the current request and reports the game result it receives.

## Run Locally

The solution targets .NET 10. Start the API with the default Greedy strategy:

```powershell
dotnet run --project src/WebApi/WebApi.csproj
```

Configure an instance with `Player__Name`, `Player__Strategy`, and `ASPNETCORE_URLS`. Supported strategy values are `Greedy` or `StrategyA`, and `Random` or `StrategyB`.

```powershell
$env:Player__Name = "Player1"
$env:Player__Strategy = "StrategyA"
$env:ASPNETCORE_URLS = "http://localhost:5001"
dotnet run --no-launch-profile --project src/WebApi/WebApi.csproj
```

The player does not register with the Master or start games. Configure its listening address in the Master, which calls the endpoints below.

## Docker Compose

Build one Player API image and start three independent player instances:

```powershell
docker compose up --build
```

The instances listen on `http://localhost:5001`, `http://localhost:5002`, and `http://localhost:5003`. Players 1 and 2 use Strategy A; Player 3 uses Strategy B. The Master API is intentionally external to this repository and can call these addresses. A fourth independently implemented player can register with the Master without running another local Player API container.

## Strategies

- **Greedy / StrategyA** selects a playable tile with the highest pip total. Equal-score choices are random. When the selected tile fits both ends, it chooses the end that leaves more of its remaining hand playable, preferring `head` on a tie.
- **Random / StrategyB** selects the first playable tile in the supplied hand order.

The shared tile catalogue contains IDs `0` through `27`; ID `0` is the real `(0,0)` tile. Both strategies pass with tile `-1` only when no hand tile is playable.

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

### `POST /end`

Accepts `{"win":true}` or `{"win":false}` and returns HTTP 200 with an empty body. The supplied result is written to structured logs; it does not affect later turns.

To log each player's exact final hand, the Master must include that player's remaining tile identifiers in an optional `your_tiles` field:

```json
{"win":false,"your_tiles":[0,14,19]}
```

The final hand may contain zero to seven unique IDs from 0 to 27. Send `[]` when the hand is empty, including when a player wins by playing its last tile. A blocked-game winner can still have remaining tiles. Invalid hands return HTTP 400. Omitted or null `your_tiles` remains valid for compatibility, but the Player cannot determine the exact final hand from `win` alone.

## Player Logs

Each valid `/play` request logs the supplied table, remaining hand, and selected move using the configured `Player__Name`. Tile values are catalogue identifiers, not pip pairs:

```text
[Player1] TURN:26 - TABLE:[22, 4, 2] - My Tiles:[14, 19] - My Move:(19, head)
```

The Master should include optional `turn` in each `/play` request: a positive integer starting at 1 for each game, incremented for every turn including passes. Players log that number unchanged; they do not generate counters or infer turns from the table. Omitted or null `turn` logs `TURN:unavailable` for backward compatibility. Non-positive or malformed numbers return HTTP 400. Repeated requests retain the supplied number.

For example, add `"turn":26` alongside `table`, `head`, `tail`, histories, `to_play`, and `your_tiles`. This field does not change the response or strategy.

Docker Compose may display different containers' log lines out of order. Parsers can order numbered moves within a single game by `TURN`, but Player logs still describe proposed moves, not Master acceptance. Turn numbers restart between games, so multiple games require a Master game identifier and authoritative accepted-turn events for reliable statistics.

A pass is logged as `My Move:(-1, )`. When `/end` receives `win: true`, the player logs:

```text
[Player1] I win!!
```

A false result logs `[Player1] I did not win.` Console output is single-line text with the standard logging prefix; Docker Compose also prefixes each line with its container name.

Each end notification also logs the supplied final hand:

```text
[Player1] Final Tiles:[0, 14, 19]
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
docker compose down
```

If you detach instead, run `docker compose down` from another terminal in the project directory to stop and remove everything.