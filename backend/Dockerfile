# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and build props
COPY Northwind.OrderManagement.sln .
COPY Directory.Build.props .

# Copy ALL csproj files (src + tests) for restore layer caching
COPY src/Northwind.Domain/Northwind.Domain.csproj src/Northwind.Domain/
COPY src/Northwind.Application/Northwind.Application.csproj src/Northwind.Application/
COPY src/Northwind.Infrastructure/Northwind.Infrastructure.csproj src/Northwind.Infrastructure/
COPY src/Northwind.Api/Northwind.Api.csproj src/Northwind.Api/
COPY tests/Northwind.Application.Tests/Northwind.Application.Tests.csproj tests/Northwind.Application.Tests/
COPY tests/Northwind.Api.Tests/Northwind.Api.Tests.csproj tests/Northwind.Api.Tests/

RUN dotnet restore

# Copy everything else and publish only the API project
COPY . .
RUN dotnet publish src/Northwind.Api/Northwind.Api.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5281
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5281

ENTRYPOINT ["dotnet", "Northwind.Api.dll"]