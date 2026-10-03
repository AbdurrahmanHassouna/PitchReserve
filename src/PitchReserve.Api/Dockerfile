From mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PitchReserve.Api/PitchReserve.Api.csproj", "PitchReserve.Api/"]
COPY ["PitchReserve.Application/PitchReserve.Application.csproj", "PitchReserve.Application/"]
COPY ["PitchReserve.Domain/PitchReserve.Domain.csproj", "PitchReserve.Domain/"]
COPY ["PitchReserve.Infrastructure/PitchReserve.Infrastructure.csproj", "PitchReserve.Infrastructure/"]

RUN dotnet restore "PitchReserve.Api/PitchReserve.Api.csproj"

COPY . .
WORKDIR "/src/PitchReserve.Api"
RUN dotnet publish "PitchReserve.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

USER $APP_UID

COPY --from=build /app/publish .


EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PitchReserve.Api.dll"]