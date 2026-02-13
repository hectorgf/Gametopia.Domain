<#
.SYNOPSIS
    Script to manage Gametopia.Domain application version

.DESCRIPTION
    This script helps get and increment the application version in the .csproj file.
    It's primarily used in CI/CD pipelines to automate versioning.

.PARAMETER Action
    The action to perform: 'Get' or 'Increment'

.EXAMPLE
    .\update-version.ps1 -Action Get
    .\update-version.ps1 -Action Increment

.NOTES
    Author: CI/CD Pipeline
    Date: February 2026
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Get', 'Increment')]
    [string]$Action
)

$ErrorActionPreference = 'Stop'

$projectFile = "Gametopia.Domain.Api\Gametopia.Domain.Api.csproj"
$versionPattern = '<Version>(\d+\.\d+\.\d+)<\/Version>'

function Get-CurrentVersion {
    try {
        $content = Get-Content -Path $projectFile -Raw
        
        if ($content -match $versionPattern) {
            return $matches[1]
        }
        else {
            Write-Host "No version found in $projectFile, using default 0.0.0"
            return "0.0.0"
        }
    }
    catch {
        Write-Error "Failed to read version from $projectFile : $_"
        exit 1
    }
}

function Increment-PatchVersion {
    param([string]$Version)
    
    $parts = $Version.Split('.')
    [int]$major = $parts[0]
    [int]$minor = $parts[1]
    [int]$patch = $parts[2]
    
    $patch++
    return "$major.$minor.$patch"
}

function Update-VersionInCsproj {
    param([string]$NewVersion)
    
    try {
        $content = Get-Content -Path $projectFile -Raw
        
        if ($content -match $versionPattern) {
            $content = $content -replace $versionPattern, "<Version>$NewVersion</Version>"
        }
        else {
            # Add Version element after PropertyGroup start, if not exists
            $content = $content -replace '(<PropertyGroup>)', "`$1`n    <Version>$NewVersion</Version>"
        }
        
        Set-Content -Path $projectFile -Value $content
        Write-Host "✓ Version updated to $NewVersion in $projectFile"
    }
    catch {
        Write-Error "Failed to update version in $projectFile : $_"
        exit 1
    }
}

switch ($Action) {
    'Get' {
        $currentVersion = Get-CurrentVersion
        Write-Host "Current version: $currentVersion"
        return $currentVersion
    }
    
    'Increment' {
        $currentVersion = Get-CurrentVersion
        Write-Host "Current version: $currentVersion"
        
        $newVersion = Increment-PatchVersion $currentVersion
        Write-Host "New version: $newVersion"
        
        Update-VersionInCsproj $newVersion
        
        return $newVersion
    }
}
