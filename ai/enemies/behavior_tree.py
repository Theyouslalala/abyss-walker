"""Behavior tree framework for enemy AI."""

from __future__ import annotations
from abc import ABC, abstractmethod
from enum import Enum


class NodeStatus(Enum):
    SUCCESS = "success"
    FAILURE = "failure"
    RUNNING = "running"


class BTNode(ABC):
    """Base behavior tree node."""

    @abstractmethod
    def tick(self, context: dict) -> NodeStatus:
        pass


class Selector(BTNode):
    """Try children in order, return SUCCESS on first success."""

    def __init__(self, children: list[BTNode]):
        self.children = children

    def tick(self, context: dict) -> NodeStatus:
        for child in self.children:
            status = child.tick(context)
            if status != NodeStatus.FAILURE:
                return status
        return NodeStatus.FAILURE


class Sequence(BTNode):
    """Execute children in order, return FAILURE on first failure."""

    def __init__(self, children: list[BTNode]):
        self.children = children

    def tick(self, context: dict) -> NodeStatus:
        for child in self.children:
            status = child.tick(context)
            if status != NodeStatus.SUCCESS:
                return status
        return NodeStatus.SUCCESS


class Condition(BTNode):
    """Check a condition function."""

    def __init__(self, check: callable):
        self.check = check

    def tick(self, context: dict) -> NodeStatus:
        return NodeStatus.SUCCESS if self.check(context) else NodeStatus.FAILURE


class Action(BTNode):
    """Execute an action function."""

    def __init__(self, action: callable):
        self.action = action

    def tick(self, context: dict) -> NodeStatus:
        return self.action(context)
