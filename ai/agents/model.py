"""Neural network models for the RL Agent."""

import torch
import torch.nn as nn


class ActorCritic(nn.Module):
    """Actor-Critic network for PPO.

    Input: flattened game state observation (~127 dims)
    Output: action probabilities (actor) + state value (critic)
    """

    def __init__(self, obs_dim: int, action_dim: int, hidden_dim: int = 256):
        super().__init__()

        # Shared feature extractor
        self.shared = nn.Sequential(
            nn.Linear(obs_dim, hidden_dim),
            nn.ReLU(),
            nn.Linear(hidden_dim, hidden_dim),
            nn.ReLU(),
        )

        # Actor head (policy)
        self.actor = nn.Sequential(
            nn.Linear(hidden_dim, 128),
            nn.ReLU(),
            nn.Linear(128, action_dim),
            nn.Softmax(dim=-1),
        )

        # Critic head (value)
        self.critic = nn.Sequential(
            nn.Linear(hidden_dim, 128),
            nn.ReLU(),
            nn.Linear(128, 1),
        )

    def forward(self, obs: torch.Tensor):
        features = self.shared(obs)
        action_probs = self.actor(features)
        state_value = self.critic(features)
        return action_probs, state_value

    def get_action(self, obs: torch.Tensor):
        action_probs, value = self.forward(obs)
        dist = torch.distributions.Categorical(action_probs)
        action = dist.sample()
        return action, dist.log_prob(action), value

    def evaluate(self, obs: torch.Tensor, actions: torch.Tensor):
        action_probs, values = self.forward(obs)
        dist = torch.distributions.Categorical(action_probs)
        log_probs = dist.log_prob(actions)
        entropy = dist.entropy()
        return log_probs, values.squeeze(-1), entropy
