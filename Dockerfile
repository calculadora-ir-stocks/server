# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build-env

WORKDIR /src

COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine as runtime
WORKDIR /publish
COPY --from=build-env /publish .

COPY init.sql /docker-entrypoint-initdb.d/

EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
