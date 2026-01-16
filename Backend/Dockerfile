FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["PureTCOWebApp.csproj", "."]
RUN dotnet restore "./PureTCOWebApp.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "PureTCOWebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PureTCOWebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PureTCOWebApp.dll"]