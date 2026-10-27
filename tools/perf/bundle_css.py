#!/usr/bin/env python3
"""
tools/perf/bundle_css.py

[DEPRECATED] Wrapper for backward compatibility.
This script has moved to tools/assets/bundle_css.py.
Scheduled for removal in v1.6.0. Please update invocations to use tools/assets/bundle_css.py.
"""

import os
import sys
import warnings

def main():
    warnings.warn(
        "tools/perf/bundle_css.py is deprecated and will be removed in v1.6.0. "
        "Use tools/assets/bundle_css.py instead.",
        DeprecationWarning,
        stacklevel=2
    )
    script_dir = os.path.dirname(os.path.abspath(__file__))
    assets_bundler = os.path.abspath(os.path.join(script_dir, "..", "assets", "bundle_css.py"))
    
    if not os.path.isfile(assets_bundler):
        print(f"Error: assets bundler not found at {assets_bundler}", file=sys.stderr)
        sys.exit(1)

    import subprocess
    cmd = [sys.executable, assets_bundler] + sys.argv[1:]
    result = subprocess.run(cmd)
    sys.exit(result.returncode)

if __name__ == "__main__":
    main()
