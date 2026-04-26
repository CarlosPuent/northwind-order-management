# Load environment variables from .env file
Get-Content "$PSScriptRoot\.env" | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.*)$') {
        [Environment]::SetEnvironmentVariable($matches[1].Trim(), $matches[2].Trim(), 'Process')
    }
}

Write-Host "Environment variables loaded from .env" -ForegroundColor Green
dotnet run --project src/Northwind.Api