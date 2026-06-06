FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore dependencies
COPY GhostSend.slnx ./
COPY GhostSend.Api/GhostSend.Api.csproj GhostSend.Api/
COPY GhostSend.Application/GhostSend.Application.csproj GhostSend.Application/
COPY GhostSend.Domain/GhostSend.Domain.csproj GhostSend.Domain/
COPY GhostSend.Infrastructure/GhostSend.Infrastructure.csproj GhostSend.Infrastructure/
COPY GhostSend.UnitTests/GhostSend.UnitTests.csproj GhostSend.UnitTests/

RUN dotnet restore GhostSend.Api/GhostSend.Api.csproj

# Copy the remaining source code and build
COPY . .
WORKDIR /src/GhostSend.Api
RUN dotnet publish GhostSend.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create a non-root system user and group for least-privilege execution
# Install GSSAPI library needed by AWS SDK for S3
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

# Create uploads directory with restricted permissions (owner read/write/exec only)
RUN mkdir -p /app/uploads \
    && chown -R appuser:appgroup /app/uploads \
    && chmod 750 /app/uploads

# Switch to non-root user
USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "GhostSend.Api.dll"]
