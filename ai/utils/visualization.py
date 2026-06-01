"""Training visualization with TensorBoard and map ASCII visualization."""

import os
from torch.utils.tensorboard import SummaryWriter


class TrainingLogger:
    """Logs training metrics to TensorBoard."""

    def __init__(self, log_dir: str = "runs/ppo_training"):
        os.makedirs(log_dir, exist_ok=True)
        self.writer = SummaryWriter(log_dir)

    def log_episode(self, episode: int, reward: float, avg_reward: float):
        self.writer.add_scalar("Reward/episode", reward, episode)
        self.writer.add_scalar("Reward/avg_50", avg_reward, episode)

    def log_training_stats(self, step: int, stats: dict):
        for key, value in stats.items():
            self.writer.add_scalar(f"Training/{key}", value, step)

    def close(self):
        self.writer.close()


def print_dungeon(dungeon) -> str:
    """Render a dungeon as ASCII art for debugging."""
    tile_chars = {
        0: ".",  # Floor
        1: "#",  # Wall
        2: "+",  # Door
        3: "$",  # Chest
        4: ">",  # Exit
    }

    lines = []
    for y in range(dungeon.height):
        row = []
        for x in range(dungeon.width):
            tile = dungeon.tiles[y][x]

            # Check for player spawn
            if (x, y) == dungeon.spawn_point:
                row.append("@")
            # Check for enemies
            elif any(e["pos"] == [x, y] for e in dungeon.enemies):
                row.append("E")
            # Check for events
            elif any(e["pos"] == [x, y] for e in dungeon.events):
                row.append("?")
            else:
                row.append(tile_chars.get(tile, " "))
        lines.append("".join(row))

    return "\n".join(lines)
