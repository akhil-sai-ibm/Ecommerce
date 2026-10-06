$ErrorActionPreference = "Stop"

$solutionDir = $PSScriptRoot
$runtimeDir = Join-Path $solutionDir ".runtime"
$stateFile = Join-Path $runtimeDir "services.json"

if (-not (Test-Path $stateFile)) {
    Write-Host "No runtime state found at $stateFile. Nothing to stop." -ForegroundColor Yellow
    exit
}

try {
    $content = Get-Content $stateFile -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        Write-Host "State file is empty." -ForegroundColor Yellow
        exit
    }
    $parsed = ConvertFrom-Json $content
    $state = @{}
    if ($parsed -ne $null) {
        foreach ($prop in $parsed.psobject.properties) {
            $state[$prop.Name] = $prop.Value
        }
    }
} catch {
    Write-Host "Failed to parse runtime state file." -ForegroundColor Red
    exit
}

Write-Host "========================================"
Write-Host " ECOMMERCE UC3 - SHUTDOWN"
Write-Host "========================================"

# Order in which to stop (reverse of startup)
$stopOrder = @("Frontend", "ApiGateway", "PaymentService", "OrderService", "ProductService", "UserService")

foreach ($key in $stopOrder) {
    if ($state.Contains($key)) {
        $pidToKill = $state[$key]
        Write-Host "Stopping $key (PID: $pidToKill)..."
        
        $proc = Get-Process -Id $pidToKill -ErrorAction SilentlyContinue
        if ($proc) {
            # Taskkill /T kills the tree, /F forces it
            $cmd = "taskkill.exe"
            $args = "/PID", $pidToKill, "/T", "/F"
            $output = & $cmd $args 2>&1
            Write-Host "  -> Stopped." -ForegroundColor Green
        } else {
            Write-Host "  -> Already stopped." -ForegroundColor Yellow
        }
        $state.Remove($key)
    }
}

# Process any remaining services not in the predefined stop order
foreach ($key in @($state.Keys)) {
    $pidToKill = $state[$key]
    Write-Host "Stopping $key (PID: $pidToKill)..."
    
    $proc = Get-Process -Id $pidToKill -ErrorAction SilentlyContinue
    if ($proc) {
        $cmd = "taskkill.exe"
        $args = "/PID", $pidToKill, "/T", "/F"
        $output = & $cmd $args 2>&1
        Write-Host "  -> Stopped." -ForegroundColor Green
    } else {
        Write-Host "  -> Already stopped." -ForegroundColor Yellow
    }
}

# Clear state
Remove-Item $stateFile -Force -ErrorAction SilentlyContinue
Write-Host "========================================"
Write-Host " Shutdown completed."
Write-Host "========================================"
