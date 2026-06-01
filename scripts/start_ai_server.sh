#!/bin/bash
# Start the AI server for Abyss Walker

set -e

echo "=== Starting Abyss Walker AI Server ==="

# Ensure conda environment is active
if [[ "$CONDA_DEFAULT_ENV" != "abyss-walker" ]]; then
    echo "Activating abyss-walker conda environment..."
    conda activate abyss-walker
fi

# Start the server
python -m ai.server.socket_server
