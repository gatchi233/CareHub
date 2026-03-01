Write-Host "Cleaning CareHub.Api..."
dotnet clean .\CareHub.Api\CareHub.Api.csproj

Write-Host "Starting API..."
Start-Process "http://localhost:5001/swagger"

dotnet run --project .\CareHub.Api\CareHub.Api.csproj