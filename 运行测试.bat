@echo off
chcp 65001 >nul
title Abyss Walker - 测试

echo ============================================
echo   Abyss Walker - 运行测试
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
python -m pytest tests/ -v

echo.
pause
