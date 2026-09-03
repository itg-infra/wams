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

# Run from backend/ so relative paths such as logs/ and storage/ resolve correctly.
Push-Location $PSScriptRoot
try {
    & dotnet run --project ".\src\WAMS.Api" --no-launch-profile
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
