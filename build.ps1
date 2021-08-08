param
(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Command
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targetName = (Get-ChildItem -Path "$PSScriptRoot\src" -Recurse -Filter *.csproj | select -First 1).Basename
$distDir = "$PSScriptRoot\dist"

function removePath($path)
{
    while ($true)
    {
        if (!(Test-Path $path))
        {
            return
        }

        Write-Host "Deleting $path"
        try
        {
            Remove-Item -Recurse $path
            break
        }
        catch
        {
            Write-Host "Could not remove $path, will retry"
            Start-Sleep 3
        }
    }
}

function getInstallDir
{
    Write-Host "Looking for RimWorld installation..."
    $installSubDir = "Steam\SteamApps\common\RimWorld"
    $installDir = "$(${Env:ProgramFiles(x86)})\$($installSubDir)"
    Write-Host "Checking $installDir"
    if (Test-Path $installDir)
    {
        return $installDir
    }

    $installDir = "$($Env:ProgramFiles)\$($installSubDir)"
    Write-Host "Checking $installDir"
    if (Test-Path $installDir)
    {
        return $installDir
    }

    Write-Host "RimWorld not found"
    return $null
}

$installDir = getInstallDir

function getProjectDir
{
    return "$PSScriptRoot\src\$targetName"
}

$assemblyInfoFile = "$(getProjectDir)\properties\AssemblyInfo.cs"

function getGameVersion
{
    $gameVersionFile = "$installDir\Version.txt"
    $gameVersionWithRev = Get-Content $gameVersionFile
    $version = [version] ($gameVersionWithRev.Split(" "))[0]

    return "$($version.Major).$($version.Minor)"
}

function updateToGameVersion
{
    if (!$installDir)
    {
        Write-Host -ForegroundColor Red `
            "Rimworld installation not found; not setting game version."

        return
    }

    $gameVersion = getGameVersion

    $content = Get-Content -Raw $assemblyInfoFile
    $newContent = $content -replace '"\d+\.\d+(\.\d+\.\d+")', "`"$gameVersion`$1"

    if ($newContent -eq $content)
    {
        return
    }
    Set-Content -Encoding UTF8 -Path $assemblyInfoFile $newContent
}

function copyDependencies
{
    $thirdpartyDir = "$PSScriptRoot\ThirdParty"
    if (Test-Path "$thirdpartyDir\*.dll")
    {
        return
    }

    if (!$installDir)
    {
        Write-Host -ForegroundColor Red `
            "Rimworld installation not found; see Readme for how to set up pre-requisites manually."

        exit 1
    }

    $depsDir = "$installDir\RimWorldWin64_Data\Managed"
    Write-Host "Copying dependencies from installation directory"
    if (!(Test-Path $thirdpartyDir)) { mkdir $thirdpartyDir | Out-Null }
    Copy-Item -Force "$depsDir\Unity*.dll" "$thirdpartyDir\"
    Copy-Item -Force "$depsDir\Assembly-CSharp.dll" "$thirdpartyDir\"
}

function doPreBuild
{
    removePath $distDir
    copyDependencies
    updateToGameVersion
}

function doPostBuild
{
    $distTargetDir = "$distDir\$targetName"
    removePath $distDir

    $targetDir = "$(getProjectDir)\bin\Release"
    $targetPath = "$targetDir\$targetName.dll"

    $distAssemblyDir = "$distTargetDir\$(getGameVersion)\Assemblies"
    mkdir $distAssemblyDir | Out-Null

    Copy-Item -Recurse -Force "$PSScriptRoot\mod-structure\*" $distTargetDir
    Copy-Item -Force $targetPath $distAssemblyDir

    $modStructureAssemblyLocation = "$PSScriptRoot\mod-structure\$(getGameVersion)\Assemblies"
    if (!(Test-Path $modStructureAssemblyLocation))
    {
        mkdir $modStructureAssemblyLocation | Out-Null
    }
    Copy-Item -Force $targetPath $modStructureAssemblyLocation

    Write-Host "Creating distro package"
    $content = Get-Content -Raw $assemblyInfoFile
    if (!($content -match '"(\d+\.\d+\.\d+\.\d+)"'))
    {
        throw "Version info not found in $assemblyInfoFile"
    }

    $version = $matches[1]
    $distZip = "$distDir\$targetName.$version.zip"
    removePath $distZip
    $sevenZip = "$PSScriptRoot\7z.exe"
    & $sevenZip a -mx=9 "$distZip" "$distDir\*"
    if ($LASTEXITCODE -ne 0)
    {
        throw "7zip command failed"
    }

    Write-Host "Created $distZip"


    if (!$installDir)
    {
        Write-Host -ForegroundColor Yellow `
            "No Steam installation found, build will not be published"

        return
    }

    $modsDir = "$installDir\Mods"
    $modDir = "$modsDir\$targetName"
    removePath $modDir

    Write-Host "Copying mod to $modDir"
    Copy-Item -Recurse -Force -Exclude *.zip "$distDir\*" $modsDir
}

& $Command