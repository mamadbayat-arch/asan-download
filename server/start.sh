#!/bin/sh
set -eu
cd /workspace/asan-download/server
export NODE_ENV=production
export HOST=0.0.0.0
export PORT=4080
export PUBLIC_ORIGIN=https://downloader.hamtavar.ir
export DATA_DIR=/workspace/asan-download/data
exec node server.mjs
