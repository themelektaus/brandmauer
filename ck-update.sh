#!/bin/bash

cd "$(dirname "$0")"

bash ck-disconnect-if-reconnecting.sh
bash ck-connect.sh
