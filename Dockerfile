# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

# Copy solution and project files
COPY ["Gametopia.Domain.Api/Gametopia.Domain.Api.csproj", "Gametopia.Domain.Api/"]
COPY ["Gametopia.Domain.Application/Gametopia.Domain.Application.csproj", "Gametopia.Domain.Application/"]
COPY ["Gametopia.Domain.Domain/Gametopia.Domain.Domain.csproj", "Gametopia.Domain.Domain/"]
COPY ["Gametopia.Domain.Infrastructure/Gametopia.Domain.Infrastructure.csproj", "Gametopia.Domain.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Gametopia.Domain.Api/Gametopia.Domain.Api.csproj"

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish "Gametopia.Domain.Api/Gametopia.Domain.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Copy published application from builder
COPY --from=builder /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080

# Run application
ENTRYPOINT ["dotnet", "Gametopia.Domain.Api.dll"]
