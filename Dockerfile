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
RUN dotnet publish "web.csproj" -c Release -o /app/publish /property:GenerateFullPaths=true

FROM tobiasfenster/nginx-custom:1.18.0-$BASE AS final
EXPOSE 80
WORKDIR /nginx
CMD "C:\nginx\nginx.exe"

COPY --from=build /app/publish/wwwroot/ /nginx/html/