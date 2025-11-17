FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проектов и восстанавливаем зависимости
COPY . /src

WORKDIR /src/WebAPI
RUN dotnet restore WebAPI.csproj

RUN dotnet build WebAPI.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish WebAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish /app
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT dotnet WebAPI.dll
