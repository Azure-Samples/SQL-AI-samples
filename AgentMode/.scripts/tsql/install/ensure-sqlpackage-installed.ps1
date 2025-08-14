# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

# Check to see if SqlPackage is installed by running:
#  `dotnet tool list --format json -g microsoft.sqlpackage`
# if it is installed, the response will be in the form:
# ```
# {"version":1,"data":[{"packageId":"microsoft.sqlpackage","version":"170.1.14","commands":["sqlpackage"]}]}
# ```

# if not installed, install it by running:
#  `dotnet tool install -g microsoft.sqlpackage`

# Check if SqlPackage is installed
$installed = dotnet tool list --format json -g microsoft.sqlpackage | ConvertFrom-Json
if ($installed.data.Count -gt 0) {
    Write-Host "SqlPackage is already installed."
    exit 0
}

# If SqlPackage is not installed, install it
Write-Host "SqlPackage is not installed. Installing..." 
dotnet tool install -g microsoft.sqlpackage
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to install SqlPackage. Please check your .NET installation and try again."
    exit 1
}




