Set-StrictMode -Version Latest

dotnet tool install --global GitVersion.Tool --version 5.5.1

$gitVersionJson = dotnet-gitversion  | ConvertFrom-Json

Write-Host "##vso[task.setvariable variable=semVer]$($gitVersionJson.semVer)"
Write-Host "##vso[task.setvariable variable=informationalVersion]$($gitVersionJson.informationalVersion)"
Write-Host "##vso[task.setvariable variable=majorMinorPatch]$($gitVersionJson.majorMinorPatch)"
Write-Host "##vso[task.setvariable variable=nuGetVersion]$($gitVersionJson.semVer)"
Write-Host "##vso[task.setvariable variable=assemblySemVer]$($gitVersionJson.assemblySemVer)"
Write-Host "##vso[task.setvariable variable=assemblySemFileVer]$($gitVersionJson.assemblySemFileVer)"