#!/bin/sh
set -eux

echo "PORT=${PORT:-<empty>}"
echo "PWD=$(pwd)"

ls -la
ls -la out || true

dotnet --info

exec dotnet ./out/PitaRadiowebseite.dll