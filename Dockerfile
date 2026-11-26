FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Wortlaut.Indexer/Wortlaut.Indexer.csproj src/Wortlaut.Indexer/
RUN dotnet restore src/Wortlaut.Indexer

COPY schema schema
COPY src src
RUN dotnet publish src/Wortlaut.Indexer --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

RUN mkdir /data && chown $APP_UID /data
VOLUME /data
USER $APP_UID
ENTRYPOINT ["dotnet", "wortlaut-indexer.dll"]
