"""Experience replay buffer for PPO training."""

import torch


class RolloutBuffer:
    """Stores trajectories collected during environment interaction."""

    def __init__(self):
        self.observations: list[torch.Tensor] = []
        self.actions: list[torch.Tensor] = []
        self.log_probs: list[torch.Tensor] = []
        self.rewards: list[float] = []
        self.values: list[torch.Tensor] = []
        self.dones: list[bool] = []
        self.advantages: list = []
        self.returns: list = []

    def add(self, obs, action, log_prob, reward, value, done):
        self.observations.append(obs)
        self.actions.append(action)
        self.log_probs.append(log_prob)
        self.rewards.append(reward)
        self.values.append(value)
        self.dones.append(done)

    def compute_returns_and_advantages(self, last_value: torch.Tensor, gamma: float = 0.99, lam: float = 0.95):
        """Compute GAE advantages and discounted returns."""
        advantages = []
        returns = []
        gae = 0.0

        values = self.values + [last_value]
        for t in reversed(range(len(self.rewards))):
            delta = self.rewards[t] + gamma * values[t + 1] * (1 - self.dones[t]) - values[t]
            gae = delta + gamma * lam * (1 - self.dones[t]) * gae
            advantages.insert(0, gae)
            returns.insert(0, gae + values[t])

        self.advantages = advantages
        self.returns = returns

    def get_batches(self, batch_size: int):
        """Yield random mini-batches for PPO updates."""
        n = len(self.observations)
        indices = torch.randperm(n)

        for start in range(0, n, batch_size):
            end = min(start + batch_size, n)
            batch_idx = indices[start:end]

            yield {
                "observations": torch.stack([self.observations[i] for i in batch_idx]),
                "actions": torch.stack([self.actions[i] for i in batch_idx]),
                "old_log_probs": torch.stack([self.log_probs[i] for i in batch_idx]),
                "advantages": torch.tensor([self.advantages[i] for i in batch_idx], dtype=torch.float32),
                "returns": torch.tensor([self.returns[i] for i in batch_idx], dtype=torch.float32),
            }

    def clear(self):
        self.observations.clear()
        self.actions.clear()
        self.log_probs.clear()
        self.rewards.clear()
        self.values.clear()
        self.dones.clear()
        self.advantages = []
        self.returns = []
