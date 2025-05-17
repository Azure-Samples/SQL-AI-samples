# A powershell script for Windows to check if any desktop container runtime is available and running

# Check if Docker Desktop is running
if (Get-Command "docker" -ErrorAction SilentlyContinue) {
    $dockerStatus = & docker info --format '{{.ServerVersion}}'
    if ($dockerStatus) {
        Write-Host "Docker Desktop is running."
        exit 0
    } else {
        Write-Host "Docker Desktop is not running."
        exit 1
    }
}

# Check if Podman Desktop is running
if (Get-Command "podman" -ErrorAction SilentlyContinue) {
    $podmanStatus = & podman info --format '{{.ServerVersion}}'
    if ($podmanStatus) {
        Write-Host "Podman Desktop is running."
        exit 0
    } else {
        Write-Host "Podman Desktop is not running."
        exit 1
    }
}
