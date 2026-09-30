#!/usr/bin/env bash
# Copies the Kit (../UI-Kit) into this sandbox's Assets, the way a game copies
# it into its own project. Run it after cloning and after changing the Kit;
# Assets/UI-Kit is gitignored, so the Kit's source of truth stays ../UI-Kit.
set -euo pipefail

root="$(cd "$(dirname "$0")" && pwd)"
src="$root/../UI-Kit"
dest="$root/Assets/UI-Kit"

if [ ! -d "$src" ]; then
  echo "[sandbox] No Kit at $src — nothing copied, Assets/UI-Kit left alone." >&2
  exit 1
fi

rm -rf "$dest"
mkdir -p "$dest"
cp -R "$src/." "$dest/"
echo "[sandbox] Copied $src -> Assets/UI-Kit"
