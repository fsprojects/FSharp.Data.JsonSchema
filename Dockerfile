FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS builder

ARG VersionSuffix
WORKDIR /sln

COPY . .

RUN dotnet tool install --tool-path ".paket" Paket
RUN dotnet restore
RUN if [ -n "$VersionSuffix" ]; then dotnet build -c Release --no-restore --version-suffix $VersionSuffix; else dotnet build -c Release --no-restore; fi
RUN dotnet test -c Release --no-restore --no-build
RUN if [ -n "$VersionSuffix" ]; then dotnet pack -c Release --no-restore --no-build --version-suffix $VersionSuffix -o /sln/artifacts; else dotnet pack -c Release --no-restore --no-build -o /sln/artifacts; fi

ENTRYPOINT [ "dotnet", "nuget", "push", "/sln/artifacts/*.nupkg" ]
CMD ["--source", "https://api.nuget.org/v3/index.json"]
