"""Adaptive difficulty system."""


class DifficultyManager:
    """Adjusts game difficulty based on player performance."""

    def __init__(self, base_difficulty: float = 1.0):
        self.base_difficulty = base_difficulty
        self.current_difficulty = base_difficulty
        self.player_deaths = 0
        self.enemies_killed = 0
        self.floors_cleared = 0

    def update(self, event: str, **kwargs):
        """Update difficulty based on game events."""
        if event == "player_death":
            self.player_deaths += 1
            self.current_difficulty = max(0.5, self.current_difficulty - 0.1)
        elif event == "enemy_killed":
            self.enemies_killed += 1
            self.current_difficulty = min(3.0, self.current_difficulty + 0.02)
        elif event == "floor_cleared":
            self.floors_cleared += 1
            self.current_difficulty = min(3.0, self.current_difficulty + 0.05)

    def get_enemy_hp_multiplier(self) -> float:
        return self.current_difficulty

    def get_enemy_attack_multiplier(self) -> float:
        return self.current_difficulty * 0.8 + 0.2

    def get_enemy_count_multiplier(self) -> float:
        return min(2.0, self.current_difficulty * 0.5 + 0.5)
