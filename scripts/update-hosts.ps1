param(
    [string]$HostsPath = "$env:WINDIR\System32\drivers\etc\hosts",
    [string]$IpAddress = "127.0.0.1"
)

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Run this script from an elevated PowerShell window (Run as Administrator)."
    exit 1
}

$start = '# BEGIN aspnetrun-microservices'
$end = '# END aspnetrun-microservices'
$backup = "$HostsPath.bak.$((Get-Date).ToString('yyyyMMddHHmmss'))"

Copy-Item -LiteralPath $HostsPath -Destination $backup -Force

$content = Get-Content -LiteralPath $HostsPath -Raw
$pattern = [regex]::Escape($start) + '.*?' + [regex]::Escape($end) + "\r?\n?"
$content = [regex]::Replace($content, $pattern, '', [System.Text.RegularExpressions.RegexOptions]::Singleline)

$aliases = @(
    'aspnetrun.local',
    'aspire.aspnetrun.local',
    'product.aspnetrun.local',
    'basket.aspnetrun.local',
    'discount.aspnetrun.local',
    'opensearch.aspnetrun.local',
    'opensearch.dashboard.aspnetrun.local',
    'logging.aspnetrun.local',
    'discount.grpc.aspnetrun.local',
    'ordering.aspnetrun.local',
    'gateway.aspnetrun.local',
    'aggregator.aspnetrun.local',
    'rabbitmq.aspnetrun.local',
    'pgadmin.aspnetrun.local',
    'portainer.aspnetrun.local'
)

$blockLines = @($start)
$blockLines += $aliases | ForEach-Object { "$IpAddress $_" }
$blockLines += $end
$block = ($blockLines -join "`r`n") + "`r`n"

$newContent = (($content.TrimEnd() + "`r`n`r`n" + $block).TrimEnd() + "`r`n")
Set-Content -LiteralPath $HostsPath -Value $newContent -Encoding ASCII

Write-Host "Updated hosts file: $HostsPath"
Write-Host "Backup created: $backup"
