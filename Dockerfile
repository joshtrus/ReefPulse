# ---- build stage: full SDK, compiles and publishes ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Copy every project file first (and restore) so this layer is cached and only re-runs
# when a .csproj changes, not on every source edit. The API transitively pulls in the
# Domain and Infrastructure projects, so all three must be present to restore.
COPY ["Directory.Build.props", "./"]
COPY ["src/ReefPulse.Api/ReefPulse.Api.csproj", "src/ReefPulse.Api/"]
COPY ["src/ReefPulse.Infrastructure/ReefPulse.Infrastructure.csproj", "src/ReefPulse.Infrastructure/"]
COPY ["src/ReefPulse.Domain/ReefPulse.Domain.csproj", "src/ReefPulse.Domain/"]
RUN dotnet restore "src/ReefPulse.Api/ReefPulse.Api.csproj"
COPY . .
RUN dotnet publish "src/ReefPulse.Api/ReefPulse.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime stage: slim ASP.NET image, no SDK ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ReefPulse.Api.dll"]
