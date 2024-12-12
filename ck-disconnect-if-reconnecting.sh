#!/bin/bash

cd "$(dirname "$0")"

expect ck-disconnect-if-reconnecting.exp
