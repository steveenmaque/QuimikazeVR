[CmdletBinding()]
param(
    [ValidateSet('all', 'education', 'content', 'lan')]
    [string]$Suite = 'all',
    [string]$Output = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrEmpty($Output)) { $Output = Join-Path $repoRoot 'AutoSync\logic_results.json' }
$Output = [IO.Path]::GetFullPath($Output)
[xml]$project = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'LogicHarness.csproj') -Raw
$sourcePaths = [Collections.Generic.List[string]]::new()
$sourcePaths.Add((Join-Path $PSScriptRoot 'GlobalUsings.cs'))
foreach ($item in $project.Project.ItemGroup.Compile) {
    if ($item.Include -ne 'Program.cs') { $sourcePaths.Add([IO.Path]::GetFullPath((Join-Path $PSScriptRoot $item.Include))) }
}

# Compilación dinámica normal de fuentes propias. No carga DLL/EXE del build ni cambia directivas.
# Los adaptadores son exclusivos de este harness; no se incorporan al juego.
Add-Type -Path $sourcePaths.ToArray() -CompilerOptions '/langversion:latest', '/nowarn:0649,0067' -ErrorAction Stop
[UnityEngine.Resources]::Root = Join-Path $repoRoot 'Assets\Resources'
$results = [Collections.Generic.List[object]]::new()
$failedSuites = 0
$totalChecks = 0
$testNames = if ($Suite -eq 'all') { @('education', 'content', 'lan') } else { @($Suite) }
foreach ($name in $testNames) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        $checkCount = switch ($name) {
            'education' { [LabQuimicaVR.EditorTools.EducationValidation]::Run() }
            'content' { [LabQuimicaVR.EditorTools.ContentInputValidation]::Run() }
            'lan' {
                [LabQuimicaVR.EditorTools.LanValidation]::Run()
                $field = [LabQuimicaVR.EditorTools.LanValidation].GetField('checks', [Reflection.BindingFlags]'NonPublic,Static')
                [int]$field.GetValue($null)
            }
        }
        $totalChecks += $checkCount
        $results.Add([ordered]@{ suite = $name; passed = $true; checks = $checkCount; elapsedMs = $timer.ElapsedMilliseconds })
    }
    catch {
        $failedSuites++
        $results.Add([ordered]@{ suite = $name; passed = $false; elapsedMs = $timer.ElapsedMilliseconds; exception = $_.Exception.ToString() })
        Write-Warning "Fallo en $name : $($_.Exception.Message)"
    }
}
$sourceHashes = [ordered]@{}
foreach ($path in $sourcePaths) {
    $relative = [IO.Path]::GetRelativePath($repoRoot, $path).Replace('\', '/')
    $sourceHashes[$relative] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
}
$report = [ordered]@{
    harness = 'QuimikazeVR LogicHarness (PowerShell Add-Type in-process)'
    utc = [DateTime]::UtcNow.ToString('o')
    runtime = [Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    powershell = $PSVersionTable.PSVersion.ToString()
    platform = [Runtime.InteropServices.RuntimeInformation]::OSDescription
    executed = $true
    passed = ($failedSuites -eq 0)
    checks = $totalChecks
    failures = $failedSuites
    suites = $results.ToArray()
    scope = @('Fuentes C# de producción compiladas directamente sin copiar algoritmos', 'Datos TXT reales del repositorio', 'Sockets TCP reales IPv4 loopback', 'Funciones puras de normalización e integración del eje vertical', 'Persistencia lógica JSON con adaptador System.Text.Json')
    limitations = @('NO ejecuta Unity ni IL2CPP', 'NO verifica gráficos, escenas, sensores ni mando físico', 'NO prueba Wi-Fi/hotspot ni Android', 'Resources de imagen verifica archivo y firma PNG/JPEG: NO decodificación/importación de textura Unity', 'PlayerPrefs en memoria: NO verifica almacenamiento Android ni preferencias de Unity', 'System.Text.Json sustituye JsonUtility: serialización nativa Unity requiere validación adicional', 'Invariantes de contenido NO validan científicamente cada afirmación de los 118 elementos')
    resourceReads = [ordered]@{ textFiles = [UnityEngine.Resources]::TextReads; imageFileChecks = [UnityEngine.Resources]::ImageFileChecks }
    sourceSha256 = $sourceHashes
}
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Output)) | Out-Null
$json = $report | ConvertTo-Json -Depth 12
[IO.File]::WriteAllText($Output, $json, [Text.UTF8Encoding]::new($false))
Write-Output $json
Write-Host "Informe: $Output"
if ($failedSuites -gt 0) { exit 1 }
