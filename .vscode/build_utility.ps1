$script:NoSection = "_"

Function Get-IniContent {
    <#
    .Synopsis
        Gets the content of an INI file

    .Description
        Gets the content of an INI file and returns it as a hashtable

    .Notes
        Updateded Version to include empty lines
        
        Orginal:
        Author		: Oliver Lipkau <oliver@lipkau.net>
		Source		: https://github.com/lipkau/PsIni
                      http://gallery.technet.microsoft.com/scriptcenter/ea40c1ef-c856-434b-b8fb-ebd7a76e8d91
        Version		: 1.0.0 - 2010/03/12 - OL - Initial release
                      1.0.1 - 2014/12/11 - OL - Typo (Thx SLDR)
                                              Typo (Thx Dave Stiff)
                      1.0.2 - 2015/06/06 - OL - Improvment to switch (Thx Tallandtree)
                      1.0.3 - 2015/06/18 - OL - Migrate to semantic versioning (GitHub issue#4)
                      1.0.4 - 2015/06/18 - OL - Remove check for .ini extension (GitHub Issue#6)
                      1.1.0 - 2015/07/14 - CB - Improve round-tripping and be a bit more liberal (GitHub Pull #7)
                                           OL - Small Improvments and cleanup
                      1.1.1 - 2015/07/14 - CB - changed .outputs section to be OrderedDictionary
                      1.1.2 - 2016/08/18 - SS - Add some more verbose outputs as the ini is parsed,
                      				            allow non-existent paths for new ini handling,
                      				            test for variable existence using local scope,
                      				            added additional debug output.

        #Requires -Version 2.0


    .Inputs
        System.String

    .Outputs
        System.Collections.Specialized.OrderedDictionary

    .Example
        $FileContent = Get-IniContent "C:\myinifile.ini"
        -----------
        Description
        Saves the content of the c:\myinifile.ini in a hashtable called $FileContent

    .Example
        $inifilepath | $FileContent = Get-IniContent
        -----------
        Description
        Gets the content of the ini file passed through the pipe into a hashtable called $FileContent

    .Example
        C:\PS>$FileContent = Get-IniContent "c:\settings.ini"
        C:\PS>$FileContent["Section"]["Key"]
        -----------
        Description
        Returns the key "Key" of the section "Section" from the C:\settings.ini file

    .Link
        Out-IniFile
    #>

    [CmdletBinding()]
    [OutputType(
        [System.Collections.Specialized.OrderedDictionary]
    )]
    Param(
        # Specifies the path to the input file.
        [ValidateNotNullOrEmpty()]
        [Parameter( Mandatory = $true, ValueFromPipeline = $true )]
        [String]
        $FilePath,

        # Specify what characters should be describe a comment.
        # Lines starting with the characters provided will be rendered as comments.
        # Default: ";"
        [Char[]]
        $CommentChar = @(";"),

        # Remove lines determined to be comments from the resulting dictionary.
        [Switch]
        $IgnoreComments,

        # Remove lines determined to be comments from the resulting dictionary.
        [Switch]
        $IgnoreEmptyLines
    )

    Begin {
        Write-Debug "PsBoundParameters:"
        $PSBoundParameters.GetEnumerator() | ForEach-Object { Write-Debug $_ }
        if ($PSBoundParameters['Debug']) {
            $DebugPreference = 'Continue'
        }
        Write-Debug "DebugPreference: $DebugPreference"

        Write-Verbose "$($MyInvocation.MyCommand.Name):: Function started"

        $commentRegex = "^\s*([$($CommentChar -join '')].*)$"
        $emptyLineRegex = "^\s*$"
        $sectionRegex = "^\s*\[(.+)\]\s*$"
        $keyRegex = "^\s*(.+?)\s*=\s*(['`"]?)(.*)\2\s*$"

        Write-Debug ("commentRegex is {0}." -f $commentRegex)
    }

    Process {
        Write-Verbose "$($MyInvocation.MyCommand.Name):: Processing file: $Filepath"

        $ini = New-Object System.Collections.Specialized.OrderedDictionary([System.StringComparer]::OrdinalIgnoreCase)
        #$ini = @{}

        if (!(Test-Path $Filepath)) {
            Write-Verbose ("Warning: `"{0}`" was not found." -f $Filepath)
            Write-Output $ini
        }

        $commentCount = 0
        switch -regex -file $FilePath {
            $sectionRegex {
                # Section
                $section = $matches[1]
                Write-Verbose "$($MyInvocation.MyCommand.Name):: Adding section : $section"
                $ini[$section] = New-Object System.Collections.Specialized.OrderedDictionary([System.StringComparer]::OrdinalIgnoreCase)
                $CommentCount = 0
                $EmptyLineCount = 0
                continue
            }
            $commentRegex {
                # Comment
                if (!$IgnoreComments) {
                    if (!(test-path "variable:local:section")) {
                        $section = $script:NoSection
                        $ini[$section] = New-Object System.Collections.Specialized.OrderedDictionary([System.StringComparer]::OrdinalIgnoreCase)
                    }
                    $value = $matches[1]
                    $CommentCount++
                    Write-Debug ("Incremented CommentCount is now {0}." -f $CommentCount)
                    $name = "Comment" + $CommentCount
                    Write-Verbose "$($MyInvocation.MyCommand.Name):: [$section] Adding $name with value: $value"
                    $ini[$section][$name] = $value
                }
                else {
                    Write-Debug ("Ignoring comment {0}." -f $matches[1])
                }

                continue
            }
            $emptyLineRegex {
                # Empty Line
                if (!$IgnoreEmptyLines) {
                    if (!(test-path "variable:local:section")) {
                        $section = $script:NoSection
                        $ini[$section] = New-Object System.Collections.Specialized.OrderedDictionary([System.StringComparer]::OrdinalIgnoreCase)
                    }
                    $EmptyLineCount++
                    Write-Debug ("Incremented EmptyLineCount is now {0}." -f $EmptyLineCount)
                    $name = "EmptyLine" + $EmptyLineCount
                    Write-Verbose "$($MyInvocation.MyCommand.Name):: [$section] Adding $name"
                    $ini[$section][$name] = ""
                }
                else {
                    Write-Debug ("Ignoring EmptyLine")
                }

                continue
            }
            $keyRegex {
                # Key
                if (!(test-path "variable:local:section")) {
                    $section = $script:NoSection
                    $ini[$section] = New-Object System.Collections.Specialized.OrderedDictionary([System.StringComparer]::OrdinalIgnoreCase)
                }
                $name, $value = $matches[1, 3]
                Write-Verbose "$($MyInvocation.MyCommand.Name):: [$section] Adding key $name with value: $value"
                if (-not $ini[$section][$name]) {
                    $ini[$section][$name] = $value
                }
                else {
                    if ($ini[$section][$name] -is [string]) {
                        $firstValue = $ini[$section][$name]
                        $ini[$section][$name] = [System.Collections.ArrayList]::new()
                        $ini[$section][$name].Add($firstValue) | Out-Null
                        $ini[$section][$name].Add($value) | Out-Null
                    }
                    else {
                        $ini[$section][$name].Add($value) | Out-Null
                    }
                }
                continue
            }
            default {
                Write-Verbose "Unmatched line: $_ "
            }
        }
        Write-Verbose "$($MyInvocation.MyCommand.Name):: Finished Processing file: $FilePath"
        Write-Output $ini
    }

    End {
        Write-Verbose "$($MyInvocation.MyCommand.Name):: Function ended"
    }
}

Function Out-IniFile {
    <#
    .Synopsis
        Write hash content to INI file

    .Description
        Write hash content to INI file

    .Notes
        Updateded Version to include empty lines

        Orginal:
        Author      : Oliver Lipkau <oliver@lipkau.net>
        Blog        : http://oliver.lipkau.net/blog/
        Source      : https://github.com/lipkau/PsIni
                      http://gallery.technet.microsoft.com/scriptcenter/ea40c1ef-c856-434b-b8fb-ebd7a76e8d91

        #Requires -Version 2.0

    .Inputs
        System.String
        System.Collections.IDictionary

    .Outputs
        System.IO.FileSystemInfo

    .Example
        Out-IniFile $IniVar "C:\myinifile.ini"
        -----------
        Description
        Saves the content of the $IniVar Hashtable to the INI File c:\myinifile.ini

    .Example
        $IniVar | Out-IniFile "C:\myinifile.ini" -Force
        -----------
        Description
        Saves the content of the $IniVar Hashtable to the INI File c:\myinifile.ini and overwrites the file if it is already present

    .Example
        $file = Out-IniFile $IniVar "C:\myinifile.ini" -PassThru
        -----------
        Description
        Saves the content of the $IniVar Hashtable to the INI File c:\myinifile.ini and saves the file into $file

    .Example
        $Category1 = @{“Key1”=”Value1”;”Key2”=”Value2”}
        $Category2 = @{“Key1”=”Value1”;”Key2”=”Value2”}
        $NewINIContent = @{“Category1”=$Category1;”Category2”=$Category2}
        Out-IniFile -InputObject $NewINIContent -FilePath "C:\MyNewFile.ini"
        -----------
        Description
        Creating a custom Hashtable and saving it to C:\MyNewFile.ini
    .Link
        Get-IniContent
    #>

    [CmdletBinding()]
    [OutputType(
        [System.IO.FileSystemInfo]
    )]
    Param(
        # Adds the output to the end of an existing file, instead of replacing the file contents.
        [switch]
        $Append,

        # Specifies the file encoding. The default is UTF8.
        #
        # Valid values are:
        # -- ASCII:  Uses the encoding for the ASCII (7-bit) character set.
        # -- BigEndianUnicode:  Encodes in UTF-16 format using the big-endian byte order.
        # -- Byte:   Encodes a set of characters into a sequence of bytes.
        # -- String:  Uses the encoding type for a string.
        # -- Unicode:  Encodes in UTF-16 format using the little-endian byte order.
        # -- UTF7:   Encodes in UTF-7 format.
        # -- UTF8:  Encodes in UTF-8 format.
        [ValidateSet("Unicode", "UTF7", "UTF8", "ASCII", "BigEndianUnicode", "Byte", "String")]
        [Parameter()]
        [String]
        $Encoding = "UTF8",

        # Specifies the path to the output file.
        [ValidateNotNullOrEmpty()]
        [ValidateScript( { Test-Path $_ -IsValid } )]
        [Parameter( Position = 0, Mandatory = $true )]
        [String]
        $FilePath,

        # Allows the cmdlet to overwrite an existing read-only file. Even using the Force parameter, the cmdlet cannot override security restrictions.
        [Switch]
        $Force,

        # Specifies the Hashtable to be written to the file. Enter a variable that contains the objects or type a command or expression that gets the objects.
        [Parameter( Mandatory = $true, ValueFromPipeline = $true )]
        [System.Collections.IDictionary]
        $InputObject,

        # Passes an object representing the location to the pipeline. By default, this cmdlet does not generate any output.
        [Switch]
        $Passthru,

        # Adds spaces around the equal sign when writing the key = value
        [Switch]
        $Loose,

        # Writes the file as "pretty" as possible
        #
        # Adds an extra linebreak between Sections
        [Switch]
        $Pretty
    )

    Begin {
        Write-Debug "PsBoundParameters:"
        $PSBoundParameters.GetEnumerator() | ForEach-Object { Write-Debug $_ }
        if ($PSBoundParameters['Debug']) {
            $DebugPreference = 'Continue'
        }
        Write-Debug "DebugPreference: $DebugPreference"

        Write-Verbose "$($MyInvocation.MyCommand.Name):: Function started"

        function Out-Keys {
            param(
                [ValidateNotNullOrEmpty()]
                [Parameter( Mandatory, ValueFromPipeline )]
                [System.Collections.IDictionary]
                $InputObject,

                [ValidateSet("Unicode", "UTF7", "UTF8", "ASCII", "BigEndianUnicode", "Byte", "String")]
                [string]
                $Encoding = "UTF8",

                [ValidateNotNullOrEmpty()]
                [ValidateScript( { Test-Path $_ -IsValid })]
                [Parameter( Mandatory, ValueFromPipelineByPropertyName )]
                [Alias("Path")]
                [string]
                $FilePath,

                [Parameter( Mandatory )]
                $Delimiter,

                [Parameter( Mandatory )]
                $Local_MyInvocation = $MyInvocation
            )

            Process {
                if (!($InputObject.get_keys())) {
                    Write-Warning ("No data found in '{0}'." -f $FilePath)
                }
                Foreach ($key in $InputObject.get_keys()) {
                    if ($key -match "^Comment\d+") {
                        Write-Verbose "$($Local_MyInvocation.MyCommand.Name):: Writing comment: $key"
                        "$($InputObject[$key])" | Out-File -Encoding $Encoding -FilePath $FilePath -Append
                    }
                    elseif ($key -match "^EmptyLine\d+") {
                        Write-Verbose "$($Local_MyInvocation.MyCommand.Name):: Writing EmptyLine: $key"
                        "" | Out-File -Encoding $Encoding -FilePath $FilePath -Append
                    }
                    else {
                        Write-Verbose "$($Local_MyInvocation.MyCommand.Name):: Writing key: $key"
                        $InputObject[$key] |
                        ForEach-Object { "$key$delimiter$_" } |
                        Out-File -Encoding $Encoding -FilePath $FilePath -Append
                    }
                }
            }
        }

        $delimiter = '='
        if ($Loose) {
            $delimiter = ' = '
        }
        # Splatting Parameters
        $parameters = @{
            Encoding = $Encoding;
            FilePath = $FilePath
        }

    }

    Process {
        $extraLF = ""

        if ($Append) {
            Write-Debug ("Appending to '{0}'." -f $FilePath)
            $outfile = Get-Item $FilePath
        }
        else {
            Write-Debug ("Creating new file '{0}'." -f $FilePath)
            $outFile = New-Item -ItemType file -Path $Filepath -Force:$Force
        }

        if (!(Test-Path $outFile.FullName)) { Throw "Could not create File" }

        Write-Verbose "$($MyInvocation.MyCommand.Name):: Writing to file: $Filepath"
        foreach ($i in $InputObject.get_keys()) {
            Write-Verbose "KEY: $i"
            if (!($InputObject[$i].GetType().GetInterface('IDictionary'))) {
                #Key value pair
                Write-Verbose "$($MyInvocation.MyCommand.Name):: Writing key: $i"
                "$i$delimiter$($InputObject[$i])" | Out-File -Append @parameters

            }
            elseif ($i -eq $script:NoSection) {
                #Key value pair of NoSection
                Write-Verbose "$($MyInvocation.MyCommand.Name):: Writing NoSection"
                Out-Keys $InputObject[$i] `
                    @parameters `
                    -Delimiter $delimiter `
                    -Local_MyInvocation $MyInvocation
            }
            else {
                #Sections
                Write-Verbose "$($MyInvocation.MyCommand.Name):: Writing Section: [$i]"

                # Only write section, if it is not a dummy ($script:NoSection)
                if ($i -ne $script:NoSection) { "$extraLF[$i]"  | Out-File -Append @parameters }
                if ($Pretty) {
                    $extraLF = "`r`n"
                }

                if ( $InputObject[$i].Count) {
                    Out-Keys $InputObject[$i] `
                        @parameters `
                        -Delimiter $delimiter `
                        -Local_MyInvocation $MyInvocation
                }

            }
        }
        Write-Verbose "$($MyInvocation.MyCommand.Name):: Finished Writing to file: $FilePath"
    }

    End {
        if ($PassThru) {
            Write-Debug ("Returning file due to PassThru argument.")
            Write-Output (Get-Item $outFile)
        }
        Write-Verbose "$($MyInvocation.MyCommand.Name):: Function ended"
    }
}


function RemoveItem() {
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $path,

        [switch]
        $silent = $false
    )
    
    $i = 0
    while ($true) {
        if (!(Test-Path $path)) {
            return
        }
        if (!($silent)) {
            Write-Host " Deleting $path"
        }

        try {

            Remove-Item -Recurse $path -Force
            break
        }
        catch {
            $i++
            if ($i -gt 10) { 
                Write-Host "Could not remove $path, skip file"
                break 
            }
            Write-Host "Could not remove $path, will retry"
            Start-Sleep 3
        }
    }
}

function GetModName {
    param (
        [Parameter(Mandatory = $true)]
        [string]
        $file_modcsproj
    )
    [xml]$csproj = Get-Content $file_modcsproj

    # Extract AssemblyName from the PropertyGroup
    return $csproj.Project.PropertyGroup[0].AssemblyName
}

function GetRimWorldVersion {
    [CmdletBinding()]
    param (
        [Parameter()]
        [ValidateScript( { Test-Path $_ -IsValid } )]
        [string]
        $RimWorldInstallationPath
    )

    $versionFile = Get-Content -Path "$RimWorldInstallationPath\version.txt"
    $versionParts = $versionFile -split '\.'
    $shortVersion = "$($versionParts[0]).$($versionParts[1])"

    Write-Host " -> Dedected RimWorld version: $shortVersion"
    return $shortVersion

}

function SetEnviromentVariableInteractive {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]
        $Question,

        [Parameter(Mandatory = $true)]
        [string]
        $EnvName
    )
    Write-Host "`r`nCtrl+Shift+V to copy the path from the clipboard"
    $value = Read-Host $Question
    [System.Environment]::SetEnvironmentVariable($EnvName, $value, [System.EnvironmentVariableTarget]::User)
    Write-Host " -> Set $EnvName to $value`r`n"
}

function GetRimWorldInstallationPath {
    # Check for RimWorld installations

    Write-Host "Looking for RimWorld installation..."

    $path_RimWorld = [System.Environment]::GetEnvironmentVariable("RimWorldInstallationPath", "User")
    if (![string]::IsNullOrEmpty($path_RimWorld)) {
        if (Test-Path "$path_RimWorld\RimWorldWin64.exe") {
            Write-Host " -> Found RimWorld Path (env): $path_RimWorld`r`n"
            return $path_RimWorld
        }
    }

    $StandardRimWorldInstallationPath = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld"
    if (Test-Path "$StandardRimWorldInstallationPath\RimWorldWin64.exe") {
        Write-Host " -> Found RimWorld Path (standard): $StandardRimWorldInstallationPath `r`n"
        [System.Environment]::SetEnvironmentVariable("RimWorldInstallationPath", $StandardRimWorldInstallationPath, [System.EnvironmentVariableTarget]::User)
        return $StandardRimWorldInstallationPath
    }

    Write-Host " -> RimWorld installation not found; Need Enviroment variable 'RimWorldInstallationPath'"
    if ((Read-Host "Do you want to create a new environment variable for the RimWorld installation path? (y/n)") -eq "y") {
         
        for ($i = 0; $i -lt 3; $i++) {
            SetEnviromentVariableInteractive -Question "Please enter the path to the RimWorld installation" -EnvName "RimWorldInstallationPath"
            $path_RimWorld = [System.Environment]::GetEnvironmentVariable("RimWorldInstallationPath", "User")
            if (Test-Path "$path_RimWorld\RimWorldWin64.exe") {
                return $path_RimWorld
            }
            Write-Host " -> RimWorldWin64.exe not found, please try again: $path_RimWorld\RimWorldWin64.exe`r`n"
        }
    }

    Write-Host " -> Rimworld installation not found; Need Enviroment variable 'RimWorldInstallationPath' "
    exit "RimWorld installation not found"

}

function GetDNSPYPath {
    # Check for DNSPY installations

    Write-Host "Looking for dnSPY installation..."
    $path_dnSpy = [System.Environment]::GetEnvironmentVariable("path_dnSpy", "User")
    if (![string]::IsNullOrEmpty($path_dnSpy)) {
        if ((Test-Path $path_dnSpy) -and ($path_dnSpy.EndsWith(".exe"))) {
            Write-Host " -> Found dnSPY Path: $path_dnSpy `r`n"
            return $path_dnSpy
        }
        else {
            Write-Host " -> dnSpy.exe not found, environment variable is wrong: $path_dnSpy`r`n"
        }
    }
    else {
        Write-Host " -> 'path_dnSpy' environment variable not found"
    }

    if ((Read-Host "Do you want to create a new environment variable 'path_dnSpy' for the path to dnSPY.exe? (y/n)") -eq "y") {
        
        for ($i = 0; $i -lt 3; $i++) {
            SetEnviromentVariableInteractive -Question "Please enter the path to the dnSPY.exe" -EnvName "path_dnSpy"
            $path_dnSpy = [System.Environment]::GetEnvironmentVariable("path_dnSpy", "User")
            if ((Test-Path $path_dnSpy) -and ($path_dnSpy.EndsWith(".exe"))) {
                return $path_dnSpy
            }
            Write-Host " -> dnSpy.exe not found, please try again: $path_dnSpy`r`n"
        }
    }

    Write-Host " -> dnSPY installation not found; Need Enviroment variable 'path_dnSpy' "
    exit "dnSPY installation not found"

}

