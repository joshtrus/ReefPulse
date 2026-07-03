# ---- build stage: full SDK, compiles and publishes ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/ReefPulse.Api/ReefPulse.Api.csproj", "src/ReefPulse.Api/"]
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
