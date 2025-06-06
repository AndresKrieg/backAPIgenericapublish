# Imagen base para ejecutar la app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Imagen para compilar y publicar la app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "csharpapigenerica.csproj"
RUN dotnet publish "csharpapigenerica.csproj" -c Release -o /app/publish

# Imagen final para producción
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "csharpapigenerica.dll"]
