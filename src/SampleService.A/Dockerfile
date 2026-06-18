FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DynamicConfig.sln ./
COPY src/DynamicConfig.Domain/DynamicConfig.Domain.csproj src/DynamicConfig.Domain/
COPY src/DynamicConfig.Library/DynamicConfig.Library.csproj src/DynamicConfig.Library/
COPY src/SampleService.A/SampleService.A.csproj src/SampleService.A/

RUN dotnet restore src/SampleService.A/SampleService.A.csproj

COPY . .
RUN dotnet publish src/SampleService.A/SampleService.A.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8081
ENTRYPOINT ["dotnet", "SampleService.A.dll"]
