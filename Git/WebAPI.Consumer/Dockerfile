FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проектов и восстанавливаем зависимости
COPY . /src

WORKDIR /src/WebAPI.Consumer
RUN dotnet restore WebAPI.Consumer.csproj

RUN dotnet build WebAPI.Consumer.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish WebAPI.Consumer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish /app
ENTRYPOINT dotnet WebAPI.Consumer.dll
