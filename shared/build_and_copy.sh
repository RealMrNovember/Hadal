#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="${ROOT}/HADAL.Shared/HADAL.Shared.csproj"
OUT_DIR="${ROOT}/../Assets/_Hadal/Plugins/Shared"
DLL_NAME="HADAL.Shared.dll"

dotnet build "${PROJECT}" -c Release

mkdir -p "${OUT_DIR}"
cp -f "${ROOT}/HADAL.Shared/bin/Release/netstandard2.1/${DLL_NAME}" "${OUT_DIR}/${DLL_NAME}"

echo "Copied ${DLL_NAME} -> ${OUT_DIR}"
