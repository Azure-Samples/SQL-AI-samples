# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

# -SourceFile e.g. ConfMS/bin/Debug/ConfMS.dacpac
param (
    [string]$ProjectName
)

# Get the target connection string by running `get-sql-connection-string.ps1`
$targetConnectionString = & ./.scripts/tsql/install/get-sql-connection-string.ps1

# Ensure the connection string is wrapped in double quotes for SqlPackage.exe
$quotedConnectionString = '"' + $targetConnectionString + '"'

# Define the source file path
$sourceFile = "./$ProjectName/bin/Debug/$ProjectName.dacpac"
if (!(Test-Path $sourceFile)) {
    $sourceFile = "../$ProjectName/bin/Debug/$ProjectName.dacpac"
}

# Print the source file path
Write-Host "Source file: $sourceFile"

# Mask password in connection string for display
$maskedConnectionString = $targetConnectionString -replace '(?i)(Password|Pwd)=([^;]*)', '$1=*****'

# Print the publish command with masked password
$maskedQuotedConnectionString = '"' + $maskedConnectionString + '"'
$maskedPublishCommand = "$env:USERPROFILE\.dotnet\tools\SqlPackage.exe /Action:Publish /SourceFile:$sourceFile /TargetConnectionString:$maskedQuotedConnectionString /p:IncludeCompositeObjects=true"
$maskedPublishCommand = $maskedPublishCommand -replace '\$', '`$' # Escape $ in the connection string
Write-Host "Running command: $maskedPublishCommand"

# Run the actual publish command (with real password)
$publishCommand = "$env:USERPROFILE\.dotnet\tools\SqlPackage.exe /Action:Publish /SourceFile:$sourceFile /TargetConnectionString:$quotedConnectionString /p:IncludeCompositeObjects=true"
$publishCommand = $publishCommand -replace '\$', '`$' # Escape $ in the connection string
Invoke-Expression $publishCommand
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to publish the DACPAC. Please check the output for errors."
    exit 1
}
