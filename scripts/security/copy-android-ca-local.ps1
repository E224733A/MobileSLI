<#
.SYNOPSIS
Copie localement la CA publique Android MobileSLI sans versionner le certificat.

.DESCRIPTION
Ce script prépare le fichier attendu par network_security_config.xml :
Platforms\Android\Resources\raw\mobilesli_root_ca.crt

Il ne doit copier que le certificat public de la CA au format .crt.
Ne jamais utiliser ce script avec une clé privée, un PFX, un keystore ou un fichier PEM contenant une clé.
#>

[CmdletBinding()]
param(
    [string]$CertSource = "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\_certificats-locaux-ne-pas-commit\android\mobilesli_root_ca.crt",
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $CertSource)) {
    throw "Certificat introuvable : $CertSource"
}

$extension = [System.IO.Path]::GetExtension($CertSource).ToLowerInvariant()
$forbiddenExtensions = @(".key", ".pfx", ".p12", ".jks", ".keystore", ".pem")

if ($forbiddenExtensions -contains $extension) {
    throw "Fichier refusé : $CertSource. Ne jamais copier de clé privée, PFX, keystore ou PEM dans le projet mobile."
}

if ($extension -ne ".crt") {
    throw "Format refusé : $CertSource. Le fichier attendu est mobilesli_root_ca.crt."
}

$rawDirectory = Join-Path $ProjectRoot "Platforms\Android\Resources\raw"
$destination = Join-Path $rawDirectory "mobilesli_root_ca.crt"

New-Item -ItemType Directory -Path $rawDirectory -Force | Out-Null
Copy-Item -LiteralPath $CertSource -Destination $destination -Force

Write-Host "CA publique copiée localement : $destination"

Push-Location $ProjectRoot
try {
    $relativePath = "Platforms/Android/Resources/raw/mobilesli_root_ca.crt"
    $ignoreResult = git check-ignore -v -- $relativePath 2>$null

    if ([string]::IsNullOrWhiteSpace($ignoreResult)) {
        Write-Warning "Le certificat n'est pas ignoré par Git. Corriger .gitignore avant tout commit."
    }
    else {
        Write-Host "OK : le certificat est ignoré par Git."
        Write-Host $ignoreResult
    }
}
finally {
    Pop-Location
}
