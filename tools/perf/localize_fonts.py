import os
import urllib.request
import re

def main():
    root_dir = r"D:\DEAN\Projects\SupplierSelectionDSS"
    fonts_dir = os.path.join(root_dir, "wwwroot", "fonts")
    css_dir = os.path.join(root_dir, "wwwroot", "css")
    
    os.makedirs(os.path.join(fonts_dir, "sourceserif4"), exist_ok=True)
    os.makedirs(os.path.join(fonts_dir, "bootstrap-icons"), exist_ok=True)

    print("1. Fetching Bootstrap Icons...")
    bi_css_url = "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"
    req = urllib.request.Request(bi_css_url, headers={"User-Agent": "Mozilla/5.0"})
    bi_css = urllib.request.urlopen(req).read().decode("utf-8")

    for ext in ["woff2", "woff"]:
        f_url = f"https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/fonts/bootstrap-icons.{ext}"
        local_f = os.path.join(fonts_dir, "bootstrap-icons", f"bootstrap-icons.{ext}")
        print(f"Downloading {f_url} -> {local_f}")
        urllib.request.urlretrieve(f_url, local_f)

    bi_css_local = bi_css.replace("./fonts/bootstrap-icons.woff2", "../fonts/bootstrap-icons/bootstrap-icons.woff2")
    bi_css_local = bi_css_local.replace("./fonts/bootstrap-icons.woff", "../fonts/bootstrap-icons/bootstrap-icons.woff")
    with open(os.path.join(css_dir, "bootstrap-icons.css"), "w", encoding="utf-8") as f:
        f.write(bi_css_local)
    print("Saved bootstrap-icons.css")

    print("2. Fetching Source Serif 4...")
    gf_url = "https://fonts.googleapis.com/css2?family=Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600&display=swap"
    req = urllib.request.Request(gf_url, headers={
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    })
    gf_css = urllib.request.urlopen(req).read().decode("utf-8")

    def replace_font(match):
        url = match.group(1)
        fname = os.path.basename(url)
        dest = os.path.join(fonts_dir, "sourceserif4", fname)
        if not os.path.exists(dest):
            print(f"Downloading font {fname}...")
            urllib.request.urlretrieve(url, dest)
        return f'url("../fonts/sourceserif4/{fname}")'

    gf_css_local = re.sub(r'url\((https://fonts\.gstatic\.com/[^)]+\.woff2)\)', replace_font, gf_css)
    with open(os.path.join(css_dir, "fonts.css"), "w", encoding="utf-8") as f:
        f.write(gf_css_local)
    print("Saved fonts.css")
    print("Font localization complete!")

if __name__ == "__main__":
    main()
