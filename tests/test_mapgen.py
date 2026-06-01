"""Tests for dungeon generation."""

import pytest
from ai.mapgen.dungeon_generator import DungeonGenerator, Dungeon
from ai.mapgen.room_templates import get_template_for_floor, ALL_TEMPLATES
from ai.utils.visualization import print_dungeon


def test_dungeon_generation_creates_rooms():
    gen = DungeonGenerator(width=40, height=30, seed=42)
    dungeon = gen.generate(floor=1)
    assert len(dungeon.rooms) >= 2


def test_dungeon_has_spawn_and_exit():
    gen = DungeonGenerator(width=40, height=30, seed=42)
    dungeon = gen.generate(floor=1)
    assert dungeon.spawn_point != (0, 0)
    assert dungeon.exit_point != (0, 0)
    assert dungeon.spawn_point != dungeon.exit_point


def test_dungeon_tiles_are_valid():
    gen = DungeonGenerator(width=40, height=30, seed=42)
    dungeon = gen.generate(floor=1)
    assert len(dungeon.tiles) == 30
    assert len(dungeon.tiles[0]) == 40
    for row in dungeon.tiles:
        for tile in row:
            assert tile in (Dungeon.WALL, Dungeon.FLOOR, Dungeon.DOOR, Dungeon.CHEST, Dungeon.EXIT)


def test_dungeon_rooms_are_walkable():
    gen = DungeonGenerator(width=40, height=30, seed=42)
    dungeon = gen.generate(floor=1)
    for room in dungeon.rooms:
        for y in range(room.y, room.y + room.h):
            for x in range(room.x, room.x + room.w):
                if 0 <= x < dungeon.width and 0 <= y < dungeon.height:
                    assert dungeon.tiles[y][x] in (Dungeon.FLOOR, Dungeon.EXIT)


def test_dungeon_spawn_in_floor():
    gen = DungeonGenerator(width=40, height=30, seed=42)
    dungeon = gen.generate(floor=1)
    sx, sy = dungeon.spawn_point
    assert dungeon.is_walkable(sx, sy)


def test_dungeon_enemies_placed():
    gen = DungeonGenerator(width=40, height=30, seed=42)
    dungeon = gen.generate(floor=3)
    assert len(dungeon.enemies) > 0
    for enemy in dungeon.enemies:
        assert "type" in enemy
        assert "pos" in enemy
        assert "hp" in enemy


def test_different_seeds_produce_different_dungeons():
    gen1 = DungeonGenerator(width=40, height=30, seed=1)
    gen2 = DungeonGenerator(width=40, height=30, seed=2)
    d1 = gen1.generate(floor=1)
    d2 = gen2.generate(floor=1)
    assert d1.tiles != d2.tiles


def test_small_dungeon():
    gen = DungeonGenerator(width=20, height=15, min_room_size=3, seed=42)
    dungeon = gen.generate(floor=1)
    assert len(dungeon.rooms) >= 1
    assert len(dungeon.tiles) == 15


def test_room_templates_per_floor():
    templates_1 = get_template_for_floor(1)
    templates_5 = get_template_for_floor(5)
    templates_10 = get_template_for_floor(10)
    assert len(templates_1) < len(templates_5)
    assert len(templates_5) <= len(templates_10)


def test_dungeon_ascii_output():
    gen = DungeonGenerator(width=20, height=10, seed=42)
    dungeon = gen.generate(floor=1)
    ascii_map = print_dungeon(dungeon)
    assert isinstance(ascii_map, str)
    assert len(ascii_map.split("\n")) == 10


def test_dungeon_higher_floors_have_more_enemies():
    gen = DungeonGenerator(width=40, height=30, seed=42)
    d1 = gen.generate(floor=1)
    d5 = gen.generate(floor=5)
    assert len(d5.enemies) >= len(d1.enemies)


def test_room_center():
    from ai.mapgen.dungeon_generator import Room
    room = Room(x=10, y=20, w=6, h=4)
    cx, cy = room.center
    assert cx == 13
    assert cy == 22


def test_dungeon_is_walkable_bounds():
    gen = DungeonGenerator(width=20, height=15, seed=42)
    dungeon = gen.generate(floor=1)
    assert not dungeon.is_walkable(-1, 0)
    assert not dungeon.is_walkable(0, -1)
    assert not dungeon.is_walkable(20, 0)
    assert not dungeon.is_walkable(0, 15)
