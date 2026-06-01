"""Message protocol for Unity-Python communication.

Messages are serialized as: [4-byte length prefix (big-endian)] [JSON payload]
"""

import json
import struct
from typing import Any


def encode_message(data: dict[str, Any]) -> bytes:
    """Encode a dict into a length-prefixed JSON message."""
    json_bytes = json.dumps(data, ensure_ascii=False).encode("utf-8")
    length_prefix = struct.pack(">I", len(json_bytes))
    return length_prefix + json_bytes


def decode_message(data: bytes) -> dict[str, Any]:
    """Decode a length-prefixed JSON message into a dict."""
    return json.loads(data.decode("utf-8"))


async def read_message(reader) -> dict[str, Any]:
    """Read a single length-prefixed message from an async reader."""
    length_bytes = await reader.readexactly(4)
    length = struct.unpack(">I", length_bytes)[0]
    payload = await reader.readexactly(length)
    return decode_message(payload)


async def send_message(writer, data: dict[str, Any]) -> None:
    """Send a single length-prefixed message through an async writer."""
    message = encode_message(data)
    writer.write(message)
    await writer.drain()
