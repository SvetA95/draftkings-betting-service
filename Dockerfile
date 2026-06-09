FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/BettingService/BettingService.csproj ./BettingService/
RUN dotnet restore BettingService/BettingService.csproj
COPY src/BettingService/ ./BettingService/
RUN dotnet publish BettingService/BettingService.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5000
ENV UsePostgres=true
EXPOSE 5000
ENTRYPOINT ["dotnet", "BettingService.dll"]