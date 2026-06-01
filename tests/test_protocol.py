"""Tests for the message protocol."""

import pytest
from ai.server.protocol import encode_message, decode_message


def test_encode_decode_roundtrip():
    data = {"type": "game_state", "payload": {"player": {"hp": 100}}}
    encoded = encode_message(data)
    # First 4 bytes are length prefix
    import struct
    length = struct.unpack(">I", encoded[:4])[0]
    assert length == len(encoded) - 4
    decoded = decode_message(encoded[4:])
    assert decoded == data


def test_encode_empty_dict():
    data = {}
    encoded = encode_message(data)
    decoded = decode_message(encoded[4:])
    assert decoded == {}


def test_encode_unicode():
    data = {"name": "深渊行者", "type": "boss"}
    encoded = encode_message(data)
    decoded = decode_message(encoded[4:])
    assert decoded["name"] == "深渊行者"


def test_encode_nested():
    data = {
        "type": "ai_decision",
        "payload": {
            "agent_action": {"move": [3, 6], "skill": "slash"},
            "enemy_actions": [{"id": 1, "move": [6, 2]}],
        },
    }
    encoded = encode_message(data)
    decoded = decode_message(encoded[4:])
    assert decoded["payload"]["agent_action"]["move"] == [3, 6]
    assert len(decoded["payload"]["enemy_actions"]) == 1
