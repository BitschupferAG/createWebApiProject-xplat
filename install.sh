#!/usr/bin/env bash
# Installer für createWebApiProject-xplat (macOS und Linux).
#   curl -fsSL https://raw.githubusercontent.com/BitschupferAG/createWebApiProject-xplat/main/install.sh | bash
# Kann gefahrlos mehrfach ausgeführt werden; ein zweiter Lauf aktualisiert.
set -euo pipefail

REPO="BitschupferAG/createWebApiProject-xplat"
INSTALL_DIR="$HOME/.createWebApiProject-xplat"
DOTNET_DIR="$HOME/.dotnet"
MARK_START="# >>> createWebApiProject-xplat >>>"
MARK_END="# <<< createWebApiProject-xplat <<<"

info() { printf '\033[96m%s\033[0m\n' "$*"; }
fail() { printf '\033[91mFehler: %s\033[0m\n' "$*" >&2; exit 1; }
command -v curl >/dev/null || fail "curl fehlt."
command -v tar >/dev/null || fail "tar fehlt."

export PATH="$DOTNET_DIR:$DOTNET_DIR/tools:$PATH"
sdk_major() { local v; v=$(cd "$HOME" && dotnet --version 2>/dev/null) || v=0; echo "${v%%.*}"; }

# 1. .NET 10 SDK
if [ "$(sdk_major)" -lt 10 ]; then
  info "Installiere .NET 10 SDK nach $DOTNET_DIR ..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir "$DOTNET_DIR"
  [ "$(sdk_major)" -ge 10 ] || fail ".NET 10 SDK lässt sich nicht starten. Unter Linux fehlt evtl. libicu (z.B. sudo apt install libicu-dev)."
else
  info ".NET SDK $(cd "$HOME" && dotnet --version) ist bereits installiert."
fi
export DOTNET_ROOT="${DOTNET_ROOT:-$(dirname "$(readlink -f "$(command -v dotnet)")")}"

# 2. dotnet-ef 10
if dotnet tool list -g | grep -qi '^dotnet-ef '; then
  info "Aktualisiere dotnet-ef ..."
  dotnet tool update -g dotnet-ef --version '10.*'
else
  info "Installiere dotnet-ef ..."
  dotnet tool install -g dotnet-ef --version '10.*'
fi

# 3. Tool herunterladen
info "Lade createWebApiProject-xplat nach $INSTALL_DIR ..."
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
curl -fsSL "https://github.com/$REPO/archive/refs/heads/main.tar.gz" | tar -xz -C "$TMP"
rm -rf "$INSTALL_DIR"
mv "$TMP/createWebApiProject-xplat-main" "$INSTALL_DIR"
chmod +x "$INSTALL_DIR/createWebApi-xplat" "$INSTALL_DIR/createMinApi-xplat" "$INSTALL_DIR/createAllApi-xplat"

# 4. PATH in Shell-Konfiguration eintragen (alter Block wird ersetzt)
BLOCK="$MARK_START
export DOTNET_ROOT=\"$DOTNET_ROOT\"
export PATH=\"$INSTALL_DIR:$DOTNET_ROOT:$DOTNET_DIR/tools:\$PATH\"
$MARK_END"
RCS=()
{ [ -f "$HOME/.zshrc" ] || [ "${SHELL##*/}" = zsh ]; } && RCS+=("$HOME/.zshrc")
{ [ -f "$HOME/.bashrc" ] || [ "${SHELL##*/}" = bash ]; } && RCS+=("$HOME/.bashrc")
[ ${#RCS[@]} -gt 0 ] || RCS+=("$HOME/.profile")
for rc in "${RCS[@]}"; do
  touch "$rc"
  awk -v s="$MARK_START" -v e="$MARK_END" '$0==s{skip=1} !skip{print} $0==e{skip=0}' "$rc" > "$rc.tmp"
  printf '%s\n' "$BLOCK" >> "$rc.tmp"
  mv "$rc.tmp" "$rc"
  info "PATH eingetragen in $rc"
done

printf '\n\033[92mFertig!\033[0m Öffne ein neues Terminal und probiere:\n  createWebApi-xplat MyBackend .\n'
