$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
$projectPath = "C:\Users\sazke\Downloads\PROYECTS\NetPulse\NetPulse.App\NetPulse.App.csproj"
$outputPath = "C:\Users\sazke\Downloads\PROYECTS\NetPulse\dist\NetPulse-win-x64"
$zipPath = "C:\Users\sazke\Downloads\PROYECTS\NetPulse\dist\NetPulse-win-x64.zip"
$certificateSubject = "CN=NetPulse Local Dev"

function Get-OrCreateCodeSigningCertificate {
    $existingCertificate = Get-ChildItem "Cert:\CurrentUser\My" |
        Where-Object { $_.Subject -eq $certificateSubject } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if ($existingCertificate) {
        return $existingCertificate
    }

    $newCertificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $certificateSubject `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -HashAlgorithm SHA256

    $certificateExportPath = Join-Path $env:TEMP "NetPulseLocalDev.cer"
    Export-Certificate -Cert $newCertificate -FilePath $certificateExportPath | Out-Null
    Import-Certificate -FilePath $certificateExportPath -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
    Import-Certificate -FilePath $certificateExportPath -CertStoreLocation "Cert:\CurrentUser\TrustedPublisher" | Out-Null

    return $newCertificate
}

function Sign-PublishedFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetPath,
        [Parameter(Mandatory = $true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    )

    Get-ChildItem $TargetPath -Recurse -Include *.exe,*.dll |
        ForEach-Object {
            Set-AuthenticodeSignature -FilePath $_.FullName -Certificate $Certificate | Out-Null
        }
}

function Write-TrustHelper {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetPath,
        [Parameter(Mandatory = $true)]
        [string]$CertificateFileName
    )

    $scriptPath = Join-Path $TargetPath "trust-local-signature.ps1"
    $scriptContent = @'
$certificatePath = Join-Path $PSScriptRoot "__CERT_FILE__"

if (-not (Test-Path $certificatePath)) {
    throw "No se encontro el certificado local: $certificatePath"
}

Import-Certificate -FilePath $certificatePath -CertStoreLocation 'Cert:\CurrentUser\Root' | Out-Null
Import-Certificate -FilePath $certificatePath -CertStoreLocation 'Cert:\CurrentUser\TrustedPublisher' | Out-Null

Write-Host ""
Write-Host "Certificado importado en CurrentUser\Root y CurrentUser\TrustedPublisher." -ForegroundColor Green
Write-Host "Ya puedes probar NetPulse.App.exe en esta maquina." -ForegroundColor Green
'@

    $scriptContent = $scriptContent.Replace("__CERT_FILE__", $CertificateFileName)

    Set-Content -Path $scriptPath -Value $scriptContent -Encoding ASCII
}

if (-not (Test-Path $dotnetPath)) {
    throw ".NET no esta instalado en la ruta esperada: $dotnetPath"
}

if (Test-Path $outputPath) {
    Remove-Item -Recurse -Force $outputPath
}

if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

New-Item -ItemType Directory -Path $outputPath | Out-Null

& $dotnetPath publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $outputPath

if ($LASTEXITCODE -ne 0) {
    throw "No se pudo publicar el release de NetPulse."
}

$certificate = Get-OrCreateCodeSigningCertificate
Sign-PublishedFiles -TargetPath $outputPath -Certificate $certificate
$certificateExportPath = Join-Path $outputPath "NetPulseLocalDev.cer"
Export-Certificate -Cert $certificate -FilePath $certificateExportPath | Out-Null
Write-TrustHelper -TargetPath $outputPath -CertificateFileName "NetPulseLocalDev.cer"
Compress-Archive -Path (Join-Path $outputPath '*') -DestinationPath $zipPath

Write-Host ""
Write-Host "Release listo en:" -ForegroundColor Cyan
Write-Host $outputPath -ForegroundColor Green
Write-Host ""
Write-Host "Ejecutable:" -ForegroundColor Cyan
Write-Host (Join-Path $outputPath "NetPulse.App.exe") -ForegroundColor Green
Write-Host ""
Write-Host "Zip:" -ForegroundColor Cyan
Write-Host $zipPath -ForegroundColor Green
