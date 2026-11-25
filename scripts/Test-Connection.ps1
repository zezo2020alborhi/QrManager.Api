param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$AdminApiKey
)

$baseUrl = $ApiBaseUrl.TrimEnd('/')
$headers = @{ 'X-Admin-Key' = $AdminApiKey }

Write-Host "Testing health endpoint: $baseUrl/health"
$health = Invoke-RestMethod -Method Get -Uri "$baseUrl/health"
Write-Host "Health status: $($health.status) | Trace ID: $($health.traceId)"

Write-Host "Testing protected connection endpoint without printing the key..."
$connection = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/connection" -Headers $headers
Write-Host "API status: $($connection.status) | QR route: $($connection.publicQrPath) | Trace ID: $($connection.traceId)"

Write-Host "Connection check completed successfully."
