FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src


COPY ["OpportunityShield.Api/OpportunityShield.Api.csproj", "OpportunityShield.Api/"]
COPY ["OpportunityShield.Application/OpportunityShield.Application.csproj", "OpportunityShield.Application/"]
COPY ["OpportunityShield.Domain/OpportunityShield.Domain.csproj", "OpportunityShield.Domain/"]
COPY ["OpportunityShield.Infrastructure/OpportunityShield.Infrastructure.csproj", "OpportunityShield.Infrastructure/"]


RUN dotnet restore "OpportunityShield.Api/OpportunityShield.Api.csproj"


COPY . .


WORKDIR "/src/OpportunityShield.Api"
RUN dotnet publish "OpportunityShield.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false



FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .


ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "OpportunityShield.Api.dll"]