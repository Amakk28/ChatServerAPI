FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ChatRoomAPI.csproj", "."]
RUN dotnet restore "ChatRoomAPI.csproj"
COPY . .
RUN dotnet build "ChatRoomAPI.csproj" -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "ChatRoomAPI.dll"]