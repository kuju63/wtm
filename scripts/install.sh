#!/bin/sh
# install.sh - wtm installer for Unix (macOS/Linux)
# Usage: curl -fsSL https://kuju63.github.io/wt/install.sh | sh
#        curl -fsSL https://kuju63.github.io/wt/install.sh | sh -s -- --prefix /usr/local
#        curl -fsSL https://kuju63.github.io/wt/install.sh | sh -s -- --force
# shellcheck disable=SC3043  # 'local' is widely supported in sh implementations (bash, dash, ash, ksh)
set -e

# --- Variables ---
INSTALL_DIR="${HOME}/.local/bin"
FORCE=false
LATEST_VERSION=""
PLATFORM_TAG=""
ARCH_TAG=""
BINARY_NAME=""
DOWNLOAD_URL=""
HASH_URL=""
INSTALL_PATH=""

# --- Functions ---

error_exit() {
    echo "Error: $1" >&2
    shift
    while [ $# -gt 0 ]; do
        echo "       $1" >&2
        shift
    done
    exit 1
}

download() {
    local url="$1"
    local dest="$2"
    if command -v curl >/dev/null 2>&1; then
        curl -fsSL -o "$dest" "$url"
    elif command -v wget >/dev/null 2>&1; then
        wget -q -O "$dest" "$url"
    else
        error_exit "Neither curl nor wget found. Install one and retry." \
            "Manual download: https://github.com/kuju63/wt/releases"
    fi
}

download_stdout() {
    local url="$1"
    if command -v curl >/dev/null 2>&1; then
        curl -fsSL "$url"
    elif command -v wget >/dev/null 2>&1; then
        wget -q -O - "$url"
    else
        error_exit "Neither curl nor wget found. Install one and retry." \
            "Manual download: https://github.com/kuju63/wt/releases"
    fi
}

is_interactive() {
    [ -t 0 ]
}

handle_existing_install() {
    local install_path="$1"
    local current_version="$2"

    if [ ! -f "$install_path" ]; then
        return 0
    fi

    if [ "$FORCE" = "true" ]; then
        return 0
    fi

    if ! is_interactive; then
        echo "Note: $install_path already exists. Skipping overwrite in non-interactive mode."
        echo "To force overwrite, re-run with --force:"
        echo "  curl -fsSL https://kuju63.github.io/wt/install.sh | sh -s -- --force"
        exit 0
    fi

    printf "Overwrite existing %s? [y/N] " "$install_path"
    read -r answer < /dev/tty
    case "$answer" in
        [yY])
            return 0
            ;;
        *)
            echo "Installation cancelled."
            exit 0
            ;;
    esac
}

detect_platform() {
    local os
    local arch
    os=$(uname -s | tr '[:upper:]' '[:lower:]')
    arch=$(uname -m)

    case "$arch" in
        x86_64|amd64)
            ARCH_TAG="x64"
            ;;
        aarch64|arm64)
            ARCH_TAG="arm64"
            ;;
        armv7l|armhf|arm*)
            ARCH_TAG="arm"
            ;;
        *)
            error_exit "Unsupported architecture: $arch" \
                "Supported: x64 (Linux/macOS), arm64 (macOS only), arm (Linux only)" \
                "Manual install: https://github.com/kuju63/wt/releases"
            ;;
    esac

    case "$os" in
        linux)
            PLATFORM_TAG="linux"
            ;;
        darwin)
            PLATFORM_TAG="macos"
            ;;
        *)
            error_exit "Unsupported OS: $os. Supported: linux-x64, linux-arm, macos-arm64" \
                "Manual install: https://github.com/kuju63/wt/releases"
            ;;
    esac

    if [ "$PLATFORM_TAG" = "linux" ] && [ "$ARCH_TAG" = "arm64" ]; then
        error_exit "Unsupported platform: linux-arm64" \
            "Supported: linux-x64, linux-arm, macos-arm64" \
            "Manual install: https://github.com/kuju63/wt/releases"
    fi

    echo "Detecting platform... ${PLATFORM_TAG}-${ARCH_TAG}"
}

fetch_latest_version() {
    local api_response
    api_response=$(download_stdout "https://api.github.com/repos/kuju63/wt/releases/latest" 2>/dev/null) || true

    LATEST_VERSION=$(echo "$api_response" | grep '"tag_name"' | sed -E 's/.*"([^"]+)".*/\1/')

    if [ -z "$LATEST_VERSION" ]; then
        error_exit "Failed to fetch latest version from GitHub API." \
            "This may be due to rate limiting. Please wait a moment and retry." \
            "Manual install: https://github.com/kuju63/wt/releases"
    fi

    echo "Fetching latest version... $LATEST_VERSION"
}

check_existing_install() {
    INSTALL_PATH="${INSTALL_DIR}/wtm"

    if [ -f "$INSTALL_PATH" ]; then
        local current_version
        current_version=$("$INSTALL_PATH" --version 2>/dev/null | head -1) || current_version="unknown"

        if [ "$current_version" = "$LATEST_VERSION" ]; then
            echo "wtm ${LATEST_VERSION} is already the latest version. Skipping."
            exit 0
        fi

        echo "wtm $current_version is already installed. Updating to $LATEST_VERSION..."
        handle_existing_install "$INSTALL_PATH" "$current_version"
    fi
}

build_urls() {
    BINARY_NAME="wtm-${LATEST_VERSION}-${PLATFORM_TAG}-${ARCH_TAG}"
    DOWNLOAD_URL="https://github.com/kuju63/wt/releases/download/${LATEST_VERSION}/${BINARY_NAME}"
    HASH_URL="${DOWNLOAD_URL}.sha256"
}

compute_sha256() {
    if command -v sha256sum >/dev/null 2>&1; then
        sha256sum "$1" | cut -d' ' -f1
    elif command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$1" | cut -d' ' -f1
    else
        error_exit "No SHA256 tool found (sha256sum or shasum required)."
    fi
}

download_and_verify() {
    local tmp_dir
    tmp_dir=$(mktemp -d)
    local bin_path="${tmp_dir}/${BINARY_NAME}"
    local hash_path="${tmp_dir}/${BINARY_NAME}.sha256"

    echo "Downloading wtm ${LATEST_VERSION} for ${PLATFORM_TAG}-${ARCH_TAG}..."
    download "$DOWNLOAD_URL" "$bin_path" || {
        rm -rf "$tmp_dir"
        error_exit "Failed to download from ${DOWNLOAD_URL}." \
            "Check your network connection."
    }

    echo "Verifying SHA256 checksum..."
    download "$HASH_URL" "$hash_path" || {
        rm -rf "$tmp_dir"
        error_exit "Failed to download checksum file." \
            "Check your network connection."
    }

    local expected
    local actual
    expected=$(cut -d' ' -f1 < "$hash_path")
    actual=$(compute_sha256 "$bin_path")

    if [ "$expected" != "$actual" ]; then
        rm -rf "$tmp_dir"
        error_exit "SHA256 verification failed for ${BINARY_NAME}." \
            "Downloaded file has been removed. Please retry." \
            "Manual install: https://github.com/kuju63/wt/releases"
    fi

    echo "Installing to ${INSTALL_PATH}..."
    mkdir -p "$INSTALL_DIR"
    mv "$bin_path" "$INSTALL_PATH"
    chmod +x "$INSTALL_PATH"

    rm -rf "$tmp_dir"
}

check_path() {
    if ! echo "$PATH" | tr ':' '\n' | grep -qx "$INSTALL_DIR"; then
        echo ""
        echo "Note: ${INSTALL_DIR} is not in your PATH."
        echo "To add it, run:"
        printf '  echo '"'"'export PATH="%s:$PATH"'"'"' >> ~/.bashrc\n' "$INSTALL_DIR"
        printf '  source ~/.bashrc\n'
        echo "  (or ~/.zshrc for Zsh, ~/.profile for other shells)"
    fi
}

print_gatekeeper_note() {
    if [ "$PLATFORM_TAG" = "macos" ]; then
        echo ""
        echo "Note: On macOS, if you see 'cannot be opened because the developer cannot be verified':"
        echo "  xattr -d com.apple.quarantine ${INSTALL_PATH}"
    fi
}

print_success() {
    echo ""
    echo "✓ wtm ${LATEST_VERSION} installed successfully!"
    echo ""
    echo "Next steps:"
    echo "  wtm --version   # verify installation"
    echo "  wtm --help      # see available commands"
}

# --- Argument Parsing ---
while [ $# -gt 0 ]; do
    case "$1" in
        --prefix)
            if [ -z "$2" ]; then
                error_exit "--prefix requires a directory argument."
            fi
            INSTALL_DIR="$2"
            shift 2
            ;;
        --force)
            FORCE=true
            shift
            ;;
        --)
            shift
            break
            ;;
        -*)
            error_exit "Unknown option: $1"
            ;;
        *)
            shift
            ;;
    esac
done

# --- Main ---
detect_platform
fetch_latest_version
check_existing_install
build_urls
download_and_verify
check_path
print_gatekeeper_note
print_success
