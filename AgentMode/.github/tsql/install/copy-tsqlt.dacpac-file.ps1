# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

# This powershell script takes a $projectPath
# as a parameter and copies the tSQLt.dacpac file to the $projectPath\Tests
# from the "$env:TEMP\tSQLt\tSQLtDacpacs" folder

param (
    [string]$projectPath
)

# Define the temp folder for unzipped files
$tempUnzipFolder = "$env:TEMP\tSQLt\tSQLtDacpacs"
# Define the tSQLt class file name
$tsqltDacPacFile = "tSQLt.2019.dacpac"
# Define the tSQLt class file path
$tsqltDacPacPath = "$tempUnzipFolder\$tsqltDacPacFile"
# Check if the tSQLt class file exists
if (Test-Path $tsqltDacPacPath) {
    # Copy the tSQLt.dacpac to the destination folder
    Copy-Item -Path $tsqltDacPacPath -Destination $projectPath\Tests -Force
    Write-Host "$tsqltDacPacFile file copied to $projectPath\Tests"
} else {
    Write-Error "$tsqltDacPacFile file not found in $tempUnzipFolder"
}



