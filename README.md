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
├── game/                  # Unity 游戏项目
│   └── Assets/Scripts/
│       ├── Core/          # 核心框架
│       ├── Map/           # 地图系统
│       ├── Entity/        # 实体系统
│       ├── Combat/        # 战斗系统
│       ├── Events/        # 随机事件
│       ├── Meta/          # Meta 进度
│       ├── Network/       # 通信层
│       └── UI/            # UI 系统
├── ai/                    # Python AI 服务
│   ├── server/            # Socket 服务端
│   ├── agents/            # RL Agent
│   ├── enemies/           # 敌人 AI
│   ├── mapgen/            # 地图生成
│   ├── training/          # 训练脚本
│   └── utils/             # 工具
├── docs/                  # 文档
├── tests/                 # 测试
├── scripts/               # 脚本
├── requirements.txt       # Python 依赖
└── environment.yml        # Conda 环境
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
python -m ai.training.train_agent
```

## 开发模块

| 阶段 | 模块 | 状态 |
|------|------|------|
| M0 | 项目初始化 + Git + 环境搭建 | 进行中 |
| M1 | Python 地图生成 + 测试 | - |
| M2 | Socket 通信层 | - |
| M3 | Unity 基础地图渲染 | - |
| M4 | 玩家移动 + 自动战斗 | - |
| M5 | 敌人 AI | - |
| M6 | 随机事件系统 | - |
| M7 | Meta 进度系统 | - |
| M8 | Gymnasium 环境封装 | - |
| M9 | PPO Agent 训练 | - |
| M10 | UI + 移动端适配 | - |
| M11 | 打磨 + 测试 + 文档 | - |
| M12 | GitHub 发布 | - |

## 许可证

MIT License
