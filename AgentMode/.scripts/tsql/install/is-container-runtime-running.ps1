# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

# A powershell script for Windows to check if any desktop container runtime is
# available and running

function Write-ContainerRuntimeInstructions {
    Write-Host "To install a container runtime, please visit the following links:"
    Write-Host "- Docker Desktop: https://www.docker.com/products/docker-desktop"
    Write-Host "- Podman Desktop: https://podman-desktop.io/"
    Write-Host "Please install one of these runtimes and try again."
}

# Check if Docker Desktop is running
$dockerAvailable = $false
$dockerRunning = $false
if (Get-Command "docker" -ErrorAction SilentlyContinue) {
    $dockerAvailable = $true
    $dockerStatus = & docker info --format '{{.ServerVersion}}' 2>$null
    if ($dockerStatus) {
        Write-Host "Docker Desktop is running."
        exit 0
    } else {
        Write-Host "Docker Desktop is not running."
    }
}

# Check if Podman Desktop is running
$podmanAvailable = $false
$podmanRunning = $false
if (Get-Command "podman" -ErrorAction SilentlyContinue) {
    $podmanAvailable = $true
    $podmanStatus = & podman info --format '{{.ServerVersion}}' 2>$null
    if ($podmanStatus) {
        Write-Host "Podman Desktop is running."
        exit 0
    } else {
        Write-Host "Podman Desktop is not running."
    }
}

if (-not $dockerAvailable -and -not $podmanAvailable) {
    Write-Error "The `docker` and `podman` commands are not available"
    Write-ContainerRuntimeInstructions
    exit 1
}

Write-Error "No container runtime is running."
Write-ContainerRuntimeInstructions
exit 1
