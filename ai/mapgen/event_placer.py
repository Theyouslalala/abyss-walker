"""Event placement strategies for dungeon maps."""

import random
from ai.mapgen.dungeon_generator import Dungeon, Room


class EventPlacer:
    """Places events and items in dungeon rooms."""

    def __init__(self, seed: int | None = None):
        if seed is not None:
            random.seed(seed)

    def place_treasure(self, dungeon: Dungeon, room: Room) -> dict | None:
        """Place a treasure chest in a room."""
        if room.w <= 2 or room.h <= 2:
            return None
        x = random.randint(room.x + 1, room.x + room.w - 2)
        y = random.randint(room.y + 1, room.y + room.h - 2)
        return {
            "type": "treasure",
            "pos": [x, y],
            "active": True,
            "contents": random.choice(["gold", "potion", "weapon", "armor"]),
        }

    def place_trap(self, dungeon: Dungeon, room: Room) -> dict | None:
        """Place a hidden trap in a room."""
        if room.w <= 2 or room.h <= 2:
            return None
        x = random.randint(room.x + 1, room.x + room.w - 2)
        y = random.randint(room.y + 1, room.y + room.h - 2)
        return {
            "type": "trap",
            "pos": [x, y],
            "active": True,
            "damage": random.randint(5, 15),
        }

    def place_altar(self, dungeon: Dungeon, room: Room) -> dict | None:
        """Place a blessing altar in a room."""
        return {
            "type": "altar",
            "pos": room.center,
            "active": True,
            "blessings": ["hp_boost", "atk_boost", "def_boost", "new_skill"],
        }
