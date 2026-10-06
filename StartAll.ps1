$ErrorActionPreference = "Stop"

$solutionDir = $PSScriptRoot
$runtimeDir = Join-Path $solutionDir ".runtime"
if (-not (Test-Path $runtimeDir)) {
    New-Item -ItemType Directory -Path $runtimeDir | Out-Null
}

$stateFile = Join-Path $runtimeDir "services.json"

# Read existing state if any
$state = @{}
if (Test-Path $stateFile) {
    try {
        $content = Get-Content $stateFile -Raw
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            $parsed = ConvertFrom-Json $content
            if ($parsed -ne $null) {
                foreach ($prop in $parsed.psobject.properties) {
                    $state[$prop.Name] = $prop.Value
                }
            }
        }
    } catch {
        $state = @{}
    }
}

$services = @(
    @{
        Name = "UserService"
        Path = "UserService\UserService.API"
        Command = "dotnet run"
        Port = 5298
    },
    @{
        Name = "ProductService"
        Path = "ProductService\ProductService.API"
        Command = "dotnet run"
        Port = 5108
    },
    @{
        Name = "OrderService"
        Path = "OrderService\OrderService.API"
        Command = "dotnet run"
        Port = 5032
    },
    @{
        Name = "PaymentService"
        Path = "PaymentService\PaymentService.API"
        Command = "dotnet run"
        Port = 5185
    },
    @{
        Name = "ApiGateway"
        Path = "ApiGateway"
        Command = "dotnet run"
        Port = 5000
    },
    @{
        Name = "Frontend"
        Path = "frontend"
        Command = "npm run dev"
        Port = 5173
    }
)

function Test-Port {
    param([int]$Port)
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $async = $tcp.BeginConnect("127.0.0.1", $Port, $null, $null)
        $wait = $async.AsyncWaitHandle.WaitOne([timespan]::FromMilliseconds(200))
        if ($wait) {
            $tcp.EndConnect($async)
            $tcp.Close()
            return $true
        }
        $tcp.Close()
        return $false
    } catch {
        return $false
    }
}

Write-Host "========================================"
Write-Host " ECOMMERCE UC3 - STARTUP"
Write-Host "========================================"

foreach ($svc in $services) {
    $svcName = $svc.Name
    $svcPath = Join-Path $solutionDir $svc.Path
    $svcCmd = $svc.Command
    $svcPort = $svc.Port

    Write-Host "Starting $svcName on port $svcPort..."
    
    if (Test-Port -Port $svcPort) {
        Write-Host "  -> Port $svcPort is already in use. Skipping start." -ForegroundColor Yellow
        $svc.Status = "PORT ALREADY IN USE"
        continue
    }

    # Start the process in a new window
    # We pass window title to make it obvious
    $title = "ECommerce UC3 - $svcName"
    $scriptBlock = "
        `$host.ui.RawUI.WindowTitle = '$title'
        Set-Location -Path '$svcPath'
        $svcCmd
        Write-Host '`nPress any key to exit...'
        `$null = `$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    "
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($scriptBlock))
    
    $proc = Start-Process powershell -ArgumentList "-NoProfile","-NoExit","-EncodedCommand",$encoded -WindowStyle Normal -PassThru

    $state[$svcName] = $proc.Id
    $svc.Status = "PROCESS STARTED"
    Start-Sleep -Milliseconds 1500
}

# Save state
$state | ConvertTo-Json | Set-Content $stateFile

# Wait for services to be ready
Write-Host "`nWaiting for services to become available..."
foreach ($svc in $services) {
    $svcName = $svc.Name
    $svcPort = $svc.Port
    if ($svc.Status -eq "PORT ALREADY IN USE") { continue }
    
    $ready = $false
    for ($i = 0; $i -lt 15; $i++) {
        if (Test-Port -Port $svcPort) {
            $ready = $true
            break
        }
        Start-Sleep -Seconds 1
    }
    
    if ($ready) {
        $svc.Status = "SERVICE RESPONDING"
    } else {
        $svc.Status = "STARTUP FAILED"
    }
}

Write-Host "`n========================================"
Write-Host " STARTUP SUMMARY"
Write-Host "========================================"
foreach ($svc in $services) {
    $status = $svc.Status
    $color = "White"
    if ($status -eq "SERVICE RESPONDING" -or $status -eq "PORT ALREADY IN USE") { $color = "Green" }
    elseif ($status -eq "PROCESS STARTED") { $color = "Yellow" }
    else { $color = "Red" }
    
    $msg = "[{0}] {1,-15} http://localhost:{2}" -f $status, $svc.Name, $svc.Port
    Write-Host $msg -ForegroundColor $color
}
Write-Host "========================================"
Write-Host " All services startup completed."
Write-Host "========================================"
Write-Host "Frontend: http://localhost:5173" -ForegroundColor Cyan
Write-Host "Gateway:  http://localhost:5000" -ForegroundColor Cyan
