"""Gymnasium environment wrapper for the Abyss Walker game."""

import gymnasium as gym
import numpy as np
from gymnasium import spaces


class AbyssWalkerEnv(gym.Env):
    """Gymnasium-compatible environment for training the RL Agent.

    This is a standalone environment that simulates the game state
    without requiring the Unity client. Used for training.
    """

    metadata = {"render_modes": ["human"]}

    def __init__(self, grid_size: int = 10, max_enemies: int = 5, max_steps: int = 200):
        super().__init__()
        self.grid_size = grid_size
        self.max_enemies = max_enemies
        self.max_steps = max_steps

        # Observation: player (7) + grid (grid_size*grid_size) + enemies (max_enemies * 4)
        obs_dim = 7 + grid_size * grid_size + max_enemies * 4
        self.observation_space = spaces.Box(low=-1, high=100, shape=(obs_dim,), dtype=np.float32)

        # Actions: 5 moves + attack
        self.action_space = spaces.Discrete(6)  # up/down/left/right/still/attack

        self.step_count = 0
        self.player_pos = [0, 0]
        self.exit_pos = [0, 0]
        self.enemies = []
        self.grid = None

    def reset(self, seed=None, options=None):
        super().reset(seed=seed)
        self.step_count = 0

        # Initialize grid (simplified)
        self.grid = np.zeros((self.grid_size, self.grid_size), dtype=np.int32)

        # Place player and exit
        self.player_pos = [1, 1]
        self.exit_pos = [self.grid_size - 2, self.grid_size - 2]

        # Place enemies
        self.enemies = []
        for i in range(3):
            ex = self.np_random.integers(2, self.grid_size - 1)
            ey = self.np_random.integers(2, self.grid_size - 1)
            self.enemies.append({"id": i, "hp": 30, "max_hp": 30, "pos": [ex, ey], "attack": 10})

        self.player_hp = 100
        self.player_max_hp = 100

        obs = self._get_obs()
        return obs, {}

    def step(self, action):
        self.step_count += 1
        reward = 0.0
        terminated = False
        truncated = False

        # Player movement
        moves = {0: (0, -1), 1: (0, 1), 2: (-1, 0), 3: (1, 0), 4: (0, 0), 5: (0, 0)}
        dx, dy = moves.get(action, (0, 0))
        new_x = max(0, min(self.grid_size - 1, self.player_pos[0] + dx))
        new_y = max(0, min(self.grid_size - 1, self.player_pos[1] + dy))
        self.player_pos = [new_x, new_y]

        # Auto-attack nearby enemies
        enemies_to_remove = []
        for i, enemy in enumerate(self.enemies):
            ex, ey = enemy["pos"]
            dist = abs(self.player_pos[0] - ex) + abs(self.player_pos[1] - ey)
            if dist <= 1:
                enemy["hp"] -= 15  # Player attack damage
                if enemy["hp"] <= 0:
                    enemies_to_remove.append(i)
                    reward += 10.0

        for i in reversed(enemies_to_remove):
            self.enemies.pop(i)

        # Enemy actions
        for enemy in self.enemies:
            ex, ey = enemy["pos"]
            dist = abs(self.player_pos[0] - ex) + abs(self.player_pos[1] - ey)
            if dist <= 1:
                self.player_hp -= enemy["attack"]
                reward -= 5.0 * (enemy["attack"] / self.player_max_hp)
            else:
                # Move toward player
                dx = 1 if self.player_pos[0] > ex else (-1 if self.player_pos[0] < ex else 0)
                dy = 1 if self.player_pos[1] > ey else (-1 if self.player_pos[1] < ey else 0)
                enemy["pos"] = [ex + dx, ey + dy]

        # Check exit
        if self.player_pos == list(self.exit_pos):
            reward += 50.0
            terminated = True

        # Check death
        if self.player_hp <= 0:
            reward -= 100.0
            terminated = True

        # Survival bonus
        reward += 0.1

        # Check truncation
        if self.step_count >= self.max_steps:
            truncated = True

        obs = self._get_obs()
        return obs, reward, terminated, truncated, {}

    def _get_obs(self) -> np.ndarray:
        """Build observation vector."""
        obs = []

        # Player state (7)
        obs.extend([
            self.player_hp / self.player_max_hp,
            1.0,  # max_hp normalized
            0.15,  # attack normalized
            0.08,  # defense normalized
            0.1,   # level normalized
            self.player_pos[0] / self.grid_size,
            self.player_pos[1] / self.grid_size,
        ])

        # Grid (flatten, normalized)
        grid_flat = self.grid.flatten().astype(np.float32) / 4.0
        obs.extend(grid_flat.tolist())

        # Enemies (max_enemies * 4)
        for i in range(self.max_enemies):
            if i < len(self.enemies):
                e = self.enemies[i]
                obs.extend([
                    e["hp"] / e["max_hp"],
                    e["pos"][0] / self.grid_size,
                    e["pos"][1] / self.grid_size,
                    0.1,  # type encoding (simplified)
                ])
            else:
                obs.extend([0.0, 0.0, 0.0, 0.0])

        return np.array(obs, dtype=np.float32)
