# Trigger Codemagic FREE ios-testflight workflow (mac_mini_m2).
# Requires one-time Codemagic signup + env group "asc" — see docs/codemagic-free-ios.md
# Never prints token values.
#
# Usage:
#   $env:CODEMAGIC_API_TOKEN = "<from Codemagic User settings>"
#   $env:CODEMAGIC_APP_ID    = "<Application settings → App ID>"
#   powershell -File mobile\scripts\trigger-codemagic-ios.ps1
# Optional: -Branch main  (default main)

param(
  [string]$Branch = "main",
  [string]$WorkflowId = "ios-testflight"
)

$ErrorActionPreference = "Stop"

$token = $env:CODEMAGIC_API_TOKEN
$appId = $env:CODEMAGIC_APP_ID

if (-not $token) {
  Write-Error "Missing CODEMAGIC_API_TOKEN (Codemagic → User settings → Integrations → API). Do one-time signup: docs/codemagic-free-ios.md"
}
if (-not $appId) {
  Write-Error "Missing CODEMAGIC_APP_ID (Codemagic Application settings). Do one-time signup: docs/codemagic-free-ios.md"
}

$body = @{
  appId      = $appId
  workflowId = $WorkflowId
  branch     = $Branch
} | ConvertTo-Json

Write-Host "CODEMAGIC_TRIGGER workflow=$WorkflowId branch=$Branch appIdLen=$($appId.Length) tokenLen=$($token.Length)"

$response = Invoke-RestMethod `
  -Method Post `
  -Uri "https://api.codemagic.io/builds" `
  -Headers @{
    "Content-Type" = "application/json"
    "x-auth-token" = $token
  } `
  -Body $body

Write-Host "CODEMAGIC_BUILD_STARTED buildId=$($response.buildId)"
Write-Host "Open: https://codemagic.io/app/$appId/build/$($response.buildId)"
