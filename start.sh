#!/bin/sh
set -eux

export ASPNETCORE_DETAILEDERRORS=true
export DOTNET_EnableDiagnostics=1
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

echo "PORT=${PORT:-<empty>}"
echo "PWD=$(pwd)"

ls -la
ls -la out || true

exec dotnet ./out/PitaRadiowebseite.dll