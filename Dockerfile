#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-1903 AS build

WORKDIR /shared
COPY ["shared/shared.csproj", "./"]
RUN dotnet restore "./shared.csproj"
COPY ["shared/", "./"]

WORKDIR /src
COPY ["web/web.csproj", "./"]
RUN dotnet restore "./web.csproj"
COPY ["web/", "./"]

WORKDIR "/src/."
RUN dotnet build "web.csproj" -c Release -o /app/build
RUN dotnet publish "web.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-nanoserver-1903 AS final
EXPOSE 80 443
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "web.dll"]
