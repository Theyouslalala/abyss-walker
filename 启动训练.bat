@echo off
chcp 65001 >nul
title Abyss Walker - AI 训练

echo ============================================
echo   Abyss Walker - AI Agent 训练
echo ============================================
echo.

set CONDA_EXE=
if exist "D:\anaconda3\condabin\conda.bat" (
    set CONDA_EXE=D:\anaconda3\condabin\conda.bat
) else if exist "%USERPROFILE%\anaconda3\condabin\conda.bat" (
    set CONDA_EXE=%USERPROFILE%\anaconda3\condabin\conda.bat
) else if exist "%USERPROFILE%\miniconda3\condabin\conda.bat" (
    set CONDA_EXE=%USERPROFILE%\miniconda3\condabin\conda.bat
) else (
    echo [错误] 未找到 Conda
    pause
    exit /b 1
)

call "%CONDA_EXE%" activate abyss-walker

echo 训练参数:
echo   - episodes: 500
echo   - grid_size: 10
echo   - 保存路径: checkpoints/
echo.
echo 训练完成后可用 tensorboard 查看曲线:
echo   tensorboard --logdir runs/
echo ============================================
echo.

python -m ai.training.train_agent --episodes 500 --grid-size 10 --save-every 50

echo.
pause
