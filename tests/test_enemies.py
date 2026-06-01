"""Tests for enemy AI behavior trees."""

import pytest
from ai.enemies.behavior_tree import NodeStatus, Selector, Sequence, Condition, Action
from ai.enemies.enemy_ai import get_enemy_decision, create_skeleton_bt, create_goblin_bt


def test_skeleton_attacks_when_close():
    context = {
        "enemy_pos": [5, 5],
        "player_pos": [5, 6],  # 1 distance away
        "enemy_hp": 30,
        "enemy_max_hp": 30,
    }
    decision = get_enemy_decision("skeleton", context)
    assert decision["type"] == "attack"


def test_skeleton_chases_when_in_sight():
    context = {
        "enemy_pos": [5, 5],
        "player_pos": [7, 5],  # 2 distance, within sight
        "enemy_hp": 30,
        "enemy_max_hp": 30,
    }
    decision = get_enemy_decision("skeleton", context)
    assert decision["type"] == "move"


def test_skeleton_patrols_when_far():
    context = {
        "enemy_pos": [5, 5],
        "player_pos": [20, 20],  # Far away
        "enemy_hp": 30,
        "enemy_max_hp": 30,
    }
    decision = get_enemy_decision("skeleton", context)
    assert decision["type"] == "move"


def test_goblin_flees_when_low_hp():
    context = {
        "enemy_pos": [5, 5],
        "player_pos": [6, 5],  # Close
        "enemy_hp": 5,  # Low HP (30% of 30 is 9, so 5 < 9)
        "enemy_max_hp": 30,
    }
    decision = get_enemy_decision("goblin", context)
    assert decision["type"] == "move"


def test_shadow_mage_uses_ranged_attack():
    context = {
        "enemy_pos": [5, 5],
        "player_pos": [7, 5],  # 2 distance, within range 3
        "enemy_hp": 20,
        "enemy_max_hp": 20,
    }
    decision = get_enemy_decision("shadow_mage", context)
    assert decision["type"] == "ranged_attack"


def test_unknown_enemy_defaults_to_skeleton():
    context = {
        "enemy_pos": [5, 5],
        "player_pos": [5, 6],
        "enemy_hp": 30,
        "enemy_max_hp": 30,
    }
    decision = get_enemy_decision("unknown_type", context)
    assert decision["type"] == "attack"


def test_behavior_tree_selector():
    # Selector should return SUCCESS on first successful child
    always_fail = Action(lambda ctx: NodeStatus.FAILURE)
    always_success = Action(lambda ctx: NodeStatus.SUCCESS)

    selector = Selector([always_fail, always_success])
    assert selector.tick({}) == NodeStatus.SUCCESS


def test_behavior_tree_sequence():
    # Sequence should return FAILURE on first failing child
    always_success = Action(lambda ctx: NodeStatus.SUCCESS)
    always_fail = Action(lambda ctx: NodeStatus.FAILURE)

    sequence = Sequence([always_success, always_fail])
    assert sequence.tick({}) == NodeStatus.FAILURE
