# -SourceFile e.g. ConfMS/bin/Debug/ConfMS.dacpac
param (
    [string]$ProjectName
)

# Get the target connection string by running `get-sql-connection-string.ps1`
$targetConnectionString = & ./.github/tsql/install/get-sql-connection-string.ps1

# Ensure the connection string is wrapped in double quotes for SqlPackage.exe
$quotedConnectionString = '"' + $targetConnectionString + '"'

# Define the source file path
$sourceFile = "./$ProjectName/bin/Debug/$ProjectName.dacpac"
if (!(Test-Path $sourceFile)) {
    $sourceFile = "../$ProjectName/bin/Debug/$ProjectName.dacpac"
}

# Print the source file path
Write-Host "Source file: $sourceFile"
# Run SqlPackage.exe /Action:Publish /SourceFile:$SourceFile /TargetConnectionString:$targetConnectionString /p:IncludeCompositeObjects=true
# Note $targetConnectionString can have $ in it
$publishCommand = "SqlPackage.exe /Action:Publish /SourceFile:$sourceFile /TargetConnectionString:$quotedConnectionString /p:IncludeCompositeObjects=true"
$publishCommand = $publishCommand -replace '\$', '`$' # Escape $ in the connection string
Write-Host "Running command: $publishCommand"
Invoke-Expression $publishCommand
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to publish the DACPAC. Please check the output for errors."
    exit 1
}
