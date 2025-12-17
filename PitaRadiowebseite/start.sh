#!/bin/sh
set -eux

echo "PORT=${PORT:-<empty>}"
echo "PWD=$(pwd)"
echo "USER=$(id -un || true)"

echo "LS ROOT:"
ls -la /

echo "LS current:"
ls -la

echo "LS out (if vorhanden):"
ls -la out || true

dotnet --info || true

if [ ! -f ./out/PitaRadiowebseite.dll ]; then
  echo "ERROR: ./out/PitaRadiowebseite.dll fehlt."
  echo "Stelle sicher, dass 'dotnet publish -c Release -o out' im Build ausgeführt wurde."
  ls -la ./out || true
  exit 1
fi

exec dotnet ./out/PitaRadiowebseite.dll