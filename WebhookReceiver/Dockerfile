FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["WebhookReceiver/WebhookReceiver.csproj", "WebhookReceiver/"]
RUN dotnet restore "WebhookReceiver/WebhookReceiver.csproj"

COPY . .
WORKDIR "/src/WebhookReceiver"
RUN dotnet build "WebhookReceiver.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebhookReceiver.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebhookReceiver.dll"]
