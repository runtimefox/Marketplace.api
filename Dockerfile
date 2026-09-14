FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MyApi.csproj ./
RUN dotnet restore MyApi.csproj
COPY . .
RUN dotnet publish MyApi.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM build AS migrations-bundle
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 10.0.12
RUN ConnectionStrings__DbConnection="Host=localhost" \
    Jwt__Key="design-time-placeholder-key-not-used-0123456789" \
    dotnet ef migrations bundle --project MyApi.csproj --configuration Release --output /app/efbundle --force

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrations
WORKDIR /app
COPY --from=migrations-bundle /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "MyApi.dll"]
