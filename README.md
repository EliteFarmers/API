<h1 align="center">Elite API</h1>
<hr>

This is the backend API for the [Elite Farmers Website](https://github.com/EliteFarmers/Website) and the [Elite Discord Bot](https://github.com/EliteFarmers/API).

**API Spec:** https://api.elitebot.dev/ <br>
**API TOS:** https://elitebot.dev/apiterms

The production API (api.elitebot.dev) should **never** be used for your own projects without permission. Please run your own instance of the API for that or use something else.
__This is neither a Mojang API nor a Hypixel API proxy.__

We are not affiliated with Mojang or Hypixel in any way.

<h2 align="center">Development</h2>
<hr>

Contributions are welcome!

### Prerequisites
- .NET 8.0 SDK
- Docker (unless you run the other services some other way)
- A Discord Application and Bot Token
  - [Create a Discord Application](https://discord.com/developers/applications)
- A Hypixel API Key
  - [Get a Hypixel API Key](https://developer.hypixel.net/)
- Recommended: JetBrains Rider or Visual Studio

### General Guidelines
1. New features should only be added if they are relevant and useful to the [Website](https://github.com/EliteFarmers/Website) or the [Discord Bot](https://github.com/EliteFarmers/Bot).
    1. Feel free to open an issue or join the [Discord](https://elitebot.dev/support) to discuss the feature before starting work on it!
    2. This generally means that new features should be related to farming in Hypixel Skyblock, or the Elite Farmers community.
2. Code should follow the existing style and conventions.
    1. I am aware the project structure isn't perfect and steps should be taken to improve it, but please don't try to change everything without discussing it first.
3. Run the tests before submitting a PR, and please consider adding tests for new features.

### Running the API Locally

1. Clone the repository
2. Make a copy of `EliteAPI/.env.example` and rename it to `.env` in the same directory. Then fill in the environment variables in your new file.
3. Make a copy of `EliteAPI/appsettings.json` and rename it to `appsettings.Development.json` in the same directory. 
4. Fill in at least the database connection string in the `appsettings.Development.json` file, but it should work with the default settings if using the provided `docker-compose` file locally.
5. Start up the database and redis server using `docker compose up -d database cache` in the root directory of the repository. The other services are usually not needed for local development.
6. Open the solution in JetBrains Rider, Visual Studio, or use the `dotnet` cli to run the API.
7. The API should now be running on `http://localhost:5164/`.

### Using the API with the Website or Bot
1. Follow the steps above to run the API locally.
2. Follow the instructions in the [Website](https://github.com/EliteFarmers/Website) or the [Discord Bot](https://github.com/EliteFarmers/Bot) repos to set them up.
3. Fill in the `ELITE_API_URL` environment variable in the Website or Bot with http://localhost:5164/.
4. The Website and Bot should now be using your local API.

When making changes to responses or adding new endpoints, download the API spec (http://localhost:5164/v1/swagger.json) and run `pnpm run generate-api-types` in the bot/website to update the typings.

### Making Database Changes

Please discuss any changes to the database schema in an issue or on [Discord](https://elitebot.dev/support) before spending the time doing so.

1. Make the changes to the `EliteAPI/Models/Entities` and related DTO mappings.
2. Run the EF Core migrations to generate a new migration into the `EliteAPI/Data/Migrations` folder.
3. If you are adding a new table, make sure to add a new `DbSet` to the `EliteAPI/Data/DataContext.cs` file.
4. Don't add multiple migrations for the same PR unless absolutely necessary. It should be easy for you to revert the migration and generate a new one with all the changes.
   1. Keeping migrations uncommited until the PR is ready for review is a good idea to avoid committing and removing multiple migrations.
5. Migrations are run automatically when the API starts up. (I am aware this is not ideal for production, but it's fine for development.)

### Get an Admin Account Locally

If you need to test admin features, currently you can only do so by manually adding an admin role to yourself from the database.

1. Run the API and Website locally.
2. Login to the local Website instance with your Discord account.
3. Use a tool like PGAdmin to connect to the local database.
4. Create a new record in the `AspNetUserRoles` table with your `UserId` and the `RoleId` of the admin role.
5. Log out and back in to the Website.
6. You can now access the admin features, including granting other users roles on the `/admin` page of the Website.

<h2 align="center">Docker Installation</h2>
<hr>

1. Clone the repository
2. Make a copy of `.env.example` and rename it to `.env` in the same directory. Then fill in the environment variables in your new file. These variables are used in the containers.
3. Make a copy of `EliteAPI/.env.example` and rename it to `.env` in the same directory. Then fill in the environment variables in your new file.
4. Make a copy of `EliteAPI/appsettings.json` and rename it to `appsettings.Development.json` in the same directory.
5. Fill in at least the database connection string in the `appsettings.Development.json` file, but it should work with the default settings if using the provided `docker-compose` file locally.
6. Run `docker compose up` in the root directory of the repository.
8. The API should now be running on `http://localhost:7008/` and can be put behind a reverse proxy if needed.
