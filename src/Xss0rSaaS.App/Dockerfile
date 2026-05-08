FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Xss0rSaaS.App/Xss0rSaaS.App.csproj", "src/Xss0rSaaS.App/"]
RUN dotnet restore "src/Xss0rSaaS.App/Xss0rSaaS.App.csproj"

COPY . .
RUN dotnet publish "src/Xss0rSaaS.App/Xss0rSaaS.App.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Xss0rSaaS.App.dll"]
