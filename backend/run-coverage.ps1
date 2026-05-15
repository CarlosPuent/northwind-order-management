# Clean previous results
Remove-Item -Recurse -Force ./TestResults -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ./coverage-report -ErrorAction SilentlyContinue

# Run tests with coverage
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML report
reportgenerator `
  -reports:"./TestResults/**/coverage.cobertura.xml" `
  -targetdir:"./coverage-report" `
  -reporttypes:Html `
  -classfilters:"-Microsoft.AspNetCore.OpenApi*;-System.Runtime*"

# Open report
Start-Process ./coverage-report/index.html