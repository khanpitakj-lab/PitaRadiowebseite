# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore PitaRadiowebseite/PitaRadiowebseite.csproj
RUN dotnet publish PitaRadiowebseite/PitaRadiowebseite.csproj -c Release -o /app/out --no-restore

# Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "PitaRadiowebseite.dll"]