#!/usr/bin/env python3
"""
tools/assets/localize_fonts.py

Explicit maintenance utility to localize and pin external typography and icon assets:
  1. Bootstrap Icons v1.11.3 (MIT License) -> wwwroot/fonts/bootstrap-icons/
  2. Source Serif 4 (SIL Open Font License 1.1) -> wwwroot/fonts/sourceserif4/

Provenance & Licensing:
  - Bootstrap Icons: https://github.com/twbs/icons (MIT License)
  - Source Serif 4: https://github.com/adobe-fonts/source-serif (SIL Open Font License 1.1)

Usage:
  python tools/assets/localize_fonts.py --check       # Verify local font files exist and are non-empty (offline safe)
  python tools/assets/localize_fonts.py --download    # Explicitly fetch and localize font assets (requires network)
"""

import argparse
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


def get_paths():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
    fonts_dir = os.path.join(repo_root, "wwwroot", "fonts")
    css_dir = os.path.join(repo_root, "wwwroot", "css")
    return repo_root, fonts_dir, css_dir


def check_fonts(fonts_dir: str, css_dir: str) -> bool:
    all_ok = True

    # Check bootstrap-icons css and fonts
    bi_css = os.path.join(css_dir, "bootstrap-icons.css")
    if not os.path.isfile(bi_css) or os.path.getsize(bi_css) == 0:
        print(f"[MISSING] {bi_css}", file=sys.stderr)
        all_ok = False
    else:
        print(f"[OK] {bi_css} ({os.path.getsize(bi_css):,} bytes)")

    for ext in ["woff2", "woff"]:
        bi_font = os.path.join(fonts_dir, "bootstrap-icons", f"bootstrap-icons.{ext}")
        if not os.path.isfile(bi_font) or os.path.getsize(bi_font) == 0:
            print(f"[MISSING] {bi_font}", file=sys.stderr)
            all_ok = False
        else:
            print(f"[OK] {bi_font} ({os.path.getsize(bi_font):,} bytes)")

    # Check sourceserif4 css and fonts
    fonts_css = os.path.join(css_dir, "fonts.css")
    if not os.path.isfile(fonts_css) or os.path.getsize(fonts_css) == 0:
        print(f"[MISSING] {fonts_css}", file=sys.stderr)
        all_ok = False
    else:
        print(f"[OK] {fonts_css} ({os.path.getsize(fonts_css):,} bytes)")

    ss4_dir = os.path.join(fonts_dir, "sourceserif4")
    if not os.path.isdir(ss4_dir):
        print(f"[MISSING] {ss4_dir}", file=sys.stderr)
        all_ok = False
    else:
        ss4_files = [f for f in os.listdir(ss4_dir) if f.endswith(".woff2")]
        if not ss4_files:
            print(f"[MISSING] No woff2 files in {ss4_dir}", file=sys.stderr)
            all_ok = False
        else:
            print(f"[OK] {ss4_dir} contains {len(ss4_files)} woff2 font files.")

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
        headers={"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"},
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

    gf_css_local = re.sub(r'url\((https://fonts\.gstatic\.com/[^)]+\.woff2)\)', replace_font, gf_css)
    with open(os.path.join(css_dir, "fonts.css"), "w", encoding="utf-8") as f:
        f.write(gf_css_local)
    print("   Saved fonts.css")
    print("Font localization maintenance complete!")


def main() -> int:
    parser = argparse.ArgumentParser(description="Localize and Pin Fonts and Icons for Supplier Selection DSS")
    parser.add_argument("--check", action="store_true", help="Verify local font assets exist without making network requests")
    parser.add_argument("--download", action="store_true", help="Explicitly download and update local font assets from pinned sources")
    args = parser.parse_args()

    _, fonts_dir, css_dir = get_paths()

    if args.download:
        download_fonts(fonts_dir, css_dir)
        return 0

    # Default or --check runs offline verification
    ok = check_fonts(fonts_dir, css_dir)
    if not ok:
        print("\nSome font assets are missing. Run with --download to fetch them.", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
