# syntax=docker/dockerfile:1

# Stage 1
FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env

WORKDIR /src

COPY . ./

RUN dotnet build stocks.sln

RUN dotnet publish -c Release -o /publish stocks.sln

# Stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 as runtime

WORKDIR /publish

COPY --from=build-env /publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "stocks.dll"]
