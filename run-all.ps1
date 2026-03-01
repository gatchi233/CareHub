Write-Host "======================================="
Write-Host "Starting CareHub Full Demo Mode"
Write-Host "======================================="

# Clean API
Write-Host "`nCleaning API..."
dotnet clean .\CareHub.Api\CareHub.Api.csproj

# Start API in new window
Write-Host "Starting API..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project .\CareHub.Api\CareHub.Api.csproj"

# Small delay to allow API to start
Start-Sleep -Seconds 3

# Optional: open Swagger
Start-Process "http://localhost:5001/swagger"

# Start Desktop app in new window
Write-Host "Starting Desktop App..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project .\CareHub.Desktop\CareHub.Desktop.csproj"

Write-Host "`nAll services launched."