FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Nifty50.Api/Nifty50.Api.csproj", "Nifty50.Api/"]
COPY ["src/Nifty50.Core/Nifty50.Core.csproj", "Nifty50.Core/"]
COPY ["src/Nifty50.Infrastructure/Nifty50.Infrastructure.csproj", "Nifty50.Infrastructure/"]
RUN dotnet restore "Nifty50.Api/Nifty50.Api.csproj"

# Copy the rest of the source code
COPY src/ Nifty50.Api/../
WORKDIR "/src/Nifty50.Api"

# Build and publish the release version
RUN dotnet publish "Nifty50.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Generate the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render exposes the port in the PORT environment variable
ENV ASPNETCORE_HTTP_PORTS=8080
ENV PORT=8080
ENV DOTNET_gcServer=0
ENV DOTNET_GCHeapHardLimit=419430400
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl --fail http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Nifty50.Api.dll"]
