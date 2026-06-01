"""Reward function design for the RL Agent."""


class RewardCalculator:
    """Calculates rewards for game events during training."""

    def __init__(
        self,
        kill_reward: float = 10.0,
        damage_penalty_ratio: float = 5.0,
        floor_reward: float = 50.0,
        death_penalty: float = -100.0,
        survival_bonus: float = 0.1,
        pickup_reward: float = 5.0,
    ):
        self.kill_reward = kill_reward
        self.damage_penalty_ratio = damage_penalty_ratio
        self.floor_reward = floor_reward
        self.death_penalty = death_penalty
        self.survival_bonus = survival_bonus
        self.pickup_reward = pickup_reward

    def on_enemy_killed(self) -> float:
        return self.kill_reward

    def on_player_damaged(self, damage: int, max_hp: int) -> float:
        return -self.damage_penalty_ratio * (damage / max_hp)

    def on_floor_cleared(self) -> float:
        return self.floor_reward

    def on_player_death(self) -> float:
        return self.death_penalty

    def on_survival(self) -> float:
        return self.survival_bonus

    def on_item_pickup(self) -> float:
        return self.pickup_reward
