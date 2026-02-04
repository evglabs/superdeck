#!/bin/bash
# Run the SuperDeck web client dev server
# Proxies API requests to the .NET server at localhost:5000
cd "$(dirname "$0")/src/WebClient"
npm run dev
