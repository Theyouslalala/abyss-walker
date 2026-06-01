"""Async TCP server for Unity-Python communication."""

import asyncio
import logging
import random

from ai.server.config import HOST, PORT
from ai.server.protocol import read_message, send_message

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger(__name__)


class GameServer:
    """Handles connections from Unity clients and dispatches AI decisions."""

    def __init__(self):
        self.clients: list[asyncio.StreamWriter] = []

    async def handle_client(self, reader: asyncio.StreamReader, writer: asyncio.StreamWriter):
        addr = writer.get_extra_info("peername")
        logger.info(f"Client connected: {addr}")
        self.clients.append(writer)

        try:
            while True:
                message = await read_message(reader)
                response = await self.process_message(message)
                if response:
                    await send_message(writer, response)
        except (asyncio.IncompleteReadError, ConnectionResetError):
            logger.info(f"Client disconnected: {addr}")
        finally:
            if writer in self.clients:
                self.clients.remove(writer)
            writer.close()
            await writer.wait_closed()

    async def process_message(self, message: dict) -> dict | None:
        """Process a message from Unity and return a response."""
        msg_type = message.get("type")

        if msg_type == "request_decision":
            return await self.handle_decision_request(message)
        elif msg_type == "game_state":
            logger.info("Received game state update")
            return None
        else:
            logger.warning(f"Unknown message type: {msg_type}")
            return None

    async def handle_decision_request(self, message: dict) -> dict:
        """Handle an AI decision request from Unity."""
        request_id = message.get("request_id", "")
        payload = message.get("payload", {})

        # Placeholder: return a simple random decision
        # This will be replaced by actual AI agent logic
        player = payload.get("player", {})
        enemies = payload.get("enemies", [])
        pos = player.get("pos", [0, 0])
        px, py = pos[0], pos[1] if len(pos) > 1 else 0

        # Simple movement decision
        directions = [(0, 1), (0, -1), (1, 0), (-1, 0), (0, 0)]
        dx, dy = random.choice(directions)
        move = [px + dx, py + dy]

        response = {
            "type": "ai_decision",
            "request_id": request_id,
            "payload": {
                "agent_action": {
                    "move": move,
                    "skill": None,
                    "target_id": None,
                },
                "enemy_actions": [
                    {
                        "id": e.get("id"),
                        "move": e.get("pos", [0, 0]),
                        "attack_player": False,
                    }
                    for e in enemies
                ],
            },
        }
        return response

    async def start(self):
        server = await asyncio.start_server(self.handle_client, HOST, PORT)
        logger.info(f"AI Server listening on {HOST}:{PORT}")
        async with server:
            await server.serve_forever()


def main():
    server = GameServer()
    asyncio.run(server.start())


if __name__ == "__main__":
    main()
