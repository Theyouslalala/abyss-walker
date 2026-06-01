@echo off
chcp 65001 >nul
title Abyss Walker - AI Server

echo ============================================
echo   Abyss Walker / 深渊行者 - AI 服务启动
echo ============================================
echo.

REM 尝试找到 conda
set CONDA_EXE=
if exist "D:\anaconda3\condabin\conda.bat" (
    set CONDA_EXE=D:\anaconda3\condabin\conda.bat
) else if exist "%USERPROFILE%\anaconda3\condabin\conda.bat" (
    set CONDA_EXE=%USERPROFILE%\anaconda3\condabin\conda.bat
) else if exist "%USERPROFILE%\miniconda3\condabin\conda.bat" (
    set CONDA_EXE=%USERPROFILE%\miniconda3\condabin\conda.bat
) else (
    echo [错误] 未找到 Conda，请先安装 Anaconda 或 Miniconda
    pause
    exit /b 1
)

echo [1/3] 激活 conda 环境...
call "%CONDA_EXE%" activate abyss-walker
if errorlevel 1 (
    echo [错误] 环境激活失败，请先运行: conda create -n abyss-walker python=3.10
    pause
    exit /b 1
)

echo [2/3] 检查依赖...
python -c "import torch; import gymnasium" 2>nul
if errorlevel 1 (
    echo 安装依赖中...
    pip install torch --index-url https://download.pytorch.org/whl/cpu
    pip install numpy gymnasium pytest tensorboard -i https://pypi.tuna.tsinghua.edu.cn/simple
)

echo [3/3] 启动 AI 服务...
echo.
echo 服务地址: 127.0.0.1:9999
echo 在 Unity 中点击 Play 即可连接
echo 按 Ctrl+C 停止服务
echo ============================================
echo.

python -m ai.server.socket_server

pause
