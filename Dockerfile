ARG BASE
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-$BASE AS build

WORKDIR /shared
COPY ["shared/shared.csproj", "./"]
RUN dotnet restore "./shared.csproj"
COPY ["shared/", "./"]

WORKDIR /src
COPY ["web/web.csproj", "./"]
COPY ["web/nuget.config", "."]
RUN dotnet restore "./web.csproj"
COPY ["web/", "./"]

WORKDIR "/src/."
USER ContainerAdministrator
RUN dotnet build "web.csproj" -c Release -o /app/build
RUN dotnet publish "web.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-nanoserver-$BASE AS final
EXPOSE 80 443
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "web.dll"]
