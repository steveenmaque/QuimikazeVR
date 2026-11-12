[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ApkPath,
    [string]$ExpectedVersion = '1.2',
    [int]$ExpectedVersionCode = 3,
    [int]$ExpectedTargetSdk = 36,
    [string]$ExpectedPackage = 'com.labquimicaperu.laboratoriovr',
    [string]$ExpectedSignerCertificateSha256 = '20a038c2a78379520931f84cbcabbaffd1d7277cd660de5e7b00135499f10e49',
    [string]$AndroidSdkPath = 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK',
    [string]$JavaPath = 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe',
    [string]$ReportPath = ''
)

$ErrorActionPreference = 'Stop'
$repoPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrEmpty($ReportPath)) { $ReportPath = Join-Path $repoPath 'AutoSync/apk-validation.json' }
$reportFile = [IO.Path]::GetFullPath($ReportPath)
$releaseChecks = [Collections.Generic.List[object]]::new()
$validation = [ordered]@{
    utc=[DateTime]::UtcNow.ToString('o'); passed=$false; error=$null
    apk=$null; sha256=$null; package=$null; version=$null; versionCode=$null; targetSdk=$null
    checks=$null; scope='Inspección del APK, manifiesto, arquitecturas y firma; no ejecuta el juego en Android.'
}
function Add-ReleaseCheck([string]$Name,[bool]$Passed) {
    $releaseChecks.Add([ordered]@{name=$Name;passed=$Passed})
}
try {
    $apkFullPath = (Resolve-Path -LiteralPath $ApkPath).Path
    if (-not [IO.File]::Exists($apkFullPath)) { throw 'No existe un archivo APK para verificar.' }
    $validation.apk = [IO.Path]::GetFileName($apkFullPath)
    $validation.sha256 = (Get-FileHash -LiteralPath $apkFullPath -Algorithm SHA256).Hash
    $buildTools = Get-ChildItem -LiteralPath (Join-Path $AndroidSdkPath 'build-tools') -Directory |
        Where-Object { $_.Name -match '^\d+\.\d+\.\d+$' } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    if ($null -eq $buildTools) { throw 'Faltan Android SDK Build Tools.' }
    $aapt = Join-Path $buildTools.FullName 'aapt2.exe'
    $badging = (& $aapt dump badging $apkFullPath 2>&1) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'aapt2 no pudo leer el APK.' }
    $packageMatch = [regex]::Match($badging,"package: name='([^']+)' versionCode='([^']+)' versionName='([^']+)'")
    if (-not $packageMatch.Success) { throw 'No se pudo leer la identidad y versión del manifiesto.' }
    $validation.package=$packageMatch.Groups[1].Value
    $validation.versionCode=$packageMatch.Groups[2].Value
    $validation.version=$packageMatch.Groups[3].Value
    $targetMatch=[regex]::Match($badging,"targetSdkVersion:'([0-9]+)'")
    if ($targetMatch.Success) { $validation.targetSdk=$targetMatch.Groups[1].Value }
    Add-ReleaseCheck 'Paquete esperado' ($validation.package -eq $ExpectedPackage)
    Add-ReleaseCheck 'Versión nueva esperada' ($validation.version -eq $ExpectedVersion)
    Add-ReleaseCheck 'Código de versión nuevo' ($validation.versionCode -eq $ExpectedVersionCode.ToString())
    Add-ReleaseCheck 'SDK objetivo estable esperado' ($validation.targetSdk -eq $ExpectedTargetSdk.ToString())
    $manifest = (& $aapt dump xmltree $apkFullPath --file AndroidManifest.xml 2>&1) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo inspeccionar el manifiesto del APK.' }
    Add-ReleaseCheck 'Permiso INTERNET para LAN' ($manifest.Contains('android.permission.INTERNET'))
    Add-ReleaseCheck 'Proveedor de informes incluido' ($manifest.Contains('com.quimikaze.reports.ReportsProvider'))
    Add-Type -AssemblyName System.IO.Compression
    $archive = [IO.Compression.ZipFile]::OpenRead($apkFullPath)
    try {
        Add-ReleaseCheck 'IL2CPP ARM64' ($null -ne $archive.GetEntry('lib/arm64-v8a/libil2cpp.so'))
        Add-ReleaseCheck 'IL2CPP ARMv7' ($null -ne $archive.GetEntry('lib/armeabi-v7a/libil2cpp.so'))
    } finally { $archive.Dispose() }
    $signerJar = Join-Path $buildTools.FullName 'lib/apksigner.jar'
    $signature = (& $JavaPath -jar $signerJar verify --print-certs $apkFullPath 2>&1) -join "`n"
    Add-ReleaseCheck 'Firma APK válida' ($LASTEXITCODE -eq 0)
    $signer = [regex]::Match($signature,'Signer #1 certificate SHA-256 digest: ([0-9a-fA-F]+)')
    if ($signer.Success) { $validation.signerCertificateSha256=$signer.Groups[1].Value }
    Add-ReleaseCheck 'Firma compatible con la versión pública anterior' ($signer.Success -and $signer.Groups[1].Value -eq $ExpectedSignerCertificateSha256)
    $validation.passed = @($releaseChecks | Where-Object { -not $_.passed }).Count -eq 0
    if (-not $validation.passed) { $validation.error='APK rechazado: no cumple las condiciones de la versión solicitada.' }
} catch { $validation.error=$_.Exception.Message }
finally {
    $validation.checks=$releaseChecks.ToArray()
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($reportFile)) | Out-Null
    $json=$validation | ConvertTo-Json -Depth 6
    [IO.File]::WriteAllText($reportFile,$json,[Text.UTF8Encoding]::new($false))
    Write-Output $json
}
if (-not $validation.passed) { exit 1 }
