FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS builder

ARG VersionSuffix
WORKDIR /sln

COPY . .

RUN dotnet tool install --tool-path ".paket" Paket
RUN dotnet restore
RUN dotnet build -c Release --no-restore --version-suffix $VersionSuffix
RUN dotnet pack -c Release --no-restore --no-build --version-suffix $VersionSuffix -o /sln/artifacts

ENTRYPOINT [ "dotnet", "nuget", "push", "/sln/artifacts/*.nupkg" ]
CMD ["--source", "https://api.nuget.org/v3/index.json"]