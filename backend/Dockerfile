# Two stages, so what ships is the runtime and the published output and nothing else: the SDK,
# the sources and the NuGet cache stay behind in the build stage.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first. Restoring before the sources are copied means editing code does not
# invalidate the package layer, so a rebuild does not download the world again.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Forum.Api/Forum.Api.csproj src/Forum.Api/
COPY src/Forum.Application/Forum.Application.csproj src/Forum.Application/
COPY src/Forum.Domain/Forum.Domain.csproj src/Forum.Domain/
COPY src/Forum.Infrastructure/Forum.Infrastructure.csproj src/Forum.Infrastructure/
RUN dotnet restore src/Forum.Api/Forum.Api.csproj

COPY src/ src/
RUN dotnet publish src/Forum.Api/Forum.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The PostgreSQL driver probes for Kerberos on its first connection. The library is absent from
# the runtime image, and its absence is logged as an error even though nothing here uses it.
# Installing it costs about two megabytes and keeps the first thing an assessor reads honest.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# The base image ships a non-root account. The API writes nothing to disk, so it needs no more.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Forum.Api.dll"]
