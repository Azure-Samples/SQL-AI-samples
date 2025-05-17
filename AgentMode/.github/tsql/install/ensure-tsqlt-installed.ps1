# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

# Install tSQLt if it is not already installed

# If tSQLt is not installed:
# - Download from: https://tsqlt.org/download/tsqlt/ to a temp folder
# - Unzip the downloaded file to a temp folder
# - Execute the PrepareServer.sql using sqlcmd, this only has to be done once per server.

# Define the URL for the tSQLt download
$tsqltUrl = "https://tsqlt.org/download/tsqlt/"
# Define the temp folder for downloads
$tempFolder = "$env:TEMP"
# Define the temp folder for unzipped files
$tempUnzipFolder = "$env:TEMP\tSQLt"
# Define the tSQLt zip file name
$tsqltZipFile = "tSQLt.zip"
# Define the tSQLt prepare server file name
$tsqltPrepareServerFile = "PrepareServer.sql"

# Define the SQLCMD command as an array for PowerShell invocation
$sqlcmdExe = "$env:ProgramFiles\SqlCmd\sqlcmd.exe"
$sqlcmdArgs = @("-i")

# Check if tSQLt is already installed
$checkTSQLtInstalled = & $sqlcmdExe -Q "SELECT COUNT(*) FROM sys.objects WHERE name = 'tSQLt' AND type = 'P'" -h -1 -W | Select-Object -First 1
if ($checkTSQLtInstalled -eq 0) {
    Write-Host "tSQLt is not installed. Installing tSQLt..."
    
    # Create the temp folder if it doesn't exist
    if (-not (Test-Path $tempUnzipFolder)) {
        New-Item -ItemType Directory -Path $tempUnzipFolder
    }

    # Download the tSQLt zip file
    Invoke-WebRequest -Uri $tsqltUrl -OutFile "$tempFolder\$tsqltZipFile"

    # Unzip the downloaded file
    Expand-Archive -Force -Path "$tempFolder\$tsqltZipFile" -DestinationPath $tempUnzipFolder

} else {
    Write-Host "tSQLt is already installed."
}

# Execute the PrepareServer.sql script
$prepareServerPath = "$tempUnzipFolder\$tsqltPrepareServerFile"
& $sqlcmdExe @($sqlcmdArgs + $prepareServerPath)
Write-Host "tSQLt installation completed."

# Delete the tsqlt zip file
if (Test-Path "$tempFolder\$tsqltZipFile") {
    Remove-Item "$tempFolder\$tsqltZipFile"
}