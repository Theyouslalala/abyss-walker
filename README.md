# Abyss Walker / 深渊行者

一款中世纪奇幻 Roguelike 游戏，融合回合制地牢探索、自动战斗和 AI 驱动的智能敌人系统。

## 特色

- **回合制地牢探索** — 随机生成的 2D 网格地牢，逐层深入
- **自动战斗** — 角色自动攻击，玩家专注于走位和技能策略
- **Meta 进度** — 每局结束后永久升级，逐步解锁新内容
- **随机事件** — 宝箱、陷阱、商店、祭坛等丰富事件
- **AI Agent** — 基于 PyTorch 强化学习的自主游戏 Agent
- **智能敌人** — 行为树 + 神经网络混合的敌人 AI
- **程序化生成** — BSP 算法生成随机关卡

## 技术栈

| 层面 | 技术 |
|------|------|
| 游戏引擎 | Unity 2022+ (C#) |
| AI / ML | Python 3.10 + PyTorch 2.x |
| 强化学习 | PPO + Gymnasium |
| 通信 | TCP Socket + JSON |
| 地图生成 | BSP (Binary Space Partitioning) |
| 环境管理 | Conda |

## 项目结构

```
abyss-walker/
├── game/Assets/Scripts/       # Unity C# 游戏代码
│   ├── Core/                  # GameManager, TurnManager, EventManager, GridManager
│   ├── Map/                   # DungeonRenderer, Room
│   ├── Entity/                # Player, Enemy, EntityStats, EntityManager
│   ├── Combat/                # CombatManager, SkillSystem, AutoAttack, DamageCalculator
│   ├── Events/                # EventData, TreasureEvent, TrapEvent, ShopEvent, AltarEvent
│   ├── Meta/                  # ProgressManager, UnlockSystem, SaveData, RunManager
│   ├── Network/               # SocketClient, MessageProtocol, GameStateSerializer
│   └── UI/                    # HUDController, MenuController, MobileInputHandler, PopupUI
├── ai/                        # Python AI 服务
│   ├── server/                # asyncio TCP 服务端 + 消息协议
│   ├── agents/                # PPO Agent (Actor-Critic 神经网络)
│   ├── enemies/               # 行为树敌人 AI (骷髅/哥布林/暗影法师)
│   ├── mapgen/                # BSP 地牢生成器 + 房间模板
│   ├── training/              # Gymnasium 环境 + 训练脚本 + 奖励函数
│   └── utils/                 # TensorBoard 日志 + ASCII 地图可视化
├── tests/                     # 37 个 Python 单元测试
├── docs/                      # 设计文档
├── scripts/                   # 环境配置和启动脚本
├── requirements.txt           # Python 依赖
└── environment.yml            # Conda 环境
```

## 快速开始

### 1. 环境配置

```bash
# 创建 conda 环境
conda env create -f environment.yml
conda activate abyss-walker

# 或手动安装
pip install -r requirements.txt
```

### 2. 启动 AI 服务

```bash
python -m ai.server.socket_server
```

### 3. 打开 Unity 项目

用 Unity Hub 打开 `game/` 目录。

### 4. 训练 AI Agent

```bash
python -m ai.training.train_agent --episodes 1000
```

### 5. 运行测试

```bash
python -m pytest tests/ -v
```

## 开发模块

| 阶段 | 模块 | 文件数 | 状态 |
|------|------|--------|------|
| M0 | 项目初始化 + Git + 环境搭建 | 6 | 完成 |
| M1 | Python 地图生成 (BSP) | 3 | 完成 |
| M2 | Socket 通信层 (双端) | 5 | 完成 |
| M3 | Unity 基础地图渲染 | 3 | 完成 |
| M4 | 玩家移动 + 自动战斗 | 4 | 完成 |
| M5 | 敌人 AI (行为树) | 3 | 完成 |
| M6 | 随机事件系统 | 7 | 完成 |
| M7 | Meta 进度系统 | 4 | 完成 |
| M8 | Gymnasium 环境封装 | 2 | 完成 |
| M9 | PPO Agent 训练 | 4 | 完成 |
| M10 | UI + 移动端适配 | 4 | 完成 |
| M11 | 测试 + 文档 | 5 | 完成 |
| M12 | GitHub 发布 | - | 完成 |

## Python 模块说明

### 地图生成 (`ai/mapgen/`)
- `dungeon_generator.py` — BSP 递归分割算法，生成房间和走廊
- `room_templates.py` — 7 种房间模板（空房间/宝箱房/怪物巢穴/Boss 竞技场/商店/祭坛/走廊）
- `event_placer.py` — 事件放置策略

### 敌人 AI (`ai/enemies/`)
- `behavior_tree.py` — 行为树框架（Selector/Sequence/Condition/Action）
- `enemy_ai.py` — 3 种敌人行为：骷髅（追击）、哥布林（包抄+逃跑）、暗影法师（远程）
- `difficulty.py` — 自适应难度系统

### 强化学习 (`ai/agents/` + `ai/training/`)
- `model.py` — Actor-Critic 神经网络（共享特征 + 策略/价值双头）
- `rl_agent.py` — PPO 算法实现（Clip 目标 + GAE 优势估计）
- `replay_buffer.py` — 经验回放缓冲区
- `env_wrapper.py` — Gymnasium 环境封装
- `reward_function.py` — 奖励函数设计

### 通信 (`ai/server/`)
- `socket_server.py` — asyncio TCP 服务端
- `protocol.py` — 长度前缀 + JSON 消息协议
- `config.py` — 连接配置

## Unity 模块说明

### 核心框架 (`Core/`)
- `GameManager.cs` — 单例游戏管理器，管理游戏状态流转
- `TurnManager.cs` — 回合制系统（玩家回合 → 敌人回合 → 环境回合）
- `EventManager.cs` — 中央事件总线（16 种事件）
- `GridManager.cs` — 2D 网格系统（坐标转换、寻路、邻居查询）

### 通信层 (`Network/`)
- `SocketClient.cs` — TCP 客户端，异步读写，自动重连
- `MessageProtocol.cs` — 消息类型定义和序列化
- `GameStateSerializer.cs` — 游戏状态与网络消息的桥接

## 许可证

MIT License
