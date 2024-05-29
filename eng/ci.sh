#!/bin/bash

# Exit script on first error
set -e

ACTION=${1}
CONFIGURATION=${DOTNET_CONFIGURATION:-"Release"}
NUGET_SOURCE=${NUGET_SOURCE:-"https://api.nuget.org/v3/index.json"}

log() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1"
}

clean() {
    pushd "$(git rev-parse --show-toplevel)" > /dev/null || exit
    log "Cleaning directories..."
    rm -rf ./**/bin ./**/obj ./artifacts
    popd > /dev/null || exit
}

restore() {
    pushd "$(git rev-parse --show-toplevel)" > /dev/null || exit
    log "Restoring NuGet packages..."
    dotnet restore --nologo --verbosity quiet --no-cache --force --ignore-failed-sources
    popd > /dev/null || exit
}

build() {
    pushd "$(git rev-parse --show-toplevel)" > /dev/null || exit
    log "Building the solution..."
    dotnet build --no-restore --nologo -c "$CONFIGURATION"
    popd > /dev/null || exit
}

pack() {
    GIT_ROOT=$(git rev-parse --show-toplevel)
    pushd "$GIT_ROOT" > /dev/null || exit
    log "Packing Packables..."
    dotnet pack --no-restore --no-build --nologo -c "$CONFIGURATION" -o "$GIT_ROOT/artifacts"
    popd > /dev/null || exit
}

push() {
    if [ -z "$NUGET_API_KEY" ]; then
        echo "NUGET_API_KEY environment variable is not set."
        exit 1
    fi
    
    pushd "$(git rev-parse --show-toplevel)" > /dev/null || exit
    log "Pushing packages to NuGet..."
    dotnet nuget push ./artifacts/*.nupkg -s "$NUGET_SOURCE" -k $NUGET_API_KEY
    popd > /dev/null || exit
}

local_ci() {
    clean
    restore
    build
    pack
}

if [[ "$CONFIGURATION" != "Release" && "$CONFIGURATION" != "Debug" ]]; then
    echo "Invalid configuration: DOTNET_CONFIGURATION environental variable must be 'Release' or 'Debug'."
    exit 1
fi

if declare -f "$ACTION" > /dev/null
then
  "$ACTION"
else
  echo "'$ACTION' is not a known function name" >&2
  exit 1
fi