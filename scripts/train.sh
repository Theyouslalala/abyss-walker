#!/bin/bash
# Start training the RL Agent

set -e

echo "=== Abyss Walker - Agent Training ==="

# Ensure conda environment is active
if [[ "$CONDA_DEFAULT_ENV" != "abyss-walker" ]]; then
    echo "Activating abyss-walker conda environment..."
    conda activate abyss-walker
fi

# Start training
python -m ai.training.train_agent "$@"
