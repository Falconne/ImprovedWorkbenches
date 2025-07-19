param
(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Command
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"


#Define Folders and Files
$script:dir_debugFiles = "$PSScriptRoot\debugFiles"
$script:file_helperscriptPS1 = "$PSScriptRoot\build_utility.ps1"


$script:file_doorstopDLL = "$script:dir_debugFiles\Doorstop.dll"
$script:file_winhttpDLL = "$script:dir_debugFiles\winhttp.dll"
$script:file_doorstopConfigINI = "$script:dir_debugFiles\doorstop_config.ini"

# import functions from helperscript
. "$script:file_helperscriptPS1"


function GetGitHubLatestReleaseTag {
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Repo
    )

    Write-Host "Determining latest release from $Repo" 
    $releases = "https://api.github.com/repos/$Repo/releases"
    $releases_jsons = (Invoke-RestMethod -Uri $releases -UseBasicParsing)

    foreach ($json in $releases_jsons) {    
        if (-not $json.prerelease -and -not $json.draft) {
            $tag_name = $json.tag_name
            Write-Host "Latest Tag: $tag_name"
            return $tag_name
        }
    }
    Write-Host -ForegroundColor Red "No Release found"
    exit "No Release found"
}


function GitHubDownload {
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Repo,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Name_file,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Destination
    )
    Write-Host "`r`nDowloading latest release: $Name_file"
    Write-Host "https://github.com/$Repo/releases/latest/download/$Name_file"
    curl.exe -L "https://github.com/$Repo/releases/latest/download/$Name_file" --output $Destination

}


function ExtractZip {
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $zipFile,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Destination
    )

    Write-Host "`r`nExtracting $zipFile" 
    Expand-Archive $zipFile -Force -DestinationPath $Destination

    # Removing temp files
    RemoveItem $zipFile 
}


function DownloadFiles {

    # Download Unity Doorstop
    $Repo = "NeighTools/UnityDoorstop"
    Write-Host -ForegroundColor Blue "Download: $Repo"
    if (!(Test-Path $script:file_winhttpDLL) -or !(Test-Path $script:file_doorstopConfigINI)) {

        $tag = GetGitHubLatestReleaseTag -repo $Repo
        $version = $tag.Substring(1)

        $name_zipfile = "doorstop_win_release_$version.zip"
        $path_zipfile = "$script:dir_debugFiles\$name_zipfile"
        GitHubDownload -Repo $Repo -Name_file $name_zipfile -Destination $path_zipfile

        $name_zipDir = Split-Path $name_zipfile -LeafBase
        $path_zipDir = "$script:dir_debugFiles\$name_zipDir"
        ExtractZip -zipFile "$path_zipfile" -Destination "$path_zipDir"

        # move winhttp.dll vom extracted folder to debugTemp folder 
        if (!(Test-Path $script:file_winhttpDLL)) { 
            Move-Item -Path "$path_zipDir\x64\winhttp.dll" -Destination $script:file_winhttpDLL 
            Write-Host " -> File downloaded: $script:file_winhttpDLL"
        }
        else {
            Write-Host " -> File already exist: $script:file_winhttpDLL"
        }

        # move file_doorstopConfigINI vom extracted folder to debugTemp folder 
        if (!(Test-Path $script:file_doorstopConfigINI)) { 
            Move-Item -Path "$path_zipDir\x64\doorstop_config.ini" -Destination $script:file_doorstopConfigINI 
            Write-Host " -> File downloaded: $script:file_doorstopConfigINI "
        }
        else {
            Write-Host " -> File already exist: $script:file_doorstopConfigINI"
        }

        # remove not needed files
        RemoveItem $path_zipDir
    }
    else {
        Write-Host " -> File already exist - skipping download"
    }


    # Download Doorstop.dll for RimWorld
    $Repo = "pardeike/Rimworld-Doorstop"
    $file = "doorstop.dll"
    Write-Host -ForegroundColor Blue "`r`nDownload $Repo"
    if (!(Test-Path $script:file_doorstopDLL)) {
        GitHubDownload -Repo $Repo -Name_file $file -Destination "$script:file_doorstopDLL"
    }
    else {
        Write-Host " -> File already exist - skipping download"
    }
}


function ModifyDoorstopConfig {
    Write-Host -ForegroundColor Blue "`r`nModify Doorstop-config.ini"

    $ini = Get-IniContent -FilePath $script:file_doorstopConfigINI -CommentChar "#"

    # Check if Section exists; if not, create it
    if (-not $ini.Contains("General")) { $ini["General"] = @{} }
    $ini["General"]["enabled"] = "true"
    $ini["General"]["target_assembly"] = "Doorstop.dll"
    $ini["General"]["redirect_output_log"] = "false"
    $ini["General"]["ignore_disable_switch"] = "true"

    # Check if Section exists; if not, create it
    if (-not $ini.Contains("UnityMono")) { $ini["UnityMono"] = @{} }
    $ini["UnityMono"]["dll_search_patch_override"] = ""
    $ini["UnityMono"]["debug_enabled"] = "true"         
    $ini["UnityMono"]["debug_address"] = "127.0.0.1:55555"
    $ini["UnityMono"]["debug_suspend"] = "false"

    $ini | Out-IniFile $script:file_doorstopConfigINI -Force 
    
    Write-Host " -> Ini File overwritten"
    Write-Host " -> Debugger is reachable on ip: $($ini.UnityMono.debug_address)"
}

# Move Files to RimWorld installation
function CopyDebugFilesToRimWorldFolder {
    Write-Host -ForegroundColor Blue "`r`nMove Files to RimWorld"
    $dir_RimWorldInstallation = GetRimWorldInstallationPath 
    
    Copy-Item -Force -Path $script:file_doorstopDLL -Destination $dir_RimWorldInstallation 
    Copy-Item -Force -Path $script:file_winhttpDLL -Destination $dir_RimWorldInstallation 
    Copy-Item -Force -Path $script:file_doorstopConfigINI -Destination $dir_RimWorldInstallation

    Write-Host -ForegroundColor Green " -> All files copied"
    Write-Host " -> Restart RimWord and attach dnSpy.exe`r`n"
}


function InstallRimWorldDebug {
    if (!(Test-Path $script:dir_debugFiles)) { mkdir $script:dir_debugFiles | Out-Null }
    DownloadFiles
    ModifyDoorstopConfig
    CopyDebugFilesToRimWorldFolder
}


function RemoveRimWorldDebug {
    Write-Host -ForegroundColor Blue "Remove debug files"
    $RimWorldPath = GetRimWorldInstallationPath 

    $fileName = Split-Path $script:file_doorstopDLL -Leaf
    RemoveItem -Path "$RimWorldPath\$fileName"
    $fileName = Split-Path $script:file_winhttpDLL -Leaf
    RemoveItem -Path "$RimWorldPath\$fileName"
    $fileName = Split-Path $script:file_doorstopConfigINI -Leaf
    RemoveItem -Path "$RimWorldPath\$fileName"

    Write-Host -ForegroundColor Green " -> All files removed"
}

& $Command