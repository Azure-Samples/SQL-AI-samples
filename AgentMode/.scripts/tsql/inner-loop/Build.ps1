# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

param (
    [string]$ProjectName
)

# Run dotnet build
dotnet build "$ProjectName/$ProjectName.sqlproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build the project. Please check the output for errors."
    exit 1
}


