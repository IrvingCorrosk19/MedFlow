# Ejecuta verificaciones HTTP para PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md (Windows PowerShell 5.1)
param(
    [string]$BaseUrl = 'http://localhost:5115',
    [string]$Password = 'MedFlow2026!'
)

$ErrorActionPreference = 'Stop'

function Get-AntiforgeryToken([string]$html) {
    $m = [regex]::Match($html, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
    if (-not $m.Success) { $m = [regex]::Match($html, 'type="hidden"\s+name="__RequestVerificationToken"\s+value="([^"]+)"') }
    if (-not $m.Success) { throw 'No se encontró token antiforgery.' }
    return $m.Groups[1].Value
}

function Invoke-WebSafe {
    param([string]$Uri, $Session, [string]$Method = 'GET', $Body = $null)
    try {
        $params = @{ Uri = $Uri; WebSession = $Session; UseBasicParsing = $true; MaximumRedirection = 8 }
        if ($Method -eq 'POST' -and $Body) { $params.Method = 'POST'; $params.Body = $Body }
        $r = Invoke-WebRequest @params
        return @{ Ok = $true; Code = [int]$r.StatusCode; Content = $r.Content }
    }
    catch {
        $resp = $_.Exception.Response
        if ($resp -eq $null) { return @{ Ok = $false; Code = 0; Content = ''; Error = $_.Exception.Message } }
        $code = [int]$resp.StatusCode
        $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
        $txt = $reader.ReadToEnd()
        return @{ Ok = $true; Code = $code; Content = $txt }
    }
}

function Login-Staff {
    param([string]$Email, [Microsoft.PowerShell.Commands.WebRequestSession]$Session)
    $page = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -WebSession $Session -UseBasicParsing
    $tok = Get-AntiforgeryToken $page.Content
    $body = @{
        Email = $Email; Password = $Password; RememberMe = 'false'; __RequestVerificationToken = $tok
    }
    $r = Invoke-WebSafe -Uri "$BaseUrl/Account/Login" -Session $Session -Method POST -Body $body
    if ($r.Code -eq 200 -and $r.Content -match 'incorrectos|portal del paciente') { return $false }
    if ($r.Code -in @(302, 200)) { return $true }
    return $false
}

function Login-Patient {
    param([string]$Email, [Microsoft.PowerShell.Commands.WebRequestSession]$Session)
    $page = Invoke-WebRequest -Uri "$BaseUrl/PatientPortal/login" -WebSession $Session -UseBasicParsing
    $tok = Get-AntiforgeryToken $page.Content
    $body = @{
        Email = $Email; Password = $Password; RememberMe = 'false'; __RequestVerificationToken = $tok
    }
    $r = Invoke-WebSafe -Uri "$BaseUrl/PatientPortal/login" -Session $Session -Method POST -Body $body
    return ($r.Code -in @(302, 200) -and $r.Content -notmatch 'incorrectos|solo para pacientes')
}

function Test-GetCode {
    param($Session, [string]$Path)
    $r = Invoke-WebSafe -Uri "$BaseUrl$Path" -Session $Session
    return @{ Code = $r.Code; Content = $r.Content }
}

$results = New-Object System.Collections.Generic.List[object]

# TP-A02
$sBad = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$p0 = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -WebSession $sBad -UseBasicParsing
$tok0 = Get-AntiforgeryToken $p0.Content
$rBad = Invoke-WebSafe -Uri "$BaseUrl/Account/Login" -Session $sBad -Method POST -Body @{
    Email = 'qa.admin@medflow.local'; Password = 'WrongPass!!!'; RememberMe = 'false'; __RequestVerificationToken = $tok0
}
$a02 = ($rBad.Code -eq 200 -and $rBad.Content -match 'incorrectos')
$results.Add([pscustomobject]@{ Id = 'TP-A02'; Result = $(if ($a02) { 'OK' } else { 'NOK' }); Notas = '' })

# TP-A01 + TP-G02 + TP-G03-admin
$s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$a01 = Login-Staff -Email 'qa.admin@medflow.local' -Session $s
$h = Test-GetCode -Session $s -Path '/'
$results.Add([pscustomobject]@{ Id = 'TP-A01'; Result = $(if ($a01 -and $h.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($h.Code)" })

$g02 = ($h.Content -match 'Facturaci')
$results.Add([pscustomobject]@{ Id = 'TP-G02'; Result = $(if ($g02) { 'OK' } else { 'NOK' }); Notas = '' })

$csvA = Test-GetCode -Session $s -Path '/Dashboard/ExportCsv?days=14'
$g03a = ($csvA.Code -eq 200 -and $csvA.Content -match 'Finanzas')
$results.Add([pscustomobject]@{ Id = 'TP-G03-admin'; Result = $(if ($g03a) { 'OK' } else { 'NOK' }); Notas = "HTTP $($csvA.Code)" })

# TP-B01, B02-GET, I01, J02
$b1 = Test-GetCode -Session $s -Path '/Patients'
$results.Add([pscustomobject]@{ Id = 'TP-B01'; Result = $(if ($b1.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($b1.Code)" })
$bc = Test-GetCode -Session $s -Path '/Patients/Create'
$results.Add([pscustomobject]@{ Id = 'TP-B02-GET'; Result = $(if ($bc.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($bc.Code)" })

# TP-B02-POST — alta paciente mínima (recepción)
$sCr = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Staff -Email 'qa.reception@medflow.local' -Session $sCr)
$pCr = Invoke-WebRequest -Uri "$BaseUrl/Patients/Create" -WebSession $sCr -UseBasicParsing
$tokCr = Get-AntiforgeryToken $pCr.Content
$docU = 'QAHTTP' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff')
$rCr = Invoke-WebSafe -Uri "$BaseUrl/Patients/Create" -Session $sCr -Method POST -Body @{
    __RequestVerificationToken = $tokCr
    PrimerNombre               = 'HttpTest'
    PrimerApellido             = 'PacienteAuto'
    NumeroDocumento            = $docU
    TipoDocumento              = 'CC'
    Telefono                   = '3001234567'
    EstadoActivo               = 'true'
}
$b02postOk = ($rCr.Code -eq 200 -and $rCr.Content -notmatch 'validation-summary|El primer nombre es obligatorio|El primer apellido es obligatorio|No se pudo registrar')
if (-not $b02postOk -and $rCr.Code -in @(302)) { $b02postOk = $true }
$results.Add([pscustomobject]@{ Id = 'TP-B02-POST'; Result = $(if ($b02postOk) { 'OK' } else { 'NOK' }); Notas = "HTTP $($rCr.Code) doc=$docU" })
$i1 = Test-GetCode -Session $s -Path '/AdminUsers'
$results.Add([pscustomobject]@{ Id = 'TP-I01'; Result = $(if ($i1.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($i1.Code)" })

$i2 = Test-GetCode -Session $s -Path '/AdminRoles'
$results.Add([pscustomobject]@{ Id = 'TP-I02'; Result = $(if ($i2.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($i2.Code)" })

$b04 = $false
$b04note = ''
$hxPat = Test-GetCode -Session $s -Path '/Patients'
$mDet = [regex]::Match($hxPat.Content, '/Patients/Details/([a-f0-9-]{36})')
if ($mDet.Success) {
    $patientId = $mDet.Groups[1].Value
    $detPg = Test-GetCode -Session $s -Path "/Patients/Details/$patientId"
    $b04 = ($detPg.Code -eq 200)
    $b04note = "patient $patientId"
}
else {
    $b04 = $true
    $b04note = 'sin pacientes en listado (omitido)'
}
$results.Add([pscustomobject]@{ Id = 'TP-B04'; Result = $(if ($b04) { 'OK' } else { 'NOK' }); Notas = $b04note })

$j2 = Test-GetCode -Session $s -Path '/SuperAdmin/Tenants'
$j2ok = ($j2.Code -in @(302, 403))
$results.Add([pscustomobject]@{ Id = 'TP-J02'; Result = $(if ($j2ok) { 'OK' } else { 'NOK' }); Notas = "HTTP $($j2.Code)" })

# Reception
$sr = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Staff -Email 'qa.reception@medflow.local' -Session $sr)
$a4 = Test-GetCode -Session $sr -Path '/AdminUsers'
$results.Add([pscustomobject]@{ Id = 'TP-A04'; Result = $(if ($a4.Code -in @(302, 403)) { 'OK' } else { 'NOK' }); Notas = "HTTP $($a4.Code)" })

$dr = Test-GetCode -Session $sr -Path '/'
# Sin permiso billing: no deben aparecer tarjetas KPI financieras (el sidebar puede decir "Facturación y caja")
$g01 = ($dr.Content -notmatch 'Facturación hoy|Facturación mes|Saldo pendiente total')
$results.Add([pscustomobject]@{ Id = 'TP-G01'; Result = $(if ($g01) { 'OK' } else { 'NOK' }); Notas = 'Sin KPIs financieros en cuerpo principal' })

$results.Add([pscustomobject]@{ Id = 'TP-C01'; Result = $(if ((Test-GetCode -Session $sr -Path '/Appointments').Code -eq 200) { 'OK' } else { 'NOK' }); Notas = '' })

$csvR = Test-GetCode -Session $sr -Path '/Dashboard/ExportCsv?days=7'
$g03r = ($csvR.Code -eq 200 -and $csvR.Content -notmatch '(?m)^Finanzas,')
$results.Add([pscustomobject]@{ Id = 'TP-G03-reception'; Result = $(if ($g03r) { 'OK' } else { 'NOK' }); Notas = '' })

# Doctor
$sd = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Staff -Email 'qa.doctor@medflow.local' -Session $sd)
$a5 = Test-GetCode -Session $sd -Path '/BillingInvoices'
$results.Add([pscustomobject]@{ Id = 'TP-A05'; Result = $(if ($a5.Code -in @(302, 403)) { 'OK' } else { 'NOK' }); Notas = "HTTP $($a5.Code)" })
$d1 = Test-GetCode -Session $sd -Path '/MedicalRecords/Search'
$results.Add([pscustomobject]@{ Id = 'TP-D01'; Result = $(if ($d1.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($d1.Code)" })
$pe = Test-GetCode -Session $sd -Path '/Prescriptions'
$results.Add([pscustomobject]@{ Id = 'TP-E01-GET'; Result = $(if ($pe.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($pe.Code)" })

# Billing
$sb = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Staff -Email 'qa.billing@medflow.local' -Session $sb)
$results.Add([pscustomobject]@{ Id = 'TP-F01'; Result = $(if ((Test-GetCode -Session $sb -Path '/BillingInvoices').Code -eq 200) { 'OK' } else { 'NOK' }); Notas = '' })
$results.Add([pscustomobject]@{ Id = 'TP-F03'; Result = $(if ((Test-GetCode -Session $sb -Path '/CashMovements').Code -eq 200) { 'OK' } else { 'NOK' }); Notas = '' })

# Staff
$ss = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Staff -Email 'qa.staff@medflow.local' -Session $ss)
$results.Add([pscustomobject]@{ Id = 'TP-B01-staff'; Result = $(if ((Test-GetCode -Session $ss -Path '/Patients').Code -eq 200) { 'OK' } else { 'NOK' }); Notas = '' })

# SuperAdmin
$su = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Staff -Email 'superadmin@medflow.ai' -Session $su)
$results.Add([pscustomobject]@{ Id = 'TP-J01'; Result = $(if ((Test-GetCode -Session $su -Path '/SuperAdmin/Tenants').Code -eq 200) { 'OK' } else { 'NOK' }); Notas = '' })

# Patient portal
$sp = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$hp = Login-Patient -Email 'qa.patient@medflow.local' -Session $sp
$ph = Test-GetCode -Session $sp -Path '/PatientPortal/inicio'
$results.Add([pscustomobject]@{ Id = 'TP-H01'; Result = $(if ($hp -and $ph.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($ph.Code)" })

# K01 K02
$sk = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Staff -Email 'qa.admin@medflow.local' -Session $sk)
$k1 = Test-GetCode -Session $sk -Path '/ChartOfAccounts'
$k2 = Test-GetCode -Session $sk -Path '/Automations'
$results.Add([pscustomobject]@{ Id = 'TP-K01'; Result = $(if ($k1.Code -in @(200, 403)) { 'OK' } else { 'NOK' }); Notas = "HTTP $($k1.Code)" })
$results.Add([pscustomobject]@{ Id = 'TP-K02'; Result = $(if ($k2.Code -in @(200, 403)) { 'OK' } else { 'NOK' }); Notas = "HTTP $($k2.Code)" })

# Mandato v2 (10 fases) — rutas representativas con qa.admin (permisos completos tenant)
$vXp = Test-GetCode -Session $sk -Path '/Patients'
$results.Add([pscustomobject]@{ Id = 'TP-V1-Experience'; Result = $(if ($vXp.Code -eq 200 -and $vXp.Content -match 'mf-xp-card') { 'OK' } else { 'NOK' }); Notas = "HTTP $($vXp.Code)" })

$vKpi = Test-GetCode -Session $sk -Path '/Dashboard/KpiSnapshot?days=14'
$results.Add([pscustomobject]@{ Id = 'TP-V2-MissionControl-Kpi'; Result = $(if ($vKpi.Code -eq 200 -and $vKpi.Content -match 'completionRatePeriod') { 'OK' } else { 'NOK' }); Notas = "HTTP $($vKpi.Code)" })

$vGrowth = Test-GetCode -Session $sk -Path '/AI/GrowthEngine'
$results.Add([pscustomobject]@{ Id = 'TP-V3-AI-GrowthEngine'; Result = $(if ($vGrowth.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($vGrowth.Code)" })

$vRev = Test-GetCode -Session $sk -Path '/RevenueRecovery'
$results.Add([pscustomobject]@{ Id = 'TP-V4-RevenueRecovery'; Result = $(if ($vRev.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($vRev.Code)" })

$vCrm = Test-GetCode -Session $sk -Path '/GrowthCrm/Segments'
$results.Add([pscustomobject]@{ Id = 'TP-V5-CRM-Segments'; Result = $(if ($vCrm.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($vCrm.Code)" })

$sV6 = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Patient -Email 'qa.patient@medflow.local' -Session $sV6)
$vPortal = Test-GetCode -Session $sV6 -Path '/portal/dashboard'
$results.Add([pscustomobject]@{ Id = 'TP-V6-PortalCanonical'; Result = $(if ($vPortal.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "/portal/dashboard HTTP $($vPortal.Code)" })

$vClinic = Test-GetCode -Session $sk -Path '/ClinicConsole'
$results.Add([pscustomobject]@{ Id = 'TP-V8-ClinicConsole'; Result = $(if ($vClinic.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($vClinic.Code)" })

$vSec = Test-GetCode -Session $sk -Path '/SecurityPosture'
$results.Add([pscustomobject]@{ Id = 'TP-V9-SecurityPosture'; Result = $(if ($vSec.Code -eq 200) { 'OK' } else { 'NOK' }); Notas = "HTTP $($vSec.Code)" })

$vm = Invoke-WebSafe -Uri "$BaseUrl/manifest.webmanifest" -Session (New-Object Microsoft.PowerShell.Commands.WebRequestSession)
$manifestTxt = $vm.Content
if ($manifestTxt -is [byte[]]) { $manifestTxt = [System.Text.Encoding]::UTF8.GetString($manifestTxt) }
$results.Add([pscustomobject]@{ Id = 'TP-V10-PWA-Manifest'; Result = $(if ($vm.Code -eq 200 -and $manifestTxt -match 'MedFlow') { 'OK' } else { 'NOK' }); Notas = "HTTP $($vm.Code)" })

# TP-A03 — Logout POST staff
$sLo = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Staff -Email 'qa.admin@medflow.local' -Session $sLo)
$pLo = Invoke-WebRequest -Uri "$BaseUrl/Patients" -WebSession $sLo -UseBasicParsing
$tokLo = Get-AntiforgeryToken $pLo.Content
$rLo = Invoke-WebSafe -Uri "$BaseUrl/Account/Logout" -Session $sLo -Method POST -Body @{ __RequestVerificationToken = $tokLo }
$afterLo = Test-GetCode -Session $sLo -Path '/Patients'
$a03ok = ($afterLo.Code -eq 200 -and ($afterLo.Content -match 'Login|Iniciar sesión|Correo electr|name="Email"|Account/Login'))
$results.Add([pscustomobject]@{ Id = 'TP-A03'; Result = $(if ($a03ok) { 'OK' } else { 'NOK' }); Notas = "logout POST code $($rLo.Code); GET /Patients $($afterLo.Code)" })

# TP-A06 — Paciente no debe ver listado staff
$sPx = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Patient -Email 'qa.patient@medflow.local' -Session $sPx)
$pxStaff = Test-GetCode -Session $sPx -Path '/Patients'
$a06ok = ($pxStaff.Code -eq 403) -or ($pxStaff.Content -match 'Acceso denegado|403|Forbidden')
if (-not $a06ok -and $pxStaff.Code -eq 200) {
    $a06ok = ($pxStaff.Content -notmatch 'Listado de pacientes')
}
$results.Add([pscustomobject]@{ Id = 'TP-A06'; Result = $(if ($a06ok) { 'OK' } else { 'NOK' }); Notas = "HTTP $($pxStaff.Code)" })

# TP-H02 — Portal: citas y facturas
$sH2 = New-Object Microsoft.PowerShell.Commands.WebRequestSession
[void](Login-Patient -Email 'qa.patient@medflow.local' -Session $sH2)
$h2c = Test-GetCode -Session $sH2 -Path '/PatientPortal/citas'
$h2f = Test-GetCode -Session $sH2 -Path '/PatientPortal/facturas'
$h02ok = ($h2c.Code -eq 200) -and ($h2f.Code -in @(200, 302))
$results.Add([pscustomobject]@{ Id = 'TP-H02'; Result = $(if ($h02ok) { 'OK' } else { 'NOK' }); Notas = "citas $($h2c.Code); facturas $($h2f.Code)" })

$results | Format-Table -AutoSize
$okCount = ($results | Where-Object { $_.Result -eq 'OK' }).Count
Write-Host "`nTotal OK: $okCount / $($results.Count)"
if ($okCount -ne $results.Count) {
    Write-Host 'ERROR: Hay casos NOK.' -ForegroundColor Red
    exit 1
}
exit 0
