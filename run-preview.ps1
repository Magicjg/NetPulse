$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
$projectPath = "C:\Users\sazke\Downloads\PROYECTS\NetPulse\NetPulse.App\NetPulse.App.csproj"
$previewPath = "C:\Users\sazke\AppData\Local\NetPulsePreview"
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

if (-not (Test-Path $dotnetPath)) {
    throw ".NET no esta instalado en la ruta esperada: $dotnetPath"
}

Get-Process NetPulse.App -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800

if (Test-Path $previewPath) {
    Remove-Item -Recurse -Force $previewPath
}

New-Item -ItemType Directory -Path $previewPath | Out-Null

& $dotnetPath publish $projectPath -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $previewPath

if ($LASTEXITCODE -ne 0) {
    throw "No se pudo publicar NetPulse."
}

$certificate = Get-OrCreateCodeSigningCertificate
Sign-PublishedFiles -TargetPath $previewPath -Certificate $certificate

Start-Process -FilePath (Join-Path $previewPath "NetPulse.App.exe")
