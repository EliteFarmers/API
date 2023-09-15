#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 9102

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EliteAPI/EliteAPI.csproj", "EliteAPI/"]
RUN dotnet restore "EliteAPI/EliteAPI.csproj"
COPY . .
WORKDIR "/src/EliteAPI"
RUN dotnet build "EliteAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EliteAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EliteAPI.dll"]