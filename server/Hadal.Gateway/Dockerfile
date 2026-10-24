# Build context: repository root (needs shared/ and server/).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/HADAL.Shared/ shared/HADAL.Shared/
COPY server/Hadal.Gateway/ server/Hadal.Gateway/
RUN dotnet publish server/Hadal.Gateway/Hadal.Gateway.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Hadal.Gateway.dll"]
