#!/usr/bin/env bash
set -euo pipefail

# replace this with your atlus script tools dir
COMPILER="/home/luna/Games/p5r modding/Atlus-Script-Tools/AtlusScriptCompiler.exe"

echo "Building BF Files"

mkdir -p ./bin

shopt -s nullglob
for src_file in ./src/*.flow; do
    name="$(basename "$src_file" .flow)"

    wine "$COMPILER" \
        "./src/$(basename "$src_file")" \
        -Compile \
        -Out "./bin/${name}.bf" \
        -OutFormat V3BE \
        -Library P5R \
        -Encoding P5R

    dest_file="./src/${name}.dst"

    if [ -f "$dest_file" ]; then
        
        local_dest=""
        while IFS= read -r line || [ -n "$line" ]; do
            local_dest="$line"
        done < "$dest_file"

        echo "$local_dest"

    
        local_dest="/${local_dest##*/}"

        dest="../P5REssentials${local_dest}"
        echo "$dest"

        mv "./bin/${name}.bf" "$dest"
    fi
done
