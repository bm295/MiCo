# syntax=docker/dockerfile:1
FROM node:24-alpine AS client-build
WORKDIR /client
COPY ClientApp/package*.json ./
RUN npm ci
COPY ClientApp/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WebApplication/WebApplication.csproj WebApplication/
RUN dotnet restore WebApplication/WebApplication.csproj

COPY . .
RUN dotnet publish WebApplication/WebApplication.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=client-build /client/dist/milkco-client/browser ./wwwroot

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WebApplication.dll"]
