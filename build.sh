#!/usr/bin/env bash
# Build script para Render

set -e

echo "Installing .NET SDK..."
# Render ya tiene .NET instalado

echo "Restoring packages..."
dotnet restore

echo "Building project..."
dotnet build -c Release

echo "Publishing project..."
dotnet publish -c Release -o out

echo "Build completed successfully!"
