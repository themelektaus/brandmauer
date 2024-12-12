#!/bin/bash

cd "$(dirname "$0")"

bash ck-disconnect.sh
bash ck-connect.sh
