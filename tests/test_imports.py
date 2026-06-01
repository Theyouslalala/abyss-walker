"""Verify all modules can be imported without errors."""

def test_import_server():
    from ai.server.config import HOST, PORT
    from ai.server.protocol import encode_message, decode_message, read_message, send_message
    from ai.server.socket_server import GameServer

def test_import_mapgen():
    from ai.mapgen.dungeon_generator import DungeonGenerator, Dungeon, Room, BSPNode
    from ai.mapgen.event_placer import EventPlacer

def test_import_enemies():
    from ai.enemies.behavior_tree import BTNode, Selector, Sequence, Condition, Action, NodeStatus
    from ai.enemies.enemy_ai import get_enemy_decision, ENEMY_BT_MAP
    from ai.enemies.difficulty import DifficultyManager

def test_import_agents():
    from ai.agents.model import ActorCritic
    from ai.agents.rl_agent import PPOAgent
    from ai.agents.replay_buffer import RolloutBuffer

def test_import_training():
    from ai.training.env_wrapper import AbyssWalkerEnv
    from ai.training.reward_function import RewardCalculator
    from ai.utils.visualization import TrainingLogger
