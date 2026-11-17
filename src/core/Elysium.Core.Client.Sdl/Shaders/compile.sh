#!/bin/sh
# Compiles every *.vert.hlsl and *.frag.hlsl in this folder for SDL3 GPU, into Compiled/:
#   *.dxil  for Direct3D 12, with the DXC of the Windows SDK (it signs the shader with dxil.dll);
#   *.spv   for Vulkan, with the DXC of the Vulkan SDK (built with SPIR-V support).
# The compiled files are committed (TD in docs/TECH_DEBT.md); run this after changing a shader:
#   sh src/core/Elysium.Core.Client.Sdl/Shaders/compile.sh
# Override the compilers with DXC_DXIL and DXC_SPIRV if they live somewhere else.
set -eu

cd "$(dirname "$0")"

if [ -z "${DXC_DXIL:-}" ]; then
    DXC_DXIL=$(ls -d "/c/Program Files (x86)/Windows Kits/10/bin/"10.*/x64/dxc.exe 2>/dev/null | sort | tail -n 1)
fi
if [ -z "${DXC_SPIRV:-}" ]; then
    DXC_SPIRV="${VULKAN_SDK:-}/Bin/dxc.exe"
fi

for compiler in "$DXC_DXIL" "$DXC_SPIRV"; do
    if [ ! -x "$compiler" ]; then
        echo "DXC not found: '$compiler'. Install the Windows SDK and the Vulkan SDK, or set DXC_DXIL and DXC_SPIRV." >&2
        exit 1
    fi
done

mkdir -p Compiled
for source in *.vert.hlsl *.frag.hlsl; do
    name=${source%.hlsl}
    case "$source" in
        *.vert.hlsl) profile=vs_6_0 ;;
        *) profile=ps_6_0 ;;
    esac
    "$DXC_DXIL" -nologo -T "$profile" -E main -O3 -Fo "Compiled/$name.dxil" "$source"
    "$DXC_SPIRV" -nologo -T "$profile" -E main -O3 -spirv -Fo "Compiled/$name.spv" "$source"
    echo "compiled $source"
done
