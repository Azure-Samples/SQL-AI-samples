# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

# Return the ADO.Net SQL connection string by running `sqlcmd config connection-strings`, which will return
#

# Run `sqlcmd config connection-strings` to get the connection strings
# and filter the output to get the ADO.Net connection string
$adoNetConnectionString = & $env:ProgramFiles\SqlCmd\sqlcmd.exe config connection-strings | Select-String -Pattern "ADO.NET" | ForEach-Object { $_.ToString().Trim() }

# Remove the "ADO.NET: " prefix from the connection string
$adoNetConnectionString = $adoNetConnectionString -replace "ADO.NET: ", ""

# Return the ADO.Net connection string
$adoNetConnectionString


