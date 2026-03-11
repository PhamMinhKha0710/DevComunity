FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/SocialTechsy.SocialNetwork.Domain/*.csproj SocialTechsy.SocialNetwork.Domain/
COPY src/SocialTechsy.SocialNetwork.Application/*.csproj SocialTechsy.SocialNetwork.Application/
COPY src/SocialTechsy.SocialNetwork.Infrastructure/*.csproj SocialTechsy.SocialNetwork.Infrastructure/
COPY src/SocialTechsy.SocialNetwork.Api/*.csproj SocialTechsy.SocialNetwork.Api/
RUN dotnet restore SocialTechsy.SocialNetwork.Api/SocialTechsy.SocialNetwork.Api.csproj

COPY src/ .
RUN dotnet publish SocialTechsy.SocialNetwork.Api/SocialTechsy.SocialNetwork.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SocialTechsy.SocialNetwork.Api.dll"]
