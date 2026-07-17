FROM --platform=$TARGETARCH mcr.microsoft.com/dotnet/aspnet:10.0 AS base

RUN apt-get update && apt-get install -y libgdiplus

WORKDIR /app
EXPOSE <port-number>
ENV ASPNETCORE_URLS=http://*:<port-number>

FROM --platform=$TARGETARCH mcr.microsoft.com/dotnet/sdk:10.0 AS build

ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0

WORKDIR /src

COPY ["api/RepoUniqueIdentifier.csproj", "api/"]

RUN dotnet restore \
	-s https://api.nuget.org/v3/index.json \
	-s https://pkgs.dev.azure.com/Pellerex/Public/_packaging/CommonLibrary/nuget/v3/index.json \
	"api/RepoUniqueIdentifier.csproj"

COPY . . 

RUN dotnet build "api/RepoUniqueIdentifier.csproj" -c Release -o /app/build

FROM build AS publish

RUN dotnet publish "api/RepoUniqueIdentifier.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "RepoUniqueIdentifier.dll"]
