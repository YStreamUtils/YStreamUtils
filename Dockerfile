FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["YStreamUtils.Web/YStreamUtils.Web.csproj", "YStreamUtils.Web/"]
RUN dotnet restore "YStreamUtils.Web/YStreamUtils.Web.csproj"
COPY . .
WORKDIR "/src/YStreamUtils.Web"
RUN dotnet build "./YStreamUtils.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./YStreamUtils.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YStreamUtils.Web.dll"]
