"""Tests for dungeon generation."""

import pytest
from ai.mapgen.dungeon_generator import DungeonGenerator, Dungeon


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
