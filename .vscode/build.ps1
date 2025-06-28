param
(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Command,

    [Parameter(Mandatory = $false)]
    [string]
    $VSConfiguration = "Release"

)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"


# Set folder paths
$script:path_vscode = "$PSScriptRoot"
$script:path_projectRoot = Resolve-Path "$script:path_vscode\.."
$script:path_assemblyOutput = "$script:path_vscode\bin"
$script:path_modOutput = "$script:path_projectroot\output"
$script:path_src = "$script:path_projectroot\src"
$script:path_localDependencies = "$script:path_projectroot\localDependencies"
$script:path_mod_structure = "$script:path_projectroot\mod-structure"

# Set file paths
$script:file_helperscriptPS1 = "$script:path_vscode\build_utility.ps1"
$script:file_thirdPartyDependenciesPS1 = "$script:path_vscode\ThirdPartyDependencies.ps1"
$script:file_modcsproj = "$script:path_vscode\RimWorld_Mod.csproj"

# Predefine Variable
$script:path_RimWorldInstallation = ""
$script:ModName = ""

# import functions from helperscript
. "$script:file_helperscriptPS1"



# FUNCTIONS

# can be called by tasks.json from vscode
function Clean {
    RemoveItem $script:path_assemblyOutput
    RemoveItem $script:path_modOutput
    RemoveItem "$script:path_localDependencies"
    RemoveItem "$script:path_projectroot\.vscode\obj"
    RemoveItem "$script:path_projectroot\.vscode\bin"
    RemoveItem "$script:path_projectroot\.vscode\debugFiles"
}

# can be called by tasks.json from vscode
function Compile {
    Write-Host -ForegroundColor Blue "`r`n#### Compiling ####"
    If (!(Test-Path $script:path_src\*)) {
        Write-Host " -> No files in $script:path_src `r`n -> Skipping compiling"
    }
    if (!(Test-Path $script:path_localDependencies\*)) {
        Write-Host -ForegroundColor Red " -> No local dependencies `r`n -> Run Task 'CopyDependencies'"
    }
    RemoveItem $script:path_assemblyOutput
    dotnet build $script:file_modcsproj --output "$script:path_assemblyOutput" --configuration "$VSConfiguration"

    if ($LASTEXITCODE -ne 0) {
        exit
    }
}

# can be called by tasks.json from vscode
function CopyDependencies {
    Write-Host -ForegroundColor Blue "`r`n#### Checking Dependencies ####"

    if ([string]::IsNullOrEmpty($script:path_RimWorldInstallation)) {
        $script:path_RimWorldInstallation = GetRimWorldInstallationPath 
    }

    # import RimWorld dependencies
    $depsPath = "$script:path_RimWorldInstallation\RimWorldWin64_Data\Managed"
    Write-Host "Copying RimWorld dependencies from installation directory"
    if (!(Test-Path $script:path_localDependencies)) { mkdir $script:path_localDependencies | Out-Null }

    $files = Get-ChildItem -Path "$depsPath\Unity*.dll" -File
    $files += Get-ChildItem -Path "$depsPath\Assembly-CSharp.dll"
    $onlySkip = $true
    $skip = $false

    foreach ($file in $files) {
        $path_fileDest = Join-Path -Path $script:path_localDependencies -ChildPath $file.Name
        if (!(Test-Path -Path $path_fileDest)) {
            # Copy the file if it doesn't exist
            Copy-Item -Path $file.FullName -Destination $path_fileDest
            Write-Host " -> Copied: $($file.Name)"
            $onlySkip = $false
        }
        else {
            # Skip the file if it exists
            Write-Verbose " -> Skipped (already exists): $($file.Name)"
            $skip = $true
        }
    }
    If ($skip -and !($onlySkip)) {
        Write-Host " -> Some files skipped (already exist)"
    }
    elseif ($skip -and $onlySkip) {
        Write-Host " -> All files skipped (already exist)"
    }


    # import third party dependencies
    Write-Host "Import ThirdPartyDependencies"
    
    # import $depsPaths variable from ThirdPartyDependencies.ps1
    .$script:file_thirdPartyDependenciesPS1
    
    if ([string]::IsNullOrEmpty($depsPaths)) {
        Write-Host " -> No ThirdParty Dependencies"
        return
    }

    foreach ($depPath in $depsPaths) {
        # Write-Host " -  Checking $depPath"
        if (Test-Path $depPath) {
            $filename = Split-Path $depPath -Leaf
            if (!(Test-path "$script:path_localDependencies\$filename")) {
                Write-Host " -> Copying $depPath"
                Copy-Item -Force $depPath "$script:path_localDependencies\"
            }
            else {
                Write-Host " -> Skipped (already exists): $filename"
            }

        }
        else {
            Write-Host -ForegroundColor Yellow " -> File does not exist: $depPath"
        }
    }
}

# subcomponents from PostBuild
function CopyAssemblyFile {
    Write-Host "Copy new assembly to mod-structur"

    if (!(Test-Path "$script:path_assemblyOutput\*")) {
        Write-Host " -> No files in $script:path_assemblyOutput `r`n -> Skipping copying files"
        return
    }
    # default assembly - remove old files + copy new ones
    $defaultAssemblyPath = "$script:path_mod_structure\Assemblies"
    Write-Host " -> Copying to $defaultAssemblyPath"
    RemoveItem("$defaultAssemblyPath") -silent
    mkdir $defaultAssemblyPath | Out-Null
    Copy-Item "$script:path_assemblyOutput\*" $defaultAssemblyPath  

    # RimWorld version assembly - remove old files + copy new ones
    if ([string]::IsNullOrEmpty($script:path_RimWorldInstallation)) {
        $script:path_RimWorldInstallation = GetRimWorldInstallationPath 
    }
    $RimWorldVersion = GetRimWorldVersion -RimWorldInstallationPath $script:path_RimWorldInstallation
    if (![string]::IsNullOrEmpty($RimWorldVersion)) {
        $shortVersionAssemblyPath = "$script:path_mod_structure\$RimWorldVersion\Assemblies"
        Write-Host " -> Coping to $shortVersionAssemblyPath"
        RemoveItem("$shortVersionAssemblyPath") -silent
        mkdir $shortVersionAssemblyPath | Out-Null
        Copy-Item "$script:path_assemblyOutput\*" $shortVersionAssemblyPath
    }
}

# subcomponents from PostBuild
function CopyFilesToRimworld {
    Write-Host "`r`nCopy files to RimWorld mod folder"
    if ([string]::IsNullOrEmpty($script:path_RimWorldInstallation)) {
        $script:path_RimWorldInstallation = GetRimWorldInstallationPath 
    }
    if ([string]::IsNullOrEmpty($script:ModName)) {
        $script:ModName = GetModName -file_modcsproj $script:file_modcsproj 
    }

    $modsPath = "$script:path_RimWorldInstallation\Mods"
    $thisModPath = "$modsPath\$script:ModName"
    RemoveItem $thisModPath -silent

    Write-Host " -> $thismodPath"
    Copy-Item -Recurse -Force "$script:path_modOutput\$script:ModName" $modsPath
}

# subcomponents from PostBuild
function CreateModZipFile {
    Write-Host "`r`nCreating distro package" 

    if ([string]::IsNullOrEmpty($script:ModName)) {
        $script:ModName = GetModName -file_modcsproj $script:file_modcsproj 
    }

    # Load the XML content of the .csproj file
    [xml]$csproj = Get-Content $script:file_modcsproj
    $version = $csproj.Project.PropertyGroup[0].VersionPrefix

    if ($VSConfiguration -eq "Debug") {
        $distZip = "$script:path_modOutput\$script:ModName-$version-DEBUG.zip"
    }
    else {
        $distZip = "$script:path_modOutput\$script:ModName-$version.zip"
    }

    Compress-Archive -Path "$script:path_modOutput\$script:ModName" -DestinationPath "$distZip" -Force
    Write-Host " -> $distZip"
}

# subcomponents from PostBuild
function CreateModFolder {

    # Copy mod-structure to path_modOutput
    Write-Host "`r`nCreating mod folder"

    if ([string]::IsNullOrEmpty($script:ModName)) {
        $script:ModName = GetModName -file_modcsproj $script:file_modcsproj 
    }
    Write-Host " -> $script:path_modOutput\$script:ModName"

    RemoveItem("$script:path_modOutput") -silent
    mkdir "$script:path_modOutput\$script:ModName" | Out-Null
    Copy-Item -Recurse -Force "$script:path_mod_structure\*" "$script:path_modOutput\$script:ModName\"

    if ($VSConfiguration -eq "Debug") {
        $AboutFilePath = "$script:path_modOutput\$script:ModName\About\About.xml"

        # Get the current timestamp in the desired format
        $timestamp = Get-Date -Format "HH:mm - dd.MM.yyyy"

        # Read the file content
        [xml]$AboutFile = Get-Content -Path $AboutFilePath

        # Modify the <description> element by adding current timestamp at the beginning
        $AboutFile.ModMetaData.description = "Buildtime: $timestamp `r`n" + $AboutFile.ModMetaData.description

        # Save the modified XML back to the file
        $AboutFile.Save($AboutFilePath)

        Write-Host " -> About file updated with buildtime."
    }
}

# can be called by tasks.json from vscode
function PostBuild {
    Write-Host -ForegroundColor Blue "`r`n#### PostBuild ####"

    CopyAssemblyFile
    CreateModFolder
    CreateModZipFile
    CopyFilesToRimworld
}

# can be called by tasks.json from vscode
function StartRimWorld {
    if ([string]::IsNullOrEmpty($script:path_RimWorldInstallation)) {
        $script:path_RimWorldInstallation = GetRimWorldInstallationPath 
    }
    Write-Host "Start RimWorld"
    Start-Process "$script:path_RimWorldInstallation\RimWorldWin64.exe" 
}

# can be called by tasks.json from vscode
function StartRimWorldQuickTest {
    if ([string]::IsNullOrEmpty($script:path_RimWorldInstallation)) {
        $script:path_RimWorldInstallation = GetRimWorldInstallationPath 
    }
    Write-Host "Start RimWorld"
    Start-Process -FilePath "$script:path_RimWorldInstallation\RimWorldWin64.exe" -ArgumentList "-quicktest" 
}

# can be called by tasks.json from vscode
function StartDNSPY {
    if ([string]::IsNullOrEmpty($script:path_RimWorldInstallation)) {
        $script:path_RimWorldInstallation = GetRimWorldInstallationPath 
    }
    if ([string]::IsNullOrEmpty($script:ModName)) {
        $script:ModName = GetModName -file_modcsproj $script:file_modcsproj 
    }
    $version = GetRimWorldVersion -RimWorldInstallationPath $script:path_RimWorldInstallation
    $path_modDLL = $script:path_RimWorldInstallation + "\Mods\" + $script:ModName + "\" + $version + "\" + "Assemblies\" + $script:ModName + ".dll"
    $DnSpyPath = GetDNSPYPath
    Write-Host "Start dnSpy with mod DLL: $path_modDLL"
    Start-Sleep -Seconds 2
    Start-Process -FilePath $DnSpyPath -ArgumentList "--files `"$path_modDLL`" --no-activate"

}

# can be called by tasks.json from vscode
function Build {
    CopyDependencies
    Compile
    PostBuild
    Write-Host -ForegroundColor Green "`r`nBUILD SUCCESSFUL`r`n"
}

# can be called by tasks.json from vscode
# Build and start RimWorld when the compile was successful
function BuildAndStartRimWorld {
    Build
    StartRimWorld
}


& $Command


