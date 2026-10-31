#!/usr/bin/env bash
# Adjusts a published Blazor WASM wwwroot folder for hosting on GitHub Pages under /<repo>/.
# Usage: build/prepare-pages.sh <wwwroot-dir> <repo-name>
set -euo pipefail
dir="$1"
repo="$2"

# 1. Base href so the app works from https://<user>.github.io/<repo>/
sed -i "s|<base href=\"/\" />|<base href=\"/${repo}/\" />|" "$dir/index.html"

# 2. Deep links: GitHub Pages serves 404.html for unknown paths
cp "$dir/index.html" "$dir/404.html"

# 3. Stop Jekyll ignoring the _framework folder
touch "$dir/.nojekyll"

# 4. index.html changed, so update its integrity hash in the service worker manifest
#    (otherwise offline install fails) and drop stale pre-compressed copies.
rm -f "$dir/index.html.br" "$dir/index.html.gz"
hash="sha256-$(openssl dgst -sha256 -binary "$dir/index.html" | openssl base64 -A)"
python3 - "$dir/service-worker-assets.js" "$hash" <<'PY'
import re, sys
path, new_hash = sys.argv[1], sys.argv[2]
text = open(path, encoding="utf-8").read()
pattern = r'("hash":\s*")[^"]+("[^{}]*"url":\s*"index\.html")'
text, n = re.subn(pattern, lambda m: m.group(1) + new_hash + m.group(2), text)
if n != 1:
    sys.exit(f"Expected one index.html entry in {path}, found {n}")
open(path, "w", encoding="utf-8").write(text)
PY
echo "Prepared $dir for GitHub Pages at /${repo}/"
