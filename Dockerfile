FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AppFletesMueve.Api/AppFletesMueve.Api.csproj AppFletesMueve.Api/
RUN dotnet restore AppFletesMueve.Api/AppFletesMueve.Api.csproj
COPY AppFletesMueve.Api/ AppFletesMueve.Api/
WORKDIR /src/AppFletesMueve.Api
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "AppFletesMueve.Api.dll"]