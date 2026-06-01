"""BSP-based dungeon generator."""

import random
from dataclasses import dataclass, field


@dataclass
class Room:
    x: int
    y: int
    w: int
    h: int

    @property
    def center(self) -> tuple[int, int]:
        return self.x + self.w // 2, self.y + self.h // 2

    @property
    def right(self) -> int:
        return self.x + self.w

    @property
    def bottom(self) -> int:
        return self.y + self.h


@dataclass
class BSPNode:
    x: int
    y: int
    w: int
    h: int
    left: "BSPNode | None" = None
    right: "BSPNode | None" = None
    room: Room | None = None

    @property
    def is_leaf(self) -> bool:
        return self.left is None and self.right is None


@dataclass
class Dungeon:
    width: int
    height: int
    tiles: list[list[int]] = field(default_factory=list)
    rooms: list[Room] = field(default_factory=list)
    spawn_point: tuple[int, int] = (0, 0)
    exit_point: tuple[int, int] = (0, 0)
    enemies: list[dict] = field(default_factory=list)
    events: list[dict] = field(default_factory=list)

    # Tile types
    WALL = 1
    FLOOR = 0
    DOOR = 2
    CHEST = 3
    EXIT = 4

    def is_walkable(self, x: int, y: int) -> bool:
        if 0 <= x < self.width and 0 <= y < self.height:
            return self.tiles[y][x] != self.WALL
        return False


class DungeonGenerator:
    """Generates dungeons using Binary Space Partitioning."""

    def __init__(
        self,
        width: int = 40,
        height: int = 30,
        min_room_size: int = 5,
        max_room_ratio: float = 0.6,
        min_leaf_size: int = 8,
        seed: int | None = None,
    ):
        self.width = width
        self.height = height
        self.min_room_size = min_room_size
        self.max_room_ratio = max_room_ratio
        self.min_leaf_size = min_leaf_size
        if seed is not None:
            random.seed(seed)

    def generate(self, floor: int = 1) -> Dungeon:
        """Generate a complete dungeon for the given floor."""
        dungeon = Dungeon(width=self.width, height=self.height)
        dungeon.tiles = [[Dungeon.WALL] * self.width for _ in range(self.height)]

        # BSP split
        root = BSPNode(0, 0, self.width, self.height)
        self._split(root)

        # Generate rooms in leaf nodes
        self._generate_rooms(root, dungeon)

        # Connect rooms with corridors
        self._connect_rooms(root, dungeon)

        # Place spawn and exit
        self._place_spawn_exit(dungeon)

        # Place enemies and events based on floor
        self._place_enemies(dungeon, floor)
        self._place_events(dungeon, floor)

        return dungeon

    def _split(self, node: BSPNode):
        """Recursively split BSP nodes."""
        if node.w < self.min_leaf_size * 2 and node.h < self.min_leaf_size * 2:
            return

        # Choose split direction (prefer splitting the longer axis)
        if node.w > node.h:
            horizontal = False
        elif node.h > node.w:
            horizontal = True
        else:
            horizontal = random.choice([True, False])

        if horizontal:
            if node.h < self.min_leaf_size * 2:
                return
            split = random.randint(self.min_leaf_size, node.h - self.min_leaf_size)
            node.left = BSPNode(node.x, node.y, node.w, split)
            node.right = BSPNode(node.x, node.y + split, node.w, node.h - split)
        else:
            if node.w < self.min_leaf_size * 2:
                return
            split = random.randint(self.min_leaf_size, node.w - self.min_leaf_size)
            node.left = BSPNode(node.x, node.y, split, node.h)
            node.right = BSPNode(node.x + split, node.y, node.w - split, node.h)

        self._split(node.left)
        self._split(node.right)

    def _generate_rooms(self, node: BSPNode, dungeon: Dungeon):
        """Generate rooms in leaf BSP nodes."""
        if node.is_leaf:
            margin = 1
            max_w = node.w - 2 * margin
            max_h = node.h - 2 * margin
            if max_w < self.min_room_size or max_h < self.min_room_size:
                return

            w = random.randint(self.min_room_size, max(self.min_room_size, int(max_w * self.max_room_ratio)))
            h = random.randint(self.min_room_size, max(self.min_room_size, int(max_h * self.max_room_ratio)))
            x = node.x + margin + random.randint(0, max(0, max_w - w))
            y = node.y + margin + random.randint(0, max(0, max_h - h))

            room = Room(x, y, w, h)
            node.room = room
            dungeon.rooms.append(room)

            # Carve room into tiles
            for ry in range(y, y + h):
                for rx in range(x, x + w):
                    if 0 <= rx < dungeon.width and 0 <= ry < dungeon.height:
                        dungeon.tiles[ry][rx] = Dungeon.FLOOR
        else:
            if node.left:
                self._generate_rooms(node.left, dungeon)
            if node.right:
                self._generate_rooms(node.right, dungeon)

    def _connect_rooms(self, node: BSPNode, dungeon: Dungeon):
        """Connect rooms in sibling BSP nodes with corridors."""
        if node.is_leaf:
            return node.room

        left_room = self._connect_rooms(node.left, dungeon) if node.left else None
        right_room = self._connect_rooms(node.right, dungeon) if node.right else None

        if left_room and right_room:
            self._carve_corridor(dungeon, left_room.center, right_room.center)

        return left_room or right_room

    def _carve_corridor(self, dungeon: Dungeon, start: tuple[int, int], end: tuple[int, int]):
        """Carve an L-shaped corridor between two points."""
        x1, y1 = start
        x2, y2 = end

        # Randomly choose L-shape direction
        if random.choice([True, False]):
            # Horizontal first, then vertical
            self._carve_horizontal(dungeon, x1, x2, y1)
            self._carve_vertical(dungeon, y1, y2, x2)
        else:
            # Vertical first, then horizontal
            self._carve_vertical(dungeon, y1, y2, x1)
            self._carve_horizontal(dungeon, x1, x2, y2)

    def _carve_horizontal(self, dungeon: Dungeon, x1: int, x2: int, y: int):
        for x in range(min(x1, x2), max(x1, x2) + 1):
            if 0 <= x < dungeon.width and 0 <= y < dungeon.height:
                if dungeon.tiles[y][x] == Dungeon.WALL:
                    dungeon.tiles[y][x] = Dungeon.FLOOR

    def _carve_vertical(self, dungeon: Dungeon, y1: int, y2: int, x: int):
        for y in range(min(y1, y2), max(y1, y2) + 1):
            if 0 <= x < dungeon.width and 0 <= y < dungeon.height:
                if dungeon.tiles[y][x] == Dungeon.WALL:
                    dungeon.tiles[y][x] = Dungeon.FLOOR

    def _place_spawn_exit(self, dungeon: Dungeon):
        """Place spawn point in first room and exit in last room."""
        if len(dungeon.rooms) >= 2:
            dungeon.spawn_point = dungeon.rooms[0].center
            dungeon.exit_point = dungeon.rooms[-1].center
            ex, ey = dungeon.exit_point
            dungeon.tiles[ey][ex] = Dungeon.EXIT
        elif len(dungeon.rooms) == 1:
            dungeon.spawn_point = dungeon.rooms[0].center
            dungeon.exit_point = dungeon.rooms[0].center

    def _place_enemies(self, dungeon: Dungeon, floor: int):
        """Place enemies in rooms based on floor difficulty."""
        enemy_types = ["skeleton", "goblin", "shadow_mage"]
        num_enemies = min(3 + floor, len(dungeon.rooms) * 2)

        for i in range(num_enemies):
            room = random.choice(dungeon.rooms[1:]) if len(dungeon.rooms) > 1 else dungeon.rooms[0]
            ex = random.randint(room.x + 1, room.x + room.w - 2)
            ey = random.randint(room.y + 1, room.y + room.h - 2)

            enemy_type = random.choice(enemy_types[: min(floor, len(enemy_types))])
            hp = 20 + floor * 5
            attack = 5 + floor * 2

            dungeon.enemies.append(
                {
                    "id": i + 1,
                    "type": enemy_type,
                    "pos": [ex, ey],
                    "hp": hp,
                    "max_hp": hp,
                    "attack": attack,
                }
            )

    def _place_events(self, dungeon: Dungeon, floor: int):
        """Place random events in rooms."""
        event_types = ["treasure", "trap", "shop"]

        for room in dungeon.rooms[1:-1]:  # Skip spawn and boss rooms
            if random.random() < 0.4:
                ex = random.randint(room.x + 1, room.x + room.w - 2)
                ey = random.randint(room.y + 1, room.y + room.h - 2)
                event_type = random.choice(event_types)

                dungeon.events.append(
                    {
                        "type": event_type,
                        "pos": [ex, ey],
                        "active": True,
                    }
                )
