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
COPY --from=build /app/publish .

# Create uploads directory (ensure permissions)
RUN mkdir -p /app/uploads && chmod 777 /app/uploads

EXPOSE 80

ENTRYPOINT ["dotnet", "GhostSend.Api.dll"]
