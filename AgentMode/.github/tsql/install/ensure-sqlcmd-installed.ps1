# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

# Ensure sqlcmd is installed in C:\Program Files\SqlCmd\sqlcmd.exe

# Test if 'sqlcmd' is available in `C:\Program Files\SqlCmd`

$expectedPath = "$env:ProgramFiles\SqlCmd\sqlcmd.exe"

if (-not (Test-Path $expectedPath)){
    Write-Host "'sqlcmd' not found in $env:ProgramFiles. Attempting to install using winget..."

    # Check if winget is available
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        Write-Error "'winget' is not installed. Please install winget manually and re-run this script."
        exit 1
    }

    # Install Microsoft SQL Server Command Line Tools (sqlcmd) using winget
    winget install sqlcmd -e --accept-source-agreements --accept-package-agreements

    # Verify installation
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        Write-Error "'sqlcmd' installation failed. Please install manually."
        exit 1
    } else {
        Write-Host "'sqlcmd' installed successfully."
    }
} else {
   Write-Host "'sqlcmd' is already installed at $expectedPath."

}