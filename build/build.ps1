param
(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Command,

    [Parameter(Mandatory = $false)]
    [string]
    $VSConfiguration = "Release",

    [Parameter(Mandatory = $false)]
    [string]
    $NewModName = "",

    [Parameter(Mandatory = $false)]
    [string]
    $EnvName = "",

    [Parameter(Mandatory = $false)]
    [string]
    $EnvValue = ""

)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"


# Set folder paths
$script:path_build = "$PSScriptRoot"
$script:path_solutionRoot = Resolve-Path "$script:path_build\.."
$script:path_modOutput = "$script:path_solutionRoot\dist"
$script:path_src = "$script:path_solutionRoot\src"
$script:path_localDependencies = "$script:path_solutionRoot\lib"
$script:path_mod_structure = "$script:path_solutionRoot\mod-structure"

# Set file paths
$script:file_helperscriptPS1 = "$script:path_build\build_utility.ps1"
$script:file_thirdPartyDependenciesPS1 = "$script:path_build\ThirdPartyDependencies.ps1"
$script:file_modcsproj = Get-ChildItem -Path "$script:path_src" -Filter "*.csproj" -Recurse -ErrorAction Stop

# More folder paths
$script:path_project = $script:file_modcsproj.DirectoryName
$script:path_assemblyOutput = "$script:path_project\bin"


# Predefine Variable
$script:path_RimWorldInstallation = ""
$script:ModName = ""

# import functions from helperscript
. "$script:file_helperscriptPS1"



# FUNCTIONS

# can be called by tasks.json from vscode
function Clean {
    Write-Host -ForegroundColor Blue "`r`n#### Clean ####"
    
    RemoveItem "$script:path_assemblyOutput"
    RemoveItem "$script:path_modOutput"
    RemoveItem "$script:path_localDependencies"
    RemoveItem "$script:path_build\debugFiles"
    RemoveItem "$script:path_project\bin"
    RemoveItem "$script:path_project\obj"
    
    Write-Host " -> Clean completed.`r`n"
}

# can be called by tasks.json from vscode
# automatically called when building by .csproj [InitialTargets="CopyDependencies"]
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

# automatically called when building by .csproj [InitialTargets="CopyDependencies"]
function PreBuild {
    Write-Host -ForegroundColor Blue "`r`n#### PreBuild ####"
    RemoveItem $script:path_assemblyOutput
}

# can be called by tasks.json from vscode
function Build {

    CopyDependencies

    Write-Host -ForegroundColor Blue "`r`n#### Build - $VSConfiguration ####"
    dotnet build $script:file_modcsproj --verbosity detailed --configuration $VSConfiguration

    Write-Host "`r`n"

    if ($LASTEXITCODE -ne 0) {
        exit
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
function CreateModFolder {

    # Copy mod-structure to path_modOutput
    Write-Host "`r`nCreating mod folder"

    if ([string]::IsNullOrEmpty($script:ModName)) {
        $script:ModName = GetModName -file_modcsproj $script:file_modcsproj 
    }
    Write-Host " -> $script:path_modOutput\$script:ModName"

    RemoveItem("$script:path_modOutput") -silent
    mkdir "$script:path_modOutput\$script:ModName" | Out-Null
    

    if ($VSConfiguration -eq "Release") {
        # Copy all files except .pdb files when building in Release mode
        Copy-Item -Recurse -Force "$script:path_mod_structure\*" "$script:path_modOutput\$script:ModName\" -Exclude "*.pdb"
    }
    else {
        Copy-Item -Recurse -Force "$script:path_mod_structure\*" "$script:path_modOutput\$script:ModName\" 
    }

    # Update About.xml file with modVersion
    $AboutFilePath = "$script:path_modOutput\$script:ModName\About\About.xml"
    [xml]$AboutFile = Get-Content -Path $AboutFilePath

    [xml]$csproj = Get-Content $script:file_modcsproj
    $version = $csproj.Project.PropertyGroup[0].VersionPrefix

    $modVersionNode = $AboutFile.ModMetaData.SelectSingleNode("modVersion")
    if ($modVersionNode) {
        if ($VSConfiguration -ne "Release") {
            $timestamp = Get-Date -Format "dd.MM-HH:mm"
            $modVersionNode.InnerText = "$version - $timestamp"
        }
        else {
            $modVersionNode.InnerText = $version
        }
    }
    else {
        Write-Host " -> Warning: modVersion node not found in About.xml"
    }

    $AboutFile.Save($AboutFilePath)
    Write-Host " -> About file updated"
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

# can be called by tasks.json from vscode
# automatically called when building by .csproj [AfterTargets="PostBuildEvent"]
function PostBuild {
    Write-Host -ForegroundColor Blue "`r`n#### PostBuild ####"
    Write-Host " -> VSConfiguration: $VSConfiguration"

    CopyAssemblyFile
    CreateModFolder
    CreateModZipFile
    CopyFilesToRimworld

    Write-Host "`r`n"
}



# ADDITIONAL FUNCTIONS

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
# StartRimWorld only called when Build is successful (exit call in Build function)
function BuildAndStartRimWorld {
    Build
    StartRimWorld
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
function ChangeModName {

    Write-Host -ForegroundColor Blue "`r`n#### ChangeModName ####"
    if ([string]::IsNullOrEmpty($newModName)) {
        Write-Host -ForegroundColor Red " -> new ModName is empty"
        exit;
    }
    elseif ($newModName -match '[^a-zA-Z0-9]' -or $newModName -match '^[0-9]') {
        Write-Host -ForegroundColor Red " -> new ModName contains invalid characters"
        Write-Host " -> alphanumeric only, must not start with a number, no spaces or special characters"
        exit;
    }

    $script:ModName = $newModName

    # Change name in about.xml file in mod-structure (read as XML)
    $aboutFile = Get-ChildItem -Path "$script:path_mod_structure\About\About.xml"
    if ($aboutFile) {
        [xml]$aboutFileContent = Get-Content -Path $aboutFile
        $aboutFileContent.ModMetaData.name = $newModName
        $aboutFileContent.Save($aboutFile)
    }
    Write-Host " -> About.xml file updated"

    # Rename project folder in src
    $projectFolder = Get-ChildItem -Path "$script:path_src" -Directory
    if (-not $projectFolder) {
        Write-Host -ForegroundColor Red " -> project folder in $script:path_src not found"
    }
    else {
        $currentFolderName = Split-Path $projectFolder.FullName -Leaf
        if ($currentFolderName -ne $newModName) {
            Rename-Item -Path $projectFolder.FullName -NewName "$newModName"
            Write-Host " -> project folder renamed to $newModName"
        }
        else {
            Write-Host " -> project folder already has the correct name: $newModName"
        }
    }

    # Rename .csproj file in src
    $script:file_modcsproj = Get-ChildItem -Path "$script:path_src" -Filter "*.csproj" -Recurse -ErrorAction Stop
    if (-not $script:file_modcsproj) {
        Write-Host -ForegroundColor Red " -> .csproj file in $script:path_src not found (Recursively searched)"
        exit;
    }
    $currentCsprojName = Split-Path $script:file_modcsproj.FullName -Leaf
    if ($currentCsprojName -ne "$newModName.csproj") {
        Rename-Item -Path $script:file_modcsproj.FullName -NewName "$newModName.csproj"
        Write-Host " -> .csproj file renamed to $newModName.csproj"
    }
    else {
        Write-Host " -> .csproj file already has the correct name: $newModName.csproj"
    }
    $script:file_modcsproj = Get-ChildItem -Path "$script:path_src" -Filter "*.csproj" -Recurse -ErrorAction Stop

    # Change content in .csproj file to new ModName (load as XML)
    [xml]$csproj = Get-Content $script:file_modcsproj.FullName
    $csproj.Project.PropertyGroup[0].AssemblyName = $newModName
    $csproj.Project.PropertyGroup[0].RootNamespace = $newModName
    $csproj.Save($script:file_modcsproj)
    Write-Host " -> .csproj file updated"
    
    # Rename .slnx file
    $slnxFile = Get-ChildItem -Path "$script:path_solutionRoot" -Filter "*.slnx"
    $currentSlnxName = Split-Path $slnxFile.FullName -Leaf
    if ($currentSlnxName -ne "$newModName.slnx") {
        Rename-Item -Path $slnxFile.FullName -NewName "$newModName.slnx"
        Write-Host " -> .slnx file renamed to $newModName.slnx"
    }
    else {
        Write-Host " -> .slnx file already has the correct name: $newModName.slnx"
    }
    $slnxPath = "$script:path_solutionRoot\$newModName.slnx"
    
    # Update the .slnx file content to reflect the new .csproj path
    [xml]$slnxFileContent = Get-Content -Path $SlnxPath
    $csprojRelativePath = [IO.Path]::GetRelativePath($script:path_solutionRoot, $script:file_modcsproj.FullName)
    $slnxFileContent.Solution.Project.Path = $csprojRelativePath
    $slnxFileContent.Save($slnxPath)
    Write-Host " -> .slnx file updated with new .csproj path"
    

    # Change content in Main.cs to new ModName
    $mainFile = Get-ChildItem -Path "$script:path_src\$newModName" -Filter "Main.cs"
    if ($mainFile) {
        $mainFileContent = Get-Content -Path $mainFile -Raw
        # Replace the string inside Log.Message("...") for the "Mod Template Loaded!" message
        $newMessage = 'Log.Message("Mod ' + $newModName + ' Loaded!");'
        $mainFileContent = $mainFileContent -replace 'Log\.Message\("Mod[^"]*Loaded!"\);', $newMessage
        # Replace the namespace name with the new ModName
        $mainFileContent = $mainFileContent -replace '(?<=namespace\s+)[^\s{]+', $newModName
        $mainFileContent | Out-File -FilePath $mainFile.FullName -Encoding UTF8
    }
    Write-Host " -> Main.cs file updated"


    Write-Host -ForegroundColor Green "All files updated to new ModName: $newModName"    
    Write-Host "`r`n"

    Clean
    Write-Host -ForegroundColor Green "Project cleaned"
}

# can be called by tasks.json from vscode
function SetEnviromentVariable {

    Write-Host -ForegroundColor Blue "`r`n#### SetEnviromentVariable ####"
    [System.Environment]::SetEnvironmentVariable($script:EnvName, $script:EnvValue, [System.EnvironmentVariableTarget]::User)
    Write-Host " -> Set Environment Variable '$script:EnvName' to '$script:EnvValue'`r`n"
}

& $Command


