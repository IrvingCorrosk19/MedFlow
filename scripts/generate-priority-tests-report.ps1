# Ejecuta PriorityClinicalFlowFunctionalTests y genera informe HTML en wwwroot/qa (sirve desde MedFlow en /qa/...)
param(
    [switch]$OpenBrowser,
    [string]$BaseUrl = 'http://localhost:5115'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$outDir = Join-Path $repoRoot 'artifacts\test'
$trxFile = Join-Path $outDir 'priority-tests.trx'
$htmlOut = Join-Path $repoRoot 'src\MedFlow.Web\wwwroot\qa\priority-functional-tests-report.html'

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path $htmlOut) | Out-Null

Push-Location $repoRoot
try {
    & dotnet test (
        'tests\MedFlow.UnitTests\MedFlow.UnitTests.csproj',
        '-c', 'Release',
        '--filter', 'FullyQualifiedName~PriorityClinicalFlowFunctionalTests',
        '--logger', "trx;LogFileName=priority-tests.trx",
        '--results-directory', $outDir
    )
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "dotnet test terminó con código $LASTEXITCODE; el informe reflejará el resultado."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path $trxFile)) {
    throw "No se generó el archivo TRX: $trxFile"
}

[xml]$trx = Get-Content -LiteralPath $trxFile -Encoding UTF8
$ns = @{ t = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010' }
$rows = Select-Xml -Xml $trx.TestRun -XPath '//t:UnitTestResult' -Namespace $ns | ForEach-Object {
    $n = $_.Node
    [pscustomobject]@{
        Name     = $n.testName -replace '^MedFlow\.UnitTests\.PriorityClinicalFlowFunctionalTests\.', ''
        Outcome  = $n.outcome
        Duration = $n.duration
    }
}

$sorted = $rows | Sort-Object Name
$passed = ($sorted | Where-Object { $_.Outcome -eq 'Passed' }).Count
$failed = ($sorted | Where-Object { $_.Outcome -ne 'Passed' }).Count
$time = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

$tbody = ($sorted | ForEach-Object {
    $badge = if ($_.Outcome -eq 'Passed') {
        '<span class="ok">OK</span>'
    }
    else {
        '<span class="fail">' + [System.Net.WebUtility]::HtmlEncode($_.Outcome) + '</span>'
    }
    $nameEnc = [System.Net.WebUtility]::HtmlEncode($_.Name)
    "<tr><td><code>$nameEnc</code></td><td>$badge</td><td>$($_.Duration)</td></tr>"
}) -join "`n"

$html = @"
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width, initial-scale=1"/>
  <title>MedFlow - Pruebas funcionales prioritarias</title>
  <style>
    :root { --bg:#0f172a; --card:#1e293b; --text:#e2e8f0; --muted:#94a3b8; --ok:#22c55e; --fail:#ef4444; }
    body { font-family:system-ui,sans-serif; background:var(--bg); color:var(--text); margin:0; padding:2rem; }
    .wrap { max-width:960px; margin:0 auto; }
    h1 { font-size:1.35rem; font-weight:600; margin:0 0 .5rem; }
    .meta { color:var(--muted); font-size:.875rem; margin-bottom:1.25rem; }
    .card { background:var(--card); border-radius:12px; padding:1.25rem 1.5rem; box-shadow:0 4px 24px rgba(0,0,0,.35); }
    table { width:100%; border-collapse:collapse; font-size:.875rem; }
    th { text-align:left; padding:.65rem .5rem; border-bottom:1px solid #334155; color:var(--muted); font-weight:600; }
    td { padding:.65rem .5rem; border-bottom:1px solid #334155; vertical-align:middle; }
    code { font-size:.8rem; word-break:break-all; }
    .ok { color:var(--ok); font-weight:600; }
    .fail { color:var(--fail); font-weight:600; }
    .sum { margin-top:1rem; font-size:.9rem; }
    a { color:#38bdf8; }
  </style>
</head>
<body>
  <div class="wrap">
    <h1>Pruebas funcionales prioritarias</h1>
    <p class="meta">Clase <code>PriorityClinicalFlowFunctionalTests</code> | Generado: $time | Total: $($sorted.Count) | Pasaron: $passed | Fallaron: $failed</p>
    <div class="card">
      <table>
        <thead><tr><th>Prueba</th><th>Resultado</th><th>Duracion</th></tr></thead>
        <tbody>
$tbody
        </tbody>
      </table>
      <p class="sum">Actualizar: ejecutar <code>scripts\generate-priority-tests-report.ps1</code> desde la raiz del repo. Con MedFlow en marcha: <a href="${BaseUrl}/qa/priority-functional-tests-report.html">abrir por HTTP</a>.</p>
    </div>
  </div>
</body>
</html>
"@

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($htmlOut, $html, $utf8NoBom)
Write-Host "Informe escrito: $htmlOut"

if ($OpenBrowser) {
    $localUri = [Uri]::new((Resolve-Path $htmlOut))
    Start-Process $localUri.AbsoluteUri
}
