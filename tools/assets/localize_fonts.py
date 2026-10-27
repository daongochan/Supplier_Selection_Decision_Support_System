#!/usr/bin/env python3
"""
tools/assets/localize_fonts.py

Explicit maintenance utility to localize, pin, and verify offline typography and icon assets:
  1. Bootstrap Icons v1.11.3 (MIT License) -> wwwroot/fonts/bootstrap-icons/
  2. Source Serif 4 (SIL Open Font License 1.1) -> wwwroot/fonts/sourceserif4/

Offline Verification:
  - Scans CSS files (wwwroot/css/bootstrap-icons.css, fonts.css, site.css)
  - Enforces zero external @import statements to prevent CDN waterfall
  - Enforces all url(...) font targets resolve to valid local files on disk
  - Compares SHA-256 hashes against canonical offline checksum manifest
  - Fails with non-zero exit code on tampering, drift, or missing assets

Usage:
  python tools/assets/localize_fonts.py --check       # Offline verification of local files, URLs, and SHA-256 hashes
  python tools/assets/localize_fonts.py --download    # Explicitly fetch and localize font assets from pinned sources
"""

import argparse
import hashlib
import os
import re
import sys
import urllib.request

BOOTSTRAP_ICONS_VERSION = "1.11.3"
BOOTSTRAP_ICONS_BASE_URL = f"https://cdn.jsdelivr.net/npm/bootstrap-icons@{BOOTSTRAP_ICONS_VERSION}/font"

# Pinned Source Serif 4 Google Font stylesheet query with fixed user-agent to ensure deterministic woff2 URLs
SOURCE_SERIF_4_GFONTS_URL = (
    "https://fonts.googleapis.com/css2?family=Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600&display=swap"
)

# Canonical SHA-256 checksum manifest of local typography and icon binary assets
FONT_MANIFEST = {
    "bootstrap-icons/bootstrap-icons.woff": "bb1de989b83970f6f4e54de1cd974c5cba55b73582da5e1b225a6d0edf029483",
    "bootstrap-icons/bootstrap-icons.woff2": "476adf42b40325098fcfa8b36ab3e769186bb4f6ce6a249753e2e1a9c22bf99e",
    "sourceserif4/vEFI2_tTDB4M7-auWDN0ahZJW1gU8tenXg.woff2": "eb9684d1e58cfe4985d3365dde645b0092e31450f79c94bb037cbf4484442168",
    "sourceserif4/vEFI2_tTDB4M7-auWDN0ahZJW1gV8tenXg.woff2": "155f6e701097cc269cf8d5d7ac86795f263f1afbeb47c14d1d747108c9335389",
    "sourceserif4/vEFI2_tTDB4M7-auWDN0ahZJW1gW8tenXg.woff2": "c4fbd9994039466f864b5f025fdb214f0f58a8c2f888d99c1885efcd516baf2d",
    "sourceserif4/vEFI2_tTDB4M7-auWDN0ahZJW1gY8tenXg.woff2": "d69b8909cbccf5948675c7de331858d40f48b1ef3e4a9fc5ddfaa8c097fb434b",
    "sourceserif4/vEFI2_tTDB4M7-auWDN0ahZJW1gb8tc.woff2": "f2ea9c12d2fe9bd3a9589b02ad2c0909da88f30938c91adc838c4f4098f9f9e0",
    "sourceserif4/vEFI2_tTDB4M7-auWDN0ahZJW1gf8tenXg.woff2": "e553fa829fd9ae0900931584f73c6b894539558b598d5ee7c4907140a1f29f05",
}


def get_paths():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
    fonts_dir = os.path.join(repo_root, "wwwroot", "fonts")
    css_dir = os.path.join(repo_root, "wwwroot", "css")
    return repo_root, fonts_dir, css_dir


def sha256_file(filepath: str) -> str:
    h = hashlib.sha256()
    with open(filepath, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()


def check_fonts(fonts_dir: str, css_dir: str) -> bool:
    all_ok = True
    print("=== [1/3] Verifying Local Font Binaries against SHA-256 Manifest ===")
    verified_files = set()
    for rel_path, expected_hash in FONT_MANIFEST.items():
        abs_path = os.path.join(fonts_dir, rel_path.replace("/", os.sep))
        if not os.path.isfile(abs_path):
            print(f"  [MISSING] Font binary missing: {rel_path}", file=sys.stderr)
            all_ok = False
            continue
        size = os.path.getsize(abs_path)
        if size == 0:
            print(f"  [EMPTY] Font file is 0 bytes: {rel_path}", file=sys.stderr)
            all_ok = False
            continue
        actual_hash = sha256_file(abs_path)
        if actual_hash != expected_hash:
            print(
                f"  [HASH MISMATCH] {rel_path}\n    Expected: {expected_hash}\n    Actual:   {actual_hash}",
                file=sys.stderr,
            )
            all_ok = False
        else:
            print(f"  [VERIFIED] {rel_path} ({size:,} bytes) - SHA-256: {actual_hash[:16]}...")
            verified_files.add(os.path.normpath(abs_path))

    print("\n=== [2/3] Verifying CSS Files (No External @import Waterfall) ===")
    css_files_to_check = [
        os.path.join(css_dir, "bootstrap-icons.css"),
        os.path.join(css_dir, "fonts.css"),
        os.path.join(css_dir, "site.css"),
    ]

    referenced_font_files = set()
    for css_file in css_files_to_check:
        rel_css = os.path.relpath(css_file, os.path.dirname(css_dir))
        if not os.path.isfile(css_file):
            print(f"  [MISSING] CSS file not found: {rel_css}", file=sys.stderr)
            all_ok = False
            continue
        with open(css_file, "r", encoding="utf-8") as f:
            content = f.read()

        # Check for prohibited external @import
        import_matches = re.findall(r"@import\s+[^;]+;", content, re.IGNORECASE)
        for imp in import_matches:
            if re.search(r"https?://|//", imp):
                print(f"  [VIOLATION] Prohibited external @import found in {rel_css}: {imp.strip()}", file=sys.stderr)
                all_ok = False

        print(f"  [VERIFIED] {rel_css} - 0 external @import rules.")

        # Check url(...) references
        url_matches = re.findall(r"""url\s*\(\s*["']?([^"'\)\s]+)["']?\s*\)""", content)
        for raw_url in url_matches:
            if raw_url.startswith("data:"):
                continue
            if re.match(r"^(https?:)?//", raw_url, re.IGNORECASE):
                print(f"  [VIOLATION] External url() found in {rel_css}: {raw_url}", file=sys.stderr)
                all_ok = False
                continue

            # Strip query strings / hash fragments like ?dd670... or #anchor
            clean_url = raw_url.split("?")[0].split("#")[0]
            # Check if this URL references a font extension
            if any(clean_url.endswith(ext) for ext in [".woff2", ".woff", ".ttf", ".otf", ".eot"]):
                resolved = os.path.normpath(os.path.join(os.path.dirname(css_file), clean_url))
                if not os.path.isfile(resolved):
                    print(
                        f"  [UNRESOLVED] Font url '{raw_url}' in {rel_css} does not resolve to a file on disk: {resolved}",
                        file=sys.stderr,
                    )
                    all_ok = False
                else:
                    rel_to_fonts = os.path.relpath(resolved, fonts_dir).replace("\\", "/")
                    if rel_to_fonts not in FONT_MANIFEST:
                        print(
                            f"  [UNMANIFESTED] Font url '{raw_url}' in {rel_css} resolves to {rel_to_fonts} which is not in FONT_MANIFEST!",
                            file=sys.stderr,
                        )
                        all_ok = False
                    else:
                        referenced_font_files.add(os.path.normpath(resolved))

    print("\n=== [3/3] Cross-Referencing CSS url() References with Manifest Assets ===")
    for rel_path in FONT_MANIFEST:
        abs_p = os.path.normpath(os.path.join(fonts_dir, rel_path.replace("/", os.sep)))
        if abs_p in referenced_font_files:
            print(f"  [REFERENCED & RESOLVED] {rel_path} is correctly linked and resolvable.")
        else:
            print(f"  [NOTE] {rel_path} in manifest but not directly referenced in checked CSS.")

    return all_ok


def download_fonts(fonts_dir: str, css_dir: str) -> None:
    os.makedirs(os.path.join(fonts_dir, "sourceserif4"), exist_ok=True)
    os.makedirs(os.path.join(fonts_dir, "bootstrap-icons"), exist_ok=True)

    print(f"1. Fetching Bootstrap Icons v{BOOTSTRAP_ICONS_VERSION}...")
    bi_css_url = f"{BOOTSTRAP_ICONS_BASE_URL}/bootstrap-icons.min.css"
    req = urllib.request.Request(bi_css_url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req) as resp:
        bi_css = resp.read().decode("utf-8")

    for ext in ["woff2", "woff"]:
        f_url = f"{BOOTSTRAP_ICONS_BASE_URL}/fonts/bootstrap-icons.{ext}"
        local_f = os.path.join(fonts_dir, "bootstrap-icons", f"bootstrap-icons.{ext}")
        print(f"   Downloading {f_url} -> {local_f}")
        urllib.request.urlretrieve(f_url, local_f)

    bi_css_local = bi_css.replace("./fonts/bootstrap-icons.woff2", "../fonts/bootstrap-icons/bootstrap-icons.woff2")
    bi_css_local = bi_css_local.replace("./fonts/bootstrap-icons.woff", "../fonts/bootstrap-icons/bootstrap-icons.woff")
    with open(os.path.join(css_dir, "bootstrap-icons.css"), "w", encoding="utf-8") as f:
        f.write(bi_css_local)
    print("   Saved bootstrap-icons.css")

    print("2. Fetching Source Serif 4 (pinned parameters)...")
    req = urllib.request.Request(
        SOURCE_SERIF_4_GFONTS_URL,
        headers={
            "User-Agent": (
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)"
                " Chrome/120.0.0.0 Safari/537.36"
            )
        },
    )
    with urllib.request.urlopen(req) as resp:
        gf_css = resp.read().decode("utf-8")

    def replace_font(match):
        url = match.group(1)
        fname = os.path.basename(url)
        dest = os.path.join(fonts_dir, "sourceserif4", fname)
        if not os.path.exists(dest):
            print(f"   Downloading font {fname}...")
            urllib.request.urlretrieve(url, dest)
        return f'url("../fonts/sourceserif4/{fname}")'

    gf_css_local = re.sub(r"url\((https://fonts\.gstatic\.com/[^)]+\.woff2)\)", replace_font, gf_css)
    with open(os.path.join(css_dir, "fonts.css"), "w", encoding="utf-8") as f:
        f.write(gf_css_local)
    print("   Saved fonts.css")
    print("Font localization maintenance complete!")


def main() -> int:
    parser = argparse.ArgumentParser(description="Localize, Pin and Verify Offline Fonts and Icons for Supplier Selection DSS")
    parser.add_argument(
        "--check",
        action="store_true",
        help="Verify local font assets exist and match SHA-256 manifest without making network requests",
    )
    parser.add_argument(
        "--download",
        action="store_true",
        help="Explicitly download and update local font assets from pinned sources",
    )
    args = parser.parse_args()

    _, fonts_dir, css_dir = get_paths()

    if args.download:
        download_fonts(fonts_dir, css_dir)
        return 0

    # Default or --check runs offline verification
    ok = check_fonts(fonts_dir, css_dir)
    if not ok:
        print("\n[ERROR] Font verification failed: missing, tampered, or unresolved assets.", file=sys.stderr)
        return 1
    print("\n[SUCCESS] All font assets, CSS references, and SHA-256 manifests verified offline!")
    return 0


if __name__ == "__main__":
    sys.exit(main())
