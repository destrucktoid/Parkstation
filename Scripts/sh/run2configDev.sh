#!/bin/env sh

# make sure to start from script dir
if [ "$(dirname $0)" != "." ]; then
  cd "$(dirname $0)"
fi

cp runconfigdev.toml ../../bin/Content.Server/server_config.toml
