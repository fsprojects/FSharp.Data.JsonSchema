#!/usr/bin/env bash
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-restore --no-build
dotnet pack -c Release -o bin --no-restore --no-build
