# Build context: repository root (needs shared/ and server/).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/Directory.Build.props shared/
COPY shared/HADAL.Shared/ shared/HADAL.Shared/
COPY server/Hadal.Game/ server/Hadal.Game/
COPY server/Hadal.Gateway/ server/Hadal.Gateway/
RUN dotnet publish server/Hadal.Gateway/Hadal.Gateway.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Same file the Unity setup generates its ScriptableObjects from.
COPY shared/catalog/catalog.json catalog/catalog.json
ENV ASPNETCORE_URLS=http://+:8080 \
    Game__CatalogPath=/app/catalog/catalog.json
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Hadal.Gateway.dll"]
