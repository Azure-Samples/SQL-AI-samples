# Use the dotnet CLI to see if Build.Sql.Templates are installed, if not, install them

# Check if Build.Sql.Templates is installed
$installed = dotnet new list | Select-String -Pattern "SQL Server Database Project"
if ($installed -eq $null) {
    Write-Host "Build.Sql.Templates is not installed. Installing..."
    # Install Build.Sql.Templates
    dotnet new install Microsoft.Build.Sql.Templates
} else {
    Write-Host "Build.Sql.Templates is already installed."
}
