"""Training script for the PPO Agent."""

import argparse
import logging
import os

import torch

from ai.agents.rl_agent import PPOAgent
from ai.training.env_wrapper import AbyssWalkerEnv
from ai.utils.visualization import TrainingLogger

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger(__name__)


def train(
    num_episodes: int = 1000,
    update_every: int = 2048,
    grid_size: int = 10,
    hidden_dim: int = 256,
    lr: float = 3e-4,
    save_every: int = 100,
    save_path: str = "checkpoints/ppo_abyss_walker.pth",
    log_dir: str = "runs/ppo_training",
):
    env = AbyssWalkerEnv(grid_size=grid_size)
    obs_dim = env.observation_space.shape[0]
    action_dim = env.action_space.n

    agent = PPOAgent(
        obs_dim=obs_dim,
        action_dim=action_dim,
        hidden_dim=hidden_dim,
        lr=lr,
    )
    training_logger = TrainingLogger(log_dir)

    logger.info(f"Training PPO Agent: obs_dim={obs_dim}, action_dim={action_dim}")
    logger.info(f"Episodes: {num_episodes}, update_every: {update_every}")

    total_steps = 0
    episode_rewards = []

    for episode in range(num_episodes):
        obs, _ = env.reset()
        obs = torch.tensor(obs, dtype=torch.float32)
        episode_reward = 0.0
        done = False

        while not done:
            action, log_prob, value = agent.select_action(obs)

            next_obs, reward, terminated, truncated, _ = env.step(action)
            done = terminated or truncated

            next_obs_tensor = torch.tensor(next_obs, dtype=torch.float32)
            agent.buffer.add(obs, torch.tensor(action), torch.tensor(log_prob), reward, torch.tensor(value), done)

            obs = next_obs_tensor
            episode_reward += reward
            total_steps += 1

            # PPO update
            if total_steps % update_every == 0 and len(agent.buffer.observations) > 0:
                with torch.no_grad():
                    _, _, last_value = agent.select_action(obs)
                agent.buffer.compute_returns_and_advantages(torch.tensor(last_value), agent.gamma, agent.lam)
                stats = agent.update()
                logger.info(f"Step {total_steps}: policy_loss={stats['policy_loss']:.4f}, value_loss={stats['value_loss']:.4f}, entropy={stats['entropy']:.4f}")

        episode_rewards.append(episode_reward)
        avg_reward = sum(episode_rewards[-50:]) / len(episode_rewards[-50:])

        training_logger.log_episode(episode, episode_reward, avg_reward)

        if episode % 50 == 0:
            logger.info(f"Episode {episode}/{num_episodes}: reward={episode_reward:.1f}, avg_50={avg_reward:.1f}")

        if (episode + 1) % save_every == 0:
            os.makedirs(os.path.dirname(save_path), exist_ok=True)
            agent.save(save_path)
            logger.info(f"Checkpoint saved: {save_path}")

    logger.info("Training complete!")
    training_logger.close()
    env.close()


def main():
    parser = argparse.ArgumentParser(description="Train PPO Agent for Abyss Walker")
    parser.add_argument("--episodes", type=int, default=1000)
    parser.add_argument("--update-every", type=int, default=2048)
    parser.add_argument("--grid-size", type=int, default=10)
    parser.add_argument("--lr", type=float, default=3e-4)
    parser.add_argument("--save-every", type=int, default=100)
    parser.add_argument("--save-path", type=str, default="checkpoints/ppo_abyss_walker.pth")
    args = parser.parse_args()

    train(
        num_episodes=args.episodes,
        update_every=args.update_every,
        grid_size=args.grid_size,
        lr=args.lr,
        save_every=args.save_every,
        save_path=args.save_path,
    )


if __name__ == "__main__":
    main()
