# Elite API

Elite API is the backend for the [Elite Farmers Website](https://github.com/EliteFarmers/Website) and the [Elite Discord Bot](https://github.com/EliteFarmers/Bot).

- Production API spec: https://api.elitebot.dev/
- Production API terms: https://elitebot.dev/apiterms

Do not build against `api.elitebot.dev` for your own projects without permission. Run your own local instance instead.

This project is not a Mojang API or Hypixel API proxy. It is not affiliated with Mojang or Hypixel.

## Requirements

- .NET 10 SDK
- Docker Desktop or Docker Engine
- A Discord application with a client ID, client secret, and bot token
  - [Create a Discord Application](https://discord.com/developers/applications)
- A Hypixel API key
  - [Get a Hypixel API Key](https://developer.hypixel.net/)
- Optional: S3 or Cloudflare R2 for image storage

This API uses [FastEndpoints](https://fast-endpoints.com/).

## Recommended Local Setup

For the easiest local development workflow, run Postgres, pgbouncer, and Redis in Docker. Then you can run the API from your IDE or `dotnet run`.

1. Start the local infrastructure:

   ```bash
   docker compose up -d
   ```

2. Copy `EliteAPI/appsettings.json` to `EliteAPI/appsettings.Development.json`.

3. Add your local secrets and overrides to `EliteAPI/appsettings.Development.json`. A minimal example:

   ```json
   {
     "Discord": {
       "ClientId": "<discord-client-id>",
       "ClientSecret": "<discord-client-secret>",
       "BotToken": "<discord-bot-token>"
     },
     "Hypixel": {
       "ApiKey": "<hypixel-api-key>"
     },
     "Jwt": {
       "Secret": "<local-jwt-secret>"
     },
     "WebsiteSecret": "<local-website-secret>"
   }
   ```

4. Validate the setup:

   ```bash
   dotnet run --project EliteAPI -- doctor
   ```

5. Run the API:

   ```bash
   dotnet run --project EliteAPI
   ```

6. Use these local URLs:

- API: `http://localhost:5164`
- OpenAPI: `http://localhost:5164/openapi/v1.json`
- Readiness: `http://localhost:5164/health/ready`

Notes for this setup:

- Postgres is available on `localhost:5436`
- Redis is available on `localhost:6380`
- `EliteAPI/.env` is not used
- Environment variables and user secrets still work, but `appsettings.Development.json` is the normal local override file

## Full Docker Setup

For production or just running everything in docker, do the following:

1. Copy `.env.example` to `.env`.

2. Fill in the required values in `.env`:

- `Discord__ClientId`
- `Discord__ClientSecret`
- `Discord__BotToken`
- `Hypixel__ApiKey`
(You can override other appsettings too, just use `__` in place of `:` seperators)

3. Start the full stack:

   ```bash
   docker compose --profile full-stack up -d
   ```

4. Use these local URLs:

- API: `http://localhost:7008`
- Readiness: `http://localhost:7008/health/ready`

5. Validate the running container when needed:

   ```bash
   docker compose --profile full-stack exec eliteapi dotnet EliteAPI.dll doctor
   ```

Optional observability stack:

```bash
docker compose --profile full-stack --profile observability up -d
```

## Website Setup

To run the Website against your local API, use the same Discord application in both repos and set these Website env vars:

```env
ELITE_API_URL=http://localhost:5164
PUBLIC_DISCORD_CLIENT_ID=<same value as Discord.ClientId>
ELITE_API_TOKEN=<same value as WebsiteSecret>
```

If the Website should talk to the Dockerized API instead, use:

```env
ELITE_API_URL=http://localhost:7008
```

Add the correct redirect URL to your Discord application:

```text
http://localhost:5173/login/callback
```

Basic smoke test:

1. Start the API.
2. Start the Website.
3. Log in with Discord.
4. Open a profile page.
5. Refresh the profile page.
6. Confirm the API still responds at `/openapi/v1.json` and `/health/ready`.

If you change API responses or add endpoints, regenerate the Website or Bot API types with the following command in the website repo.

```sh
pnpm run generate-api
```

## Supported Development Setups

These combinations are expected to work:

- Local API + local Website
- Full Docker API + local Website
- Full Docker API only

## Troubleshooting

- If you run the API locally from your IDE, use `localhost:5436` for Postgres and `localhost:6380` for Redis.
- If you run the API in Docker, use `pgbouncer:5432` for Postgres and `cache:6379` for Redis.
- If you change Docker config and nothing happens, restart the affected containers. You should not need to rebuild the image for normal config changes.
- If you edit `EliteAPI/.env` and nothing changes, that is expected. The local IDE flow uses `appsettings.Development.json`, environment variables, or user secrets.
- If Website login or profile refresh fails locally, verify `ELITE_API_URL`, `PUBLIC_DISCORD_CLIENT_ID`, `ELITE_API_TOKEN`, and the Discord callback URL.
- For extra request-level diagnostics during local Website debugging, set `SetupDiagnostics:LogProfileRequests=true`.
- Run `dotnet run --project EliteAPI -- doctor` to catch common config mistakes before startup.

## Contributing

- Keep changes relevant to the Website or Discord Bot.
- Follow the existing code style and project structure.
- Run tests before opening a PR when possible.
- Add tests for new behavior when practical.
- If a feature is large or changes core behavior, discuss it in an issue or in the community Discord first: https://elitebot.dev/support

## Database Changes

1. Make your entity and DTO changes.
2. Add or update the related `DbSet` in `EliteAPI/Data/DataContext.cs` if needed.
3. Generate an EF Core migration in `EliteAPI/Data/Migrations`.
4. Avoid stacking multiple migrations in the same PR unless there is a clear reason.
5. Migrations run automatically when the API starts in development.

## Local Admin Account

If you need admin features locally, set `Seed:AdminUserId` to your Discord user ID. This only works if there are no existing admin users and that account has already logged in once.

1. Set `Seed:AdminUserId` in `EliteAPI/appsettings.Development.json`, an environment variable, or user secrets.
2. Run the API and Website locally.
3. Log in with that Discord account.
4. Restart the API.
5. Use `/admin` on the Website.
