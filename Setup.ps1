<#
.Synopsis
   Installs the InstaTech Server and all its dependencies.
.DESCRIPTION
   Installs the InstaTech Server and all its dependencies.
.AUTHOR
   Jared Goodwin (http://invis.me)
.UPDATED
   December 23, 2017
#>
$ErrorActionPreference = "Suspend"
$Host.UI.RawUI.WindowTitle = "InstaTech Setup"
Clear-Host

#region Variables
$HostName = ""
$CompanyName = ""
$InstallPath = ""
$Website = $null
$ServerCmdlets = $false
$FirewallSet = $false
$CopyErrors = $false
if ($PSScriptRoot -eq ""){
    $PSScriptRoot = (Get-Location)
}
$Bin = "$PSScriptRoot\Setup"
$ParentFolder = (New-Object System.IO.DirectoryInfo $PSScriptRoot).Parent.FullName
#endregion

#region Functions
function Wrap-Host
{
    [CmdletBinding()]
    [Alias()]
    [OutputType([int])]
    Param
    (
        # The text to write.
        [Parameter(Mandatory=$false,
                   ValueFromPipelineByPropertyName=$true,
                   Position=0)]
        [String]
        $Text,

        # Param2 help description
        [Parameter(Mandatory=$false,
                   ValueFromPipelineByPropertyName=$true,
                   Position=1)]
        [ConsoleColor]
        $ForegroundColor
    )

    Begin
    {
    }
    Process
    {
        if (!$Text){
            Write-Host
            return
        }
        $Width = $Host.UI.RawUI.BufferSize.Width
        $SB = New-Object System.Text.StringBuilder
        while ($Text.Length -gt $Width) {
            [int]$LastSpace = $Text.Substring(0, $Width).LastIndexOf(" ")
            $SB.AppendLine($Text.Substring(0, $LastSpace).Trim()) | Out-Null
            $Text = $Text.Substring(($LastSpace), $Text.Length - $LastSpace).Trim()
        }
        $SB.Append($Text) | Out-Null
        if ($ForegroundColor)
        {
            Write-Host $SB.ToString() -ForegroundColor $ForegroundColor
        }
        else
        {
            Write-Host $SB.ToString()
        }
        
    }
    End
    {
    }
}
function Retry-Block ($ScriptBlock, $TryCount, $ErrorVar) {
    Start-Sleep -Seconds 2
    Write-Host
    Write-Host "Operation failed.  Retrying (Count: $TryCount)..." -ForegroundColor Red
    try
    {
        Invoke-Command -ScriptBlock $ScriptBlock
        Write-Host
        Write-Host "Retry successful." -ForegroundColor Green
    }
    catch
    {
        if ($TryCount -lt 3)
        {
            $TryCount++
            Retry-Block -ScriptBlock $ScriptBlock -TryCount $TryCount
        }
        else
        {
            Write-Host
            $ErrorVar = $true;
            Write-Host "Operation aborted." -ForegroundColor Red
        }
    }
}
#endregion


#region Prerequisite Tests
### Test if process is elevated. ###
$User = [Security.Principal.WindowsIdentity]::GetCurrent()
if ((New-Object Security.Principal.WindowsPrincipal $User).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator) -eq $false) {
    Wrap-Host
    Wrap-Host "Error: This installation needs to be run from an elevated process (Run as Administrator)." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    return
}
### Check Script Root ###
if (!$PSScriptRoot) {
    Wrap-Host
    Wrap-Host "Error: Unable to determine working directory.  Please make sure you're running the full script and not just a section." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    return
}
### Check for project files. ###
if ((Test-Path -Path "$PSScriptRoot\InstaTech_Server.sln") -eq $false) {
    Wrap-Host
    Wrap-Host "Error: Could not find the InstaTech_Server solution file.  Please make sure you run this script from the same directory as the InstaTech_Server.sln file." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    return
}
### Check OS version. ###
$OS = Get-WmiObject -Class Win32_OperatingSystem
if ($OS.Name.ToLower().Contains("home") -or $OS.Caption.ToLower().Contains("home")) {
    Wrap-Host
    Wrap-Host "Error: Windows Home version does not have the necessary features to run InstaTech." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    return
}
### Test if Windows Feature cmdlets are available. ###
if ((Get-Command -Name "Add-WindowsFeature" -ErrorAction Ignore) -eq $null) {
    $ServerCmdlets = $false
}
else {
    $ServerCmdlets = $true
}
#endregion

### Building Requirements ###
Wrap-Host
Wrap-Host "**********************************"
Wrap-Host "           IMPORTANT" -ForegroundColor Green
Wrap-Host "**********************************"
Wrap-Host
Wrap-Host "In order to build the InstaTech clients, you must have the .NET 4.5.2 developer pack installed (or Visual Studio)."
Wrap-Host
Wrap-Host "If you have not already done so, please download and install it from the following link:"
Wrap-Host
Wrap-Host "https://www.microsoft.com/net/download/thank-you/net452-developer-pack"
Wrap-Host
Read-Host "Press Enter to continue"
Clear-Host

### License Agreement ###
Wrap-Host
Wrap-Host "**********************************"
Wrap-Host "        License Agreement" -ForegroundColor Green
Wrap-Host "**********************************"
Wrap-Host
Wrap-Host "By continuing this installation, you're agreeing to the license terms published at:" -ForegroundColor Cyan
Wrap-Host
Wrap-Host "http://instatech.invis.me/Docs/InstaTech_Server_License.html" -ForegroundColor Cyan
Wrap-Host
Wrap-Host "If the web page is unavailable for any reason, please contact Translucency_Software@outlook.com for a copy before proceeding." -ForegroundColor Cyan
Wrap-Host
Wrap-Host "If you do not accept the agreement, please close this window now.  Otherwise, press Enter to agree and continue." -ForegroundColor Cyan
Wrap-Host
Read-Host "Press Enter to continue"

### Intro ###
Clear-Host
Wrap-Host
Wrap-Host "**********************************"
Wrap-Host "         InstaTech Setup" -ForegroundColor Cyan
Wrap-Host "**********************************"
Wrap-Host
Wrap-Host "Hello, and thank you for trying out InstaTech!" -ForegroundColor Green
Wrap-Host
Wrap-Host "This setup script will install InstaTech on this machine, or provide the files for deployment to a remote server.  If installing to this machine, please make sure you've already created a website in IIS where InstaTech will be installed." -ForegroundColor Green
Wrap-Host
Wrap-Host "If you encounter any problems or have any questions, please contact Translucency_Software@outlook.com." -ForegroundColor Green
Wrap-Host
Read-Host "Press Enter to continue"
Clear-Host

### Inputs ###
Clear-Host
Wrap-Host
Wrap-Host "**********************************"
Wrap-Host "          Configuration" -ForegroundColor Cyan
Wrap-Host "**********************************"
Wrap-Host
$CompanyName = Read-Host "What company name would you like to appear on the site?"
Wrap-Host
Wrap-Host
$HostName = Read-Host "What will be the host name of your site, minus the protocol (e.g. google.com)?"
Wrap-Host
Wrap-Host
Wrap-Host "Okay.  The apps will be recompiled using the company name of $CompanyName and will connect to the server at $HostName." -ForegroundColor Green
Wrap-Host
Wrap-Host
Wrap-Host "Build logs for each client application can be found in the InstaTech_Server root folder."
Wrap-Host
Wrap-Host
Read-Host "Press Enter to begin installation."
Clear-Host

### Download Client Files ###
try {
    if ((Test-Path -Path "$ParentFolder\InstaTech_Client\")){
        Remove-Item -Path "$ParentFolder\InstaTech_Client\" -Recurse -Force
    }
    Wrap-Host "Downloading client files..."
    Invoke-WebRequest -Uri "https://api.github.com/repos/Jay-Rad/InstaTech_Client/zipball/master" -OutFile "$ParentFolder\InstaTech_Client.zip"
    Wrap-Host "Extracting client files..."
	[System.Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") | Out-Null
	[System.IO.Compression.ZipFile]::ExtractToDirectory("$ParentFolder\InstaTech_Client.zip", "$ParentFolder\InstaTech_Client\")
    Get-ChildItem -Path (Get-ChildItem -Path "$ParentFolder\InstaTech_Client\").FullName | ForEach-Object {
        Move-Item -Path $_.FullName -Destination "$ParentFolder\InstaTech_Client\$($_.Name)"
    }
}
catch {
    Wrap-Host
    Wrap-Host "Error: Unable to download or extract client files." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    return
}


### Compiling ASP.NET Site ###
if ((Test-Path -Path "$Bin\Temp")){
    Remove-Item -Path "$Bin\Temp" -Force -Recurse
}
New-Item -Path "$Bin\Temp" -ItemType Directory | Out-Null
# Get Config.cs
[string[]]$Content = Get-Content -Path "$PSScriptRoot\InstaTech_Server\App_Code\Config.cs"
# Set company name.
$Index = $Content.IndexOf(($Content | Where-Object {$_ -like "*Company_Name { get; set; }*"}))
$Content[$Index] = $Content[$Index].Split("=")[0] + "= `"$CompanyName`";"

# Set updated Config.cs
Set-Content -Value $Content -Path "$PSScriptRoot\InstaTech_Server\App_Code\Config.cs" -Force

# Compile ASP.NET app.
Write-Host
Write-Host "Compiling ASP.NET website..."
$Proc = Start-Process -FilePath "$Bin\ASPNET\aspnet_compiler.exe" -ArgumentList "-v /InstaTech_Server -p $PSScriptRoot\InstaTech_Server -u $Bin\Temp -x /InstaTech_Server/App_Data" -PassThru -WindowStyle Hidden
while ($Proc.HasExited -eq $false) {
    Start-Sleep -Seconds 1
}

# Create downloads folder.
New-Item -Path "$Bin\Temp\Downloads" -ItemType "Directory" -Force | Out-Null

# Rebuild Notifier EXE.
Write-Host
[string[]]$Content = Get-Content -Path "$ParentFolder\InstaTech_Client\Notifier\MainWindow.xaml"
$Index = $Content.IndexOf(($Content | Where-Object {$_ -like "*<TextBlock Text=`"Services by*"}))
$Content[$Index] = "  <TextBlock Text=`"Services by $CompanyName`" FontSize=`"10`" FontStyle=`"Italic`" Margin=`"0,0,0,10`"></TextBlock>"
Set-Content -Value $Content -Path "$ParentFolder\InstaTech_Client\Notifier\MainWindow.xaml" -Force
Write-Host "Compiling Notifier..."
$Proc = Start-Process -FilePath "$Bin\MSBuild\MSBuild.exe" -ArgumentList "/property:Configuration=Release $ParentFolder\InstaTech_Client\Notifier\Notifier.csproj /fl /flp:logfile=NotifierBuildOutput.log" -PassThru -WindowStyle Hidden
while ($Proc.HasExited -eq $false) {
    Start-Sleep -Seconds 1
}
if ((Test-Path "$ParentFolder\InstaTech_Client\Notifier\bin\Release\Notifier.exe") -eq $false){
    Wrap-Host -Text "Failed to compile Notifier." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    return
}
Copy-Item -Path "$ParentFolder\InstaTech_Client\Notifier\bin\Release\Notifier.exe" -Destination "$ParentFolder\InstaTech_Client\InstaTech_Service\Resources\Notifier.exe" -Force

# Rebuild Service EXE.
Write-Host
Write-Host "Compiling service client..."
[string[]]$Content = Get-Content -Path "$ParentFolder\InstaTech_Client\InstaTech_Service\Socket.cs"
$Index = $Content.IndexOf(($Content | Where-Object {$_ -like "*const string hostName =*"} | Select-Object -Last 1))
$Content[$Index] = $Content[$Index].Split("=")[0] + "= `"$HostName`";"
Set-Content -Value $Content -Path "$ParentFolder\InstaTech_Client\InstaTech_Service\Socket.cs" -Force
$Proc = Start-Process -FilePath "$Bin\MSBuild\MSBuild.exe" -ArgumentList "/property:Configuration=Release $ParentFolder\InstaTech_Client\InstaTech_Service\InstaTech_Service.csproj /fl /flp:logfile=ServiceBuildOutput.log" -PassThru -WindowStyle Hidden
while ($Proc.HasExited -eq $false) {
    Start-Sleep -Seconds 1
}
if ((Test-Path "$ParentFolder\InstaTech_Client\InstaTech_Service\bin\Release\InstaTech_Service.exe") -eq $false){
    Wrap-Host -Text "Failed to compile InstaTech Service." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    return
}
Copy-Item -Path "$ParentFolder\InstaTech_Client\InstaTech_Service\bin\Release\InstaTech_Service.exe" -Destination "$Bin\Temp\Downloads\InstaTech_Service.exe" -Force
Copy-Item -Path "$ParentFolder\InstaTech_Client\InstaTech_Service\bin\Release\InstaTech_Service.exe" -Destination "$ParentFolder\InstaTech_Client\InstaTech_Client\Resources\InstaTech_Service.exe" -Force

# Rebuild Client EXE.
Write-Host
Write-Host "Compiling WPF client..."
[string[]]$Content = Get-Content -Path "$ParentFolder\InstaTech_Client\InstaTech_Client\MainWindow.xaml"
$Index = $Content.IndexOf(($Content | Where-Object {$_ -like "*<TextBlock Text=`"Services by*"}))
$Content[$Index] = "                    <TextBlock Text=`"Services by $CompanyName`" FontStyle=`"Italic`" VerticalAlignment=`"Top`" HorizontalAlignment=`"Left`"></TextBlock>"
Set-Content -Value $Content -Path "$ParentFolder\InstaTech_Client\InstaTech_Client\MainWindow.xaml" -Force
[string[]]$Content = Get-Content -Path "$ParentFolder\InstaTech_Client\InstaTech_Client\MainWindow.xaml.cs"
$Index = $Content.IndexOf(($Content | Where-Object {$_ -like "*const string hostName =*"} | Select-Object -Last 1))
$Content[$Index] = $Content[$Index].Split("=")[0] + "= `"$HostName`";"
Set-Content -Value $Content -Path "$ParentFolder\InstaTech_Client\InstaTech_Client\MainWindow.xaml.cs"
$Proc = Start-Process -FilePath "$Bin\MSBuild\MSBuild.exe" -ArgumentList "/property:Configuration=Release $ParentFolder\InstaTech_Client\InstaTech_Client\InstaTech_Client.csproj /fl /flp:logfile=ClientBuildOutput.log" -PassThru -WindowStyle Hidden
while ($Proc.HasExited -eq $false) {
    Start-Sleep -Seconds 1
}
if ((Test-Path "$ParentFolder\InstaTech_Client\InstaTech_Client\bin\Release\InstaTech_Client.exe") -eq $false){
    Wrap-Host -Text "Failed to compile InstaTech Client." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    return
}
Copy-Item -Path "$ParentFolder\InstaTech_Client\InstaTech_Client\bin\Release\InstaTech_Client.exe" -Destination "$Bin\Temp\Downloads\InstaTech_Client.exe" -Force

$Option = $null
while ($Option -eq $null) {
    Clear-Host
    Wrap-Host
    Wrap-Host "Building complete.  The compiled website can be found in $PSScriptRoot\Setup\Temp\."
    Wrap-Host
    Wrap-Host
    Wrap-Host "Do you want to manually copy the files somewhere or install them on this machine?"
    Wrap-Host
    Wrap-Host
    Wrap-Host "[0] - Open the folder so I can copy them manually."
    Wrap-Host "[1] - Install on this machine."
    Wrap-Host
    $Option = Read-Host "Choose an option"
    if ($Option -ne "0" -and $Option -ne "1") {
        $Option = $null
    }
    elseif ($Option -eq "0") {
        Start-Process -FilePath "$PSScriptRoot\Setup\Temp\"
        return
    }
}

### Automatic IIS Setup ###
if ($ServerCmdlets) {
    $RebootRequired = $false
    Wrap-Host
    Wrap-Host "Installing IIS components..." -ForegroundColor Green
    Write-Progress -Activity "IIS Component Installation" -Status "Installing web server" -PercentComplete (1/7*100)
    $Result = Add-WindowsFeature Web-Server
    if ($Result.RestartNeeded -like "Yes")
    {
        $RebootRequired = $true
    }
    Write-Progress -Activity "IIS Component Installation" -Status "Installing ASP.NET" -PercentComplete (2/7*100)
    $Result = Add-WindowsFeature Web-Asp-Net
    if ($Result.RestartNeeded -like "Yes")
    {
        $RebootRequired = $true
    }
    Write-Progress -Activity "IIS Component Installation" -Status "Installing ASP.NET 4.5" -PercentComplete (3/7*100)
    $Result = Add-WindowsFeature Web-Asp-Net45
    if ($Result.RestartNeeded -like "Yes")
    {
        $RebootRequired = $true
    }
    Write-Progress -Activity "IIS Component Installation" -Status "Installing web sockets" -PercentComplete (4/7*100)
    $Result = Add-WindowsFeature Web-WebSockets
    if ($Result.RestartNeeded -like "Yes")
    {
        $RebootRequired = $true
    }
    Write-Progress -Activity "IIS Component Installation" -Status "Installing IIS management tools" -PercentComplete (5/7*100)
    $Result = Add-WindowsFeature Web-Mgmt-Tools
    if ($Result.RestartNeeded -like "Yes")
    {
        $RebootRequired = $true
    }
    Write-Progress -Activity "IIS Component Installation" -Status "Installing web filtering" -PercentComplete (6/7*100)
    $Result = Add-WindowsFeature Web-Filtering
    if ($Result.RestartNeeded -like "Yes")
    {
        $RebootRequired = $true
    }

    Write-Progress -Activity "IIS Component Installation" -Status "IIS setup completed" -PercentComplete (7/7*100) -Completed
    Start-Sleep 2
}
else
{
    Wrap-Host
    Wrap-Host "Installing IIS components..." -ForegroundColor Green
    Write-Progress -Activity "IIS Component Installation" -Status "Installing web server" -PercentComplete (1/7*100)
    DISM /Online /Enable-Feature /FeatureName:IIS-WebServer /All /Quiet

    Write-Progress -Activity "IIS Component Installation" -Status "Installing ASP.NET" -PercentComplete (2/7*100)
    DISM /Online /Enable-Feature /FeatureName:IIS-ASPNET /All /Quiet

    Write-Progress -Activity "IIS Component Installation" -Status "Installing ASP.NET 4.5" -PercentComplete (3/7*100)
    DISM /Online /Enable-Feature /FeatureName:IIS-ASPNET45 /All /Quiet

    Write-Progress -Activity "IIS Component Installation" -Status "Installing web sockets" -PercentComplete (4/7*100)
    DISM /Online /Enable-Feature /FeatureName:IIS-WebSockets /All /Quiet

    Write-Progress -Activity "IIS Component Installation" -Status "Installing IIS management tools" -PercentComplete (5/7*100)
    DISM /Online /Enable-Feature /FeatureName:IIS-ManagementConsole /All /Quiet

    Write-Progress -Activity "IIS Component Installation" -Status "Installing web filtering" -PercentComplete (6/7*100)
    DISM /Online /Enable-Feature /FeatureName:IIS-RequestFiltering /All /Quiet

    Write-Progress -Activity "IIS Component Installation" -Status "IIS setup completed" -PercentComplete (7/7*100) -Completed
    Start-Sleep 2

}
# Add AppImage MIME type.
Add-WebConfigurationProperty -Filter //staticContent  -Name Collection -Value @{fileExtension='.AppImage'; mimeType='application/octet-stream'}

Clear-Host
$Sites = Get-Website
### Site Selection ###
while ($Website -eq $null) {
    Wrap-Host
    $Sites
    Wrap-Host
    Wrap-Host "Enter the ID of the website where InstaTech will be installed." -ForegroundColor Green
    Wrap-Host
    $ID = Read-Host "Website ID"

    $Website = Get-Website | Where-Object {$_.ID -like $ID}
}
$InstallPath = $Website.physicalPath.Replace("%SystemDrive%", $env:SystemDrive)

Wrap-Host
Wrap-Host "This will DELETE ALL FILES in the selected website and install InstaTech Server.  If this is not your intention, close this window now and create a new website where InstaTech Server will be installed." -ForegroundColor Red
Wrap-Host
pause

# Stop site.
Clear-Host
Wrap-Host
Wrap-Host "Stopping website..." -ForegroundColor Green
Stop-Website -Name $($Website.name)

# Set bindings.
Wrap-Host
Wrap-Host "Setting bindings..." -ForegroundColor Green
$Bindings = Get-WebBinding
if (($Bindings.bindingInformation | Out-String).Contains(":80:") -eq $false)
{
    New-WebBinding -Name $Website.name -Protocol "http" -HostHeader $HostName -Port 80
}
if (($Bindings.bindingInformation | Out-String).Contains(":443:") -eq $false)
{
    New-WebBinding -Name $Website.name -Protocol "https" -HostHeader $HostName -Port 443 -SslFlags 1
}

### File Cleanup ###
Wrap-Host
Wrap-Host "Cleaning up existing files..." -ForegroundColor Green
$ExistingFiles = Get-ChildItem -Path "$InstallPath" -Recurse
foreach ($File in $ExistingFiles)
{
    if ($File.FullName -notlike "*\App_Data*" -and $File.FullName -notlike "*\Override*")
    {
        try
        {
            Remove-Item -Path $File.FullName -Force -Recurse -ErrorAction SilentlyContinue
        }
        catch
        {
            Retry-Block -TryCount 0 -ErrorVar $CopyErrors -ScriptBlock {
                Remove-Item -Path $File.FullName -Force -Recurse -ErrorAction SilentlyContinue
            }
            continue
        }
        
    }
}

### File Copy ###
Wrap-Host
Wrap-Host "Copying new files..." -ForegroundColor Green
$Files = Get-ChildItem -Path "$Bin\Temp" -Recurse
foreach ($File in $Files) {
    $destPath = $File.FullName.Replace("$Bin\Temp", "$InstallPath")
    if ($destPath -like "*App_Data*" -and (Test-Path -Path $destPath)) {
        continue
    }
    else {
        try
        {
            if ([System.IO.Directory]::Exists([System.IO.Path]::GetDirectoryName($destPath)) -eq $false) {
                [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($destPath))
            }
            Copy-Item -Path $File.FullName -Destination $destPath -Force
        }
        catch
        {
            Retry-Block -TryCount 0 -ErrorVar $CopyErrors -ScriptBlock {
                if ([System.IO.Directory]::Exists([System.IO.Path]::GetDirectoryName($destPath)) -eq $false) {
                    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($destPath))
                }
                Copy-Item -Path $File.FullName -Destination $destPath -Force
            }
            continue
        }
    }
}
if (!(Test-Path -Path "$InstallPath\App_Data")) {
    New-Item -Path "$InstallPath\App_Data" -ItemType Directory | Out-Null
}
### Set ACL on website folders and files ###
Wrap-Host
Wrap-Host "Setting ACLs..." -ForegroundColor Green
$Acl = Get-Acl -Path $InstallPath
$Rule = New-Object System.Security.AccessControl.FileSystemAccessRule("BUILTIN\IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$Acl.AddAccessRule($Rule)
$Acl.SetOwner((New-Object System.Security.Principal.NTAccount("Administrators")))
Get-ChildItem -Path $InstallPath -Recurse | ForEach-Object {
    Set-Acl -Path $_.FullName -AclObject $Acl   
}

### Firewall Rules ###
Wrap-Host
Wrap-Host "Checking firewall rules for HTTP/HTTPS..." -ForegroundColor Green
try
{
    Enable-NetFirewallRule -Name "IIS-WebServerRole-HTTP-In-TCP"
    Enable-NetFirewallRule -Name "IIS-WebServerRole-HTTPS-In-TCP"
    if ((Get-NetFirewallRule -Name "IIS-WebServerRole-HTTP-In-TCP").Enabled -like "False" -or (Get-NetFirewallRule -Name "IIS-WebServerRole-HTTP-In-TCP").Enabled -like "False")
    {
        $FirewallSet = $false
    }
    else
    {
        $FirewallSet = $true
    }
}
catch
{
    $FirewallSet = $false
}

# Add custom WebSocket port.
[string[]]$Content = Get-Content -Path "$InstallPath\scripts\Main_Model.ts"
$Index = $Content.IndexOf(($Content | Where-Object {$_ -like "*InstaTech.Socket_Port =*"}))
if (!$Content[$Index].Contains(" 80`";")) {
    $Port = $Content[$Index].Split("=".ToCharArray())[1].Replace(";", "").Replace("`"", "").Trim()
    if (($Bindings.bindingInformation | Out-String).Contains(":$Port`:") -eq $false) {
        try {
            New-WebBinding -Name $Website.name -Protocol "http" -Port $Port -HostHeader $HostName
        }
        catch{}
    }
        
}
$Index = $Content.IndexOf(($Content | Where-Object {$_ -like "*InstaTech.Secure_Socket_Port =*"}))
if (!$Content[$Index].Contains(" 443`";")) {
    $Port = $Content[$Index].Split("=".ToCharArray())[1].Replace(";", "").Replace("`"", "").Trim()
    if (($Bindings.bindingInformation | Out-String).Contains(":$Port`:") -eq $false) {
        try {
            New-WebBinding -Name $Website.name -Protocol "https" -Port $Port -HostHeader $HostName -SslFlags 1
        }
        catch{}
    }
}

# Start website.
Start-Website -Name $($Website.name)

Wrap-Host
Wrap-Host
Wrap-Host
Wrap-Host "**********************************"
Wrap-Host "      Server setup complete!" -ForegroundColor Green
Wrap-Host "**********************************"
Wrap-Host
Wrap-Host "SSL needs to be set up in IIS, if it's not already.  This process needs to be completed manually.  Check the post-installation notes for details." -ForegroundColor Green

if ($RebootRequired) {
    Wrap-Host
    Wrap-Host "A reboot is required for the new IIS components to work properly.  Please reboot your computer at your earliest convenience." -ForegroundColor Red
}
if ($FirewallSet -eq $false)
{
    Wrap-Host
    Wrap-Host "Firewall rules were not properly set.  Please ensure that ports 80 (HTTP) and 443 (HTTPS) are open.  Windows Firewall has predefined rules for these called ""World Wide Web Services (HTTP(S) Traffic-In)""." -ForegroundColor Red
}

if ($CopyErrors)
{
    Wrap-Host
    Wrap-Host "There were errors copying some of the server files.  Please try deleting all files in the website directory and trying again." -ForegroundColor Red
}
Wrap-Host
Read-Host "Press Enter to exit and view Quick Start"
Start-Process -FilePath "$env:SystemDrive\Program Files\Internet Explorer\iexplore.exe" -ArgumentList "$InstallPath\Docs\Quick_Start.html"