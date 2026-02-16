#!/bin/bash

##############################################################################
# Script: update-version.sh
# Purpose: Automatically update application version for production releases
# Usage: ./scripts/update-version.sh [--get | --increment]
##############################################################################

set -e

PROJECT_FILE="Gametopia.Domain.Api/Gametopia.Domain.Api.csproj"
VERSION_REGEX='<Version>([0-9]+\.[0-9]+\.[0-9]+)<\/Version>'

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

##############################################################################
# Functions
##############################################################################

print_info() {
  echo -e "${GREEN}[INFO]${NC} $1"
}

print_warn() {
  echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
  echo -e "${RED}[ERROR]${NC} $1"
}

get_current_version() {
  if grep -q "$VERSION_REGEX" "$PROJECT_FILE"; then
    grep -oP "<Version>\K[^<]+" "$PROJECT_FILE"
  else
    echo "0.0.0"
  fi
}

increment_patch_version() {
  local version=$1
  IFS='.' read -r major minor patch <<< "$version"
  patch=$((patch + 1))
  echo "$major.$minor.$patch"
}

update_version_in_csproj() {
  local new_version=$1
  
  if grep -q "$VERSION_REGEX" "$PROJECT_FILE"; then
    sed -i "s/<Version>.*<\/Version>/<Version>$new_version<\/Version>/" "$PROJECT_FILE"
  else
    sed -i "/<PropertyGroup>/a\\    <Version>$new_version</Version>" "$PROJECT_FILE"
  fi
}

##############################################################################
# Main
##############################################################################

case "${1:-}" in
  --get)
    current=$(get_current_version)
    echo "$current"
    exit 0
    ;;
  
  --increment)
    current=$(get_current_version)
    print_info "Current version: $current"
    
    new=$(increment_patch_version "$current")
    print_info "New version: $new"
    
    update_version_in_csproj "$new"
    print_info "Version updated successfully in $PROJECT_FILE"
    
    echo "$new"
    exit 0
    ;;
  
  *)
    print_error "Invalid argument: $1"
    echo "Usage: $0 [--get | --increment]"
    echo ""
    echo "Options:"
    echo "  --get           Get current version"
    echo "  --increment     Increment patch version and update .csproj"
    exit 1
    ;;
esac
