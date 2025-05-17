# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

# This script takes a parameter which is the context-name (passed into sqlcmd)
# and creates a new SQL Server instance using the sqlcmd command line tool:
#   `sqlcmd create mssql --tag 2019-latest --context-name $ContextName`

# If the context already exists, it will be removed first.

param (
    [string]$ProjectName
)

# Run `sqlcmd config get-contexts`, it returns a lists of contexts, e.g.
# 
# ```
# - mssql
# - mssql2
# - mssql3
# ```

# Check if the context already exists
$existingContexts = & $env:ProgramFiles\SqlCmd\sqlcmd.exe config get-contexts

# Normalize context names by trimming and removing dashes
$normalizedContexts = $existingContexts | ForEach-Object { $_.Trim().TrimStart('-').Trim() }
$contextExists = $normalizedContexts | Where-Object { $_ -ieq $ProjectName }
if ($contextExists) {
    # Run `sqlcmd use-context $ProjectName`, it sets the current context, which we'll then delete
    & $env:ProgramFiles\SqlCmd\sqlcmd.exe config use-context $ProjectName

    # Delete the context by running `sqlcmd delete --force --yes`
    & $env:ProgramFiles\SqlCmd\sqlcmd.exe delete --force --yes
}

# Create a new SQL Server instance using the sqlcmd command line tool
& $env:ProgramFiles\SqlCmd\sqlcmd.exe create mssql --tag 2019-latest --accept-eula --user-database $ProjectName --context-name $ProjectName
