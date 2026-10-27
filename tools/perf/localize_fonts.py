#!/usr/bin/env python3
"""
tools/perf/localize_fonts.py

[DEPRECATED] Wrapper for backward compatibility.
This script has moved to tools/assets/localize_fonts.py.
Scheduled for removal in v1.6.0.
"""

import os
import sys
import warnings

def main():
    warnings.warn(
        "tools/perf/localize_fonts.py is deprecated and will be removed in v1.6.0. "
        "Use tools/assets/localize_fonts.py instead.",
        DeprecationWarning,
        stacklevel=2
    )
    script_dir = os.path.dirname(os.path.abspath(__file__))
    assets_localizer = os.path.abspath(os.path.join(script_dir, "..", "assets", "localize_fonts.py"))
    
    if not os.path.isfile(assets_localizer):
        print(f"Error: assets localizer not found at {assets_localizer}", file=sys.stderr)
        sys.exit(1)

    import subprocess
    cmd = [sys.executable, assets_localizer] + sys.argv[1:]
    result = subprocess.run(cmd)
    sys.exit(result.returncode)

if __name__ == "__main__":
    main()
