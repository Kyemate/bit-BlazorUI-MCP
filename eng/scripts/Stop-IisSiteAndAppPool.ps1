# Copyright (c) 2026 Mud MCP Contributors
# Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

<#
.SYNOPSIS
    Stops an IIS application pool gracefully.

.DESCRIPTION
    Stops the specified IIS application pool if it exists and is running.
    Waits for the pool to reach a stable state before attempting to stop it.

.PARAMETER AppPoolName
    The name of the IIS application pool to stop.

.EXAMPLE
    .\Stop-IisSiteAndAppPool.ps1 -AppPoolName "MudBlazorMcpPool"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppPoolName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

# Validate app pool name (alphanumeric, underscores, hyphens only)
if ($AppPoolName -notmatch '^[a-zA-Z0-9_-]+$') {
    Write-Error "Invalid app pool name. Only alphanumeric characters, underscores, and hyphens are allowed."
    exit 1
}

Import-Module WebAdministration -ErrorAction SilentlyContinue

# Check if app pool exists
if (Get-IISAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
    $appPool = Get-IISAppPool -Name $AppPoolName
    
    # Wait for pool to reach stable state first
    $stableStates = @('Started', 'Stopped')
    $timeout = 30
    $elapsed = 0
    while ($appPool.State -notin $stableStates -and $elapsed -lt $timeout) {
        Write-Host "Waiting for app pool to reach stable state (current: $($appPool.State))..."
        Start-Sleep -Seconds 1
        $appPool = Get-IISAppPool -Name $AppPoolName
        $elapsed++
    }
    
    if ($appPool.State -eq 'Started') {
        Write-Host "Stopping application pool: $AppPoolName"
        Stop-WebAppPool -Name $AppPoolName
        
        # Wait for pool to stop
        $elapsed = 0
        while ((Get-IISAppPool -Name $AppPoolName).State -ne 'Stopped' -and $elapsed -lt $timeout) {
            Start-Sleep -Seconds 1
            $elapsed++
        }
        Write-Host "Application pool stopped."
    } else {
        Write-Host "Application pool is already stopped."
    }
} else {
    Write-Host "Application pool does not exist. Will be created during deployment."
}

exit 0
