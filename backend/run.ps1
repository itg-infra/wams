param(
    [string]$EnvFile = (Join-Path $PSScriptRoot ".env")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $EnvFile -PathType Leaf)) {
    throw "Environment file not found: $EnvFile"
}

# Load KEY=VALUE entries from the dotenv file into this process.
# The child dotnet process inherits these values.
Get-Content -LiteralPath $EnvFile | ForEach-Object {
    $line = $_.Trim()

    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
        return
    }

    $parts = $line -split "=", 2
    if ($parts.Count -ne 2) {
        return
    }

    $key = $parts[0].Trim()
    $value = $parts[1].Trim()

    if ([string]::IsNullOrWhiteSpace($key)) {
        return
    }

    # Support quoted dotenv values such as Name="WAMS System".
    if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
        $value = $value.Substring(1, $value.Length - 2)
    }

    Set-Item -Path "Env:$key" -Value $value
}

# Keep generated production files inside the existing repository folder.
$publishDir = Join-Path $PSScriptRoot "publish"

Write-Host "Publishing WAMS API to $publishDir..."
& dotnet publish (Join-Path $PSScriptRoot "src\WAMS.Api\WAMS.Api.csproj") `
    --configuration Release `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

# Run from publish/ so appsettings.json and all published files are resolved correctly.
Push-Location $publishDir
try {
    & dotnet ".\WAMS.Api.dll"
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
