FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

RUN mkdir -p /src/SinovApp

COPY SinovApp.csproj /src/SinovApp/SinovApp.csproj
WORKDIR "/src/SinovApp"
RUN dotnet restore "SinovApp.csproj"

COPY . /src/SinovApp/
WORKDIR "/src/SinovApp"

RUN dotnet build "SinovApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SinovApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SinovApp.dll"]
