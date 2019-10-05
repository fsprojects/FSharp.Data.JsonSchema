#!/usr/bin/env bash
dotnet tool install --tool-path ".paket" Paket
dotnet restore
dotnet build -c Release
dotnet pack -c Release -o bin --no-build