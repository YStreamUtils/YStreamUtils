FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["YStreamUtils.Docker/YStreamUtils.Docker.csproj", "YStreamUtils.Docker/"]
RUN dotnet restore "YStreamUtils.Docker/YStreamUtils.Docker.csproj"
COPY . .
WORKDIR "/src/YStreamUtils.Docker"
RUN dotnet build "./YStreamUtils.Docker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./YStreamUtils.Docker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YStreamUtils.Docker.dll"]
