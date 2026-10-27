#!/usr/bin/env python3
"""
tools/assets/bundle_css.py

Bundles core design system CSS files into wwwroot/css/site.css deterministically.
Eliminates @import waterfalls while keeping modular development source files.

Inputs:
  - tokens.css
  - base/typography.css
  - base/layout.css
  - base/responsive.css
  - components/buttons.css
  - components/cards.css
  - components/status.css
  - components/forms.css
  - components/tables.css
  - components/pagination.css
  - components/shared.css
  - components/background.css
  - base/utilities.css

Output:
  - wwwroot/css/site.css (auto-generated, deterministic, LF line endings, UTF-8)

CLI Options:
  --check    Verify that current site.css matches bundled sources without modifying it.
             Exits with 0 if in sync, 1 if drift detected.
  --output   Specify custom output file path (default: wwwroot/css/site.css).
"""

import argparse
import hashlib
import os
import sys

HEADER = """/* =========================================================
   SITE — CORE DESIGN SYSTEM (BUNDLED, ZERO @IMPORT WATERFALL)
   AUTO-GENERATED FILE — DO NOT EDIT DIRECTLY.
   Source files located in wwwroot/css/ (tokens.css, base/, components/).
   Regenerate using: python tools/assets/bundle_css.py
   ========================================================= */
"""

CORE_SOURCES = [
    "tokens.css",
    os.path.join("base", "typography.css"),
    os.path.join("base", "layout.css"),
    os.path.join("base", "responsive.css"),
    os.path.join("components", "buttons.css"),
    os.path.join("components", "cards.css"),
    os.path.join("components", "status.css"),
    os.path.join("components", "forms.css"),
    os.path.join("components", "tables.css"),
    os.path.join("components", "pagination.css"),
    os.path.join("components", "shared.css"),
    os.path.join("components", "background.css"),
    os.path.join("base", "utilities.css"),
]


def bundle_css(css_root: str) -> str:
    sections = [HEADER.strip()]

    for rel_path in CORE_SOURCES:
        full_path = os.path.join(css_root, rel_path)
        if not os.path.isfile(full_path):
            raise FileNotFoundError(f"Required CSS source file not found: {full_path}")

        normalized_rel = rel_path.replace(os.sep, "/")
        with open(full_path, "r", encoding="utf-8") as f:
            content = f.read().strip()

        sections.append(f"/* --- BEGIN {normalized_rel} --- */")
        sections.append(content)
        sections.append(f"/* --- END {normalized_rel} --- */\n")

    return "\n".join(sections).strip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Deterministic CSS Bundler for Supplier Selection DSS")
    parser.add_argument("--check", action="store_true", help="Check if bundled CSS is up-to-date without writing")
    parser.add_argument("--output", type=str, default=None, help="Custom output path for bundled CSS")
    parser.add_argument("--css-dir", type=str, default=None, help="Custom CSS source root directory")
    args = parser.parse_args()

    script_dir = os.path.dirname(os.path.abspath(__file__))
    repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))

    css_dir = args.css_dir or os.path.join(repo_root, "wwwroot", "css")
    if not os.path.isdir(css_dir):
        print(f"Error: CSS directory not found: {css_dir}", file=sys.stderr)
        return 1

    target_file = args.output or os.path.join(css_dir, "site.css")

    try:
        bundled = bundle_css(css_dir)
    except Exception as ex:
        print(f"Error bundling CSS: {ex}", file=sys.stderr)
        return 1

    bundled_bytes = bundled.encode("utf-8")
    bundled_hash = hashlib.sha256(bundled_bytes).hexdigest()

    if args.check:
        if not os.path.isfile(target_file):
            print(f"Check failed: Target file does not exist: {target_file}", file=sys.stderr)
            return 1

        with open(target_file, "rb") as f:
            current_bytes = f.read()

        current_hash = hashlib.sha256(current_bytes).hexdigest()
        if bundled_hash != current_hash:
            print("Check failed: wwwroot/css/site.css is out of sync with source CSS files.", file=sys.stderr)
            print(f"  Current SHA-256:  {current_hash}", file=sys.stderr)
            print(f"  Expected SHA-256: {bundled_hash}", file=sys.stderr)
            print("Run 'python tools/assets/bundle_css.py' to synchronize.", file=sys.stderr)
            return 1

        print(f"Check passed: site.css matches sources perfectly (SHA-256: {bundled_hash[:16]}...).")
        return 0

    # Write output with LF line endings and UTF-8
    with open(target_file, "wb") as f:
        f.write(bundled_bytes)

    print(f"Bundled site.css written: {len(bundled):,} chars ({len(bundled_bytes) / 1024:.1f} KB)")
    print(f"SHA-256: {bundled_hash}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
