"""Training visualization with TensorBoard."""

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
