#!/usr/bin/env bash
dotnet tool install --tool-path ".paket" paket
dotnet restore
dotnet build -c Release
# paket seems to have an issue with the generated nuspec on mac and linux
#dotnet pack -c Release -o bin --no-build
