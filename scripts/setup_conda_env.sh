#!/bin/bash
# Setup conda environment for Abyss Walker

set -e

echo "=== Abyss Walker - Environment Setup ==="

# Check if conda is available
if ! command -v conda &> /dev/null; then
    echo "Error: conda not found. Please install Anaconda or Miniconda first."
    exit 1
fi

# Create conda environment
echo "Creating conda environment 'abyss-walker'..."
conda env create -f environment.yml

echo ""
echo "Setup complete! Activate the environment with:"
echo "  conda activate abyss-walker"
