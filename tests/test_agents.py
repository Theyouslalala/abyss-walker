"""Tests for RL Agent components."""

import pytest
import torch
from ai.agents.model import ActorCritic
from ai.agents.rl_agent import PPOAgent
from ai.agents.replay_buffer import RolloutBuffer
from ai.training.env_wrapper import AbyssWalkerEnv


def test_actor_critic_forward():
    model = ActorCritic(obs_dim=127, action_dim=6)
    obs = torch.randn(1, 127)
    action_probs, value = model(obs)
    assert action_probs.shape == (1, 6)
    assert value.shape == (1, 1)
    assert torch.allclose(action_probs.sum(), torch.tensor(1.0), atol=1e-5)


def test_actor_critic_get_action():
    model = ActorCritic(obs_dim=127, action_dim=6)
    obs = torch.randn(127)
    action, log_prob, value = model.get_action(obs.unsqueeze(0))
    assert 0 <= action.item() < 6
    assert log_prob.dim() == 1
    assert value.dim() == 2


def test_ppo_agent_select_action():
    agent = PPOAgent(obs_dim=127, action_dim=6, device="cpu")
    obs = torch.randn(127)
    action, log_prob, value = agent.select_action(obs)
    assert 0 <= action < 6


def test_rollout_buffer():
    buffer = RolloutBuffer()
    obs = torch.randn(127)
    buffer.add(obs, torch.tensor(0), torch.tensor(-0.5), 1.0, torch.tensor(0.5), False)
    buffer.add(obs, torch.tensor(1), torch.tensor(-0.3), 2.0, torch.tensor(0.6), True)
    assert len(buffer.observations) == 2


def test_env_reset():
    env = AbyssWalkerEnv(grid_size=10)
    obs, info = env.reset(seed=42)
    assert obs.shape == (127,)  # 7 + 100 + 20 (player + grid + enemies)
    assert env.player_hp == 100


def test_env_step():
    env = AbyssWalkerEnv(grid_size=10)
    env.reset(seed=42)
    obs, reward, terminated, truncated, info = env.step(0)  # Move up
    assert obs.shape == (127,)
    assert isinstance(reward, float)


def test_env_episode_ends():
    env = AbyssWalkerEnv(grid_size=10, max_steps=10)
    env.reset(seed=42)
    for _ in range(15):
        obs, reward, terminated, truncated, info = env.step(4)  # Stay still
        if terminated or truncated:
            break
    assert terminated or truncated
