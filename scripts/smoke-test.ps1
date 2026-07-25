param(
    [string]$BaseUrl =
        "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

$BaseUrl =
    $BaseUrl.TrimEnd("/")

Write-Host "Checking SeatFlow API at $BaseUrl"

$root =
    Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/"

if ($root.status -ne "Running") {
    throw "Root endpoint returned an unexpected status."
}

$live =
    Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/health/live"

if ($live.status -ne "Healthy") {
    throw "Liveness health check failed."
}

$ready =
    Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/health/ready"

if ($ready.status -ne "Healthy") {
    throw "Readiness health check failed."
}

$catalog =
    Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/events?page=1&pageSize=20"

if ($catalog.page -ne 1) {
    throw "Event catalog returned an unexpected page."
}

if ($catalog.pageSize -ne 20) {
    throw "Event catalog returned an unexpected page size."
}

$result =
    [pscustomobject]@{
        ApplicationStatus =
            $root.status

        LivenessStatus =
            $live.status

        ReadinessStatus =
            $ready.status

        CatalogItems =
            @($catalog.items).Count

        CatalogTotalCount =
            $catalog.totalCount
    }

$result |
    Format-List

Write-Host "SeatFlow smoke test completed successfully."