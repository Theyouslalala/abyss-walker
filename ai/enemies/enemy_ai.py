"""Enemy AI using behavior trees."""

import random
from ai.enemies.behavior_tree import (
    Action,
    BTNode,
    Condition,
    NodeStatus,
    Selector,
    Sequence,
)


def _is_player_in_range(context: dict, attack_range: int = 1) -> bool:
    ex, ey = context.get("enemy_pos", [0, 0])
    px, py = context.get("player_pos", [0, 0])
    return abs(ex - px) + abs(ey - py) <= attack_range


def _is_player_in_sight(context: dict, sight_range: int = 3) -> bool:
    ex, ey = context.get("enemy_pos", [0, 0])
    px, py = context.get("player_pos", [0, 0])
    return abs(ex - px) + abs(ey - py) <= sight_range


def _attack_player(context: dict) -> NodeStatus:
    context["action"] = {"type": "attack", "target": "player"}
    return NodeStatus.SUCCESS


def _move_toward_player(context: dict) -> NodeStatus:
    ex, ey = context.get("enemy_pos", [0, 0])
    px, py = context.get("player_pos", [0, 0])
    dx = 1 if px > ex else (-1 if px < ex else 0)
    dy = 1 if py > ey else (-1 if py < ey else 0)

    # Prefer horizontal or vertical movement
    if random.choice([True, False]):
        context["action"] = {"type": "move", "to": [ex + dx, ey]}
    else:
        context["action"] = {"type": "move", "to": [ex, ey + dy]}
    return NodeStatus.SUCCESS


def _patrol(context: dict) -> NodeStatus:
    ex, ey = context.get("enemy_pos", [0, 0])
    dx, dy = random.choice([(0, 1), (0, -1), (1, 0), (-1, 0)])
    context["action"] = {"type": "move", "to": [ex + dx, ey + dy]}
    return NodeStatus.SUCCESS


def create_skeleton_bt() -> BTNode:
    """Skeleton: simple chase and attack."""
    return Selector(
        [
            Sequence(
                [
                    Condition(lambda ctx: _is_player_in_range(ctx, 1)),
                    Action(_attack_player),
                ]
            ),
            Sequence(
                [
                    Condition(lambda ctx: _is_player_in_sight(ctx, 3)),
                    Action(_move_toward_player),
                ]
            ),
            Action(_patrol),
        ]
    )


def create_goblin_bt() -> BTNode:
    """Goblin: flanking behavior, runs away when low HP."""

    def _should_flee(ctx: dict) -> bool:
        return ctx.get("enemy_hp", 100) < ctx.get("enemy_max_hp", 100) * 0.3

    def _flee(context: dict) -> NodeStatus:
        ex, ey = context.get("enemy_pos", [0, 0])
        px, py = context.get("player_pos", [0, 0])
        dx = -1 if px > ex else (1 if px < ex else 0)
        dy = -1 if py > ey else (1 if py < ey else 0)
        context["action"] = {"type": "move", "to": [ex + dx, ey + dy]}
        return NodeStatus.SUCCESS

    return Selector(
        [
            Sequence([Condition(_should_flee), Action(_flee)]),
            Sequence(
                [
                    Condition(lambda ctx: _is_player_in_range(ctx, 1)),
                    Action(_attack_player),
                ]
            ),
            Sequence(
                [
                    Condition(lambda ctx: _is_player_in_sight(ctx, 4)),
                    Action(_move_toward_player),
                ]
            ),
            Action(_patrol),
        ]
    )


def create_shadow_mage_bt() -> BTNode:
    """Shadow mage: keeps distance, attacks from range."""

    def _ranged_attack(context: dict) -> NodeStatus:
        context["action"] = {"type": "ranged_attack", "target": "player", "range": 3}
        return NodeStatus.SUCCESS

    def _keep_distance(context: dict) -> NodeStatus:
        ex, ey = context.get("enemy_pos", [0, 0])
        px, py = context.get("player_pos", [0, 0])
        dx = -1 if px > ex else (1 if px < ex else 0)
        dy = -1 if py > ey else (1 if py < ey else 0)
        context["action"] = {"type": "move", "to": [ex + dx, ey + dy]}
        return NodeStatus.SUCCESS

    return Selector(
        [
            Sequence(
                [
                    Condition(lambda ctx: _is_player_in_range(ctx, 3)),
                    Action(_ranged_attack),
                ]
            ),
            Action(_keep_distance),
        ]
    )


ENEMY_BT_MAP = {
    "skeleton": create_skeleton_bt,
    "goblin": create_goblin_bt,
    "shadow_mage": create_shadow_mage_bt,
}


def get_enemy_decision(enemy_type: str, context: dict) -> dict:
    """Get the decision for an enemy given its type and game context."""
    bt_factory = ENEMY_BT_MAP.get(enemy_type, create_skeleton_bt)
    bt = bt_factory()
    bt.tick(context)
    return context.get("action", {"type": "idle"})
