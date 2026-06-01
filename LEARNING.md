# Abyss Walker 学习指南

本指南帮助你理解和学习本项目的各个技术模块。

## 前置知识

- **Python 基础** — 变量、函数、类、模块
- **C# 基础** — Unity 脚本编写
- **基本数学** — 坐标系、向量、概率

## 学习路线

### 阶段 1: 项目基础 (M0)

**目标:** 搭建开发环境，理解项目结构

1. 安装 Anaconda/Miniconda
2. 创建 conda 环境: `conda env create -f environment.yml`
3. 安装 Unity 2022 LTS
4. 了解 Git 基本操作

**关键概念:** conda 环境管理、Git 版本控制

### 阶段 2: 地图生成 (M1)

**目标:** 理解 BSP 地牢生成算法

**学习文件:** `ai/mapgen/dungeon_generator.py`

**核心算法:**
```
BSP (Binary Space Partitioning):
1. 将地图递归分割为小矩形
2. 在每个小矩形中生成房间
3. 用走廊连接相邻房间
```

**练习:**
- 修改房间大小范围，观察效果
- 添加新的房间形状 (圆形、L形)
- 可视化生成结果

### 阶段 3: 网络通信 (M2)

**目标:** 理解 TCP Socket 双向通信

**学习文件:**
- `ai/server/socket_server.py` — Python 服务端
- `game/Assets/Scripts/Network/SocketClient.cs` — Unity 客户端

**核心概念:**
- TCP Socket 基础 (connect/bind/listen/accept)
- 异步编程 (asyncio)
- JSON 序列化/反序列化
- 长度前缀消息协议

### 阶段 4: 游戏核心 (M3-M4)

**目标:** 理解回合制游戏逻辑

**学习文件:**
- `game/Assets/Scripts/Core/TurnManager.cs` — 回合管理
- `game/Assets/Scripts/Entity/Player.cs` — 玩家逻辑
- `game/Assets/Scripts/Combat/CombatManager.cs` — 战斗系统

### 阶段 5: 行为树 AI (M5)

**目标:** 理解行为树架构

**学习文件:** `ai/enemies/behavior_tree.py`

**核心概念:**
- 节点类型: Selector (选择)、Sequence (序列)、Action (动作)、Condition (条件)
- 树的执行流程
- 如何组合简单行为产生复杂智能

### 阶段 6: 强化学习 (M8-M9)

**目标:** 理解 PPO 算法和训练流程

**学习文件:**
- `ai/training/env_wrapper.py` — 环境封装
- `ai/agents/model.py` — 神经网络
- `ai/agents/rl_agent.py` — PPO 算法
- `ai/agents/trainer.py` — 训练循环

**核心概念:**
- 强化学习基本框架 (Agent-Environment Loop)
- Actor-Critic 架构
- PPO (Proximal Policy Optimization)
- GAE (Generalized Advantage Estimation)
- 经验回放

**推荐阅读:**
- [Spinning Up in Deep RL](https://spinningup.openai.com/) — OpenAI 的 RL 入门教程
- [PPO 论文](https://arxiv.org/abs/1707.06347) — John Schulman et al.

### 阶段 7: 程序化内容 (M6-M7)

**目标:** 理解随机事件和 Meta 进度设计

**学习文件:**
- `ai/mapgen/event_placer.py` — 事件放置策略
- `game/Assets/Scripts/Meta/ProgressManager.cs` — 进度系统

## 常见问题

### Q: 为什么用 Unity 而不是 Pygame?
A: Unity 对移动端支持更好，2D 工具链更成熟 (Tilemap、Animator 等)。纯 Python 方案在移动端部署困难。

### Q: 为什么 AI 用 Python 而不用 C#?
A: PyTorch 只有 Python 版本，且 Python 的 ML 生态远比 C# 成熟。通过 Socket 通信可以解耦游戏和 AI。

### Q: 如何调试 AI 行为?
A: 使用 TensorBoard 可视化训练曲线。在游戏中可以开启 AI 调试模式显示决策过程。

## 进一步学习

- [Unity 官方教程](https://learn.unity.com/)
- [PyTorch 官方教程](https://pytorch.org/tutorials/)
- [Gymnasium 文档](https://gymnasium.fauxpilot.io/)
- [Spinning Up in Deep RL](https://spinningup.openai.com/)
