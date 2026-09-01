FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Ecommerce.slnx ./
COPY src/Ecommerce.Domain/Ecommerce.Domain.csproj src/Ecommerce.Domain/
COPY src/Ecommerce.Application/Ecommerce.Application.csproj src/Ecommerce.Application/
COPY src/Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj src/Ecommerce.Infrastructure/
COPY src/Ecommerce.Api/Ecommerce.Api.csproj src/Ecommerce.Api/
COPY tests/Ecommerce.Application.Tests/Ecommerce.Application.Tests.csproj tests/Ecommerce.Application.Tests/
COPY tests/Ecommerce.Api.Tests/Ecommerce.Api.Tests.csproj tests/Ecommerce.Api.Tests/
RUN dotnet restore src/Ecommerce.Api/Ecommerce.Api.csproj

COPY . .
RUN dotnet publish src/Ecommerce.Api/Ecommerce.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Ecommerce.Api.dll"]
