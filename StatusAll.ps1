$ErrorActionPreference = "Stop"

$services = @(
    @{ Name = "UserService"; Port = 5298 }
    @{ Name = "ProductService"; Port = 5108 }
    @{ Name = "OrderService"; Port = 5032 }
    @{ Name = "PaymentService"; Port = 5185 }
    @{ Name = "ApiGateway"; Port = 5000 }
    @{ Name = "Frontend"; Port = 5173 }
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
Write-Host " ECOMMERCE UC3 - STATUS"
Write-Host "========================================"

foreach ($svc in $services) {
    $isUp = Test-Port -Port $svc.Port
    $status = if ($isUp) { "[RUNNING]" } else { "[STOPPED]" }
    $color = if ($isUp) { "Green" } else { "DarkGray" }
    
    $msg = "{0,-15} :{1,-5} {2}" -f $svc.Name, $svc.Port, $status
    Write-Host $msg -ForegroundColor $color
}
Write-Host "========================================"
