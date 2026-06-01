"""Room templates for dungeon generation."""

from dataclasses import dataclass


@dataclass
class RoomTemplate:
    name: str
    min_width: int
    min_height: int
    max_width: int
    max_height: int
    enemy_density: float  # 0.0 - 1.0
    event_density: float  # 0.0 - 1.0
    description: str


# Predefined room templates
EMPTY_ROOM = RoomTemplate(
    name="empty",
    min_width=4, min_height=4,
    max_width=8, max_height=8,
    enemy_density=0.2, event_density=0.1,
    description="A bare room with little of interest."
)

TREASURE_ROOM = RoomTemplate(
    name="treasure",
    min_width=5, min_height=5,
    max_width=7, max_height=7,
    enemy_density=0.3, event_density=0.6,
    description="A room that likely holds valuable items."
)

ENEMY_LAIR = RoomTemplate(
    name="enemy_lair",
    min_width=6, min_height=6,
    max_width=10, max_height=10,
    enemy_density=0.8, event_density=0.1,
    description="A den crawling with hostile creatures."
)

BOSS_ARENA = RoomTemplate(
    name="boss_arena",
    min_width=8, min_height=8,
    max_width=12, max_height=12,
    enemy_density=0.5, event_density=0.0,
    description="A large chamber fit for a fearsome boss."
)

SHOP_ROOM = RoomTemplate(
    name="shop",
    min_width=5, min_height=5,
    max_width=6, max_height=6,
    enemy_density=0.0, event_density=1.0,
    description="A merchant's stall in the depths."
)

ALTAR_ROOM = RoomTemplate(
    name="altar",
    min_width=5, min_height=5,
    max_width=7, max_height=7,
    enemy_density=0.1, event_density=0.8,
    description="A sacred space radiating mysterious power."
)

CORRIDOR = RoomTemplate(
    name="corridor",
    min_width=3, min_height=3,
    max_width=4, max_height=4,
    enemy_density=0.15, event_density=0.05,
    description="A narrow passageway connecting rooms."
)

ALL_TEMPLATES = [EMPTY_ROOM, TREASURE_ROOM, ENEMY_LAIR, BOSS_ARENA, SHOP_ROOM, ALTAR_ROOM]


def get_template_for_floor(floor: int) -> list[RoomTemplate]:
    """Get available room templates based on dungeon floor."""
    templates = [EMPTY_ROOM, CORRIDOR]

    if floor >= 1:
        templates.append(ENEMY_LAIR)
    if floor >= 2:
        templates.append(TREASURE_ROOM)
    if floor >= 3:
        templates.append(SHOP_ROOM)
    if floor >= 5:
        templates.append(ALTAR_ROOM)
    if floor % 5 == 0:
        templates.append(BOSS_ARENA)

    return templates
