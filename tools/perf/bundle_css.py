import os

def main():
    root_dir = r"D:\DEAN\Projects\SupplierSelectionDSS\wwwroot\css"
    core_files = [
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
        os.path.join("components", "background.css")
    ]

    # Read existing site.css from line 45 onwards (resets and utilities)
    site_path = os.path.join(root_dir, "site.css")
    with open(site_path, "r", encoding="utf-8") as f:
        site_content = f.read()

    # Find where global reset starts
    split_marker = "/* =========================================================\n   GLOBAL RESET"
    if split_marker not in site_content:
        split_marker = "/* =========================================================\r\n   GLOBAL RESET"
    
    parts = site_content.split(split_marker)
    if len(parts) < 2:
        raise ValueError("Could not find GLOBAL RESET marker in site.css")
    
    site_utilities = split_marker + parts[1]

    # Combine all core files
    bundled = [
        "/* =========================================================",
        "   SITE — CORE DESIGN SYSTEM (BUNDLED, ZERO @IMPORT WATERFALL)",
        "   Contains: Design Tokens, Typography, Layout, Responsive,",
        "   Core UI Components, and Global System Utilities.",
        "   Page-specific styles are loaded on demand via @section Styles.",
        "   ========================================================= */\n"
    ]

    for rel in core_files:
        full_p = os.path.join(root_dir, rel)
        bundled.append(f"/* --- BEGIN {rel.replace(os.sep, '/')} --- */")
        with open(full_p, "r", encoding="utf-8") as f:
            bundled.append(f.read().strip())
        bundled.append(f"/* --- END {rel.replace(os.sep, '/')} --- */\n")

    bundled.append("/* --- BEGIN site utilities & resets --- */")
    bundled.append(site_utilities.strip())
    bundled.append("/* --- END site utilities & resets --- */\n")

    output = "\n".join(bundled)
    with open(site_path, "w", encoding="utf-8") as f:
        f.write(output)

    print(f"Bundled site.css written: {len(output):,} characters ({len(output.encode('utf-8'))/1024:.1f} KB)")

if __name__ == "__main__":
    main()
