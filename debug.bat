@echo off
echo === 诊断开始 ===
echo.

echo [1] 检查 conda 是否存在...
if exist "D:\anaconda3\condabin\conda.bat" (
    echo   找到: D:\anaconda3\condabin\conda.bat
) else (
    echo   未找到 D:\anaconda3\condabin\conda.bat
    echo   请告诉我你的 conda 安装在哪个目录
    goto :end
)

echo.
echo [2] 激活环境...
call D:\anaconda3\condabin\conda.bat activate abyss-walker
if errorlevel 1 (
    echo   环境激活失败!
    goto :end
)
echo   环境激活成功

echo.
echo [3] 检查 Python...
python --version
if errorlevel 1 (
    echo   Python 找不到!
    goto :end
)

echo.
echo [4] 检查 torch...
python -c "import torch; print('torch:', torch.__version__)"
if errorlevel 1 (
    echo   torch 未安装!
    goto :end
)

echo.
echo [5] 检查项目目录...
cd /d "D:\Wang Yuhan\Desktop\Project\github_project\my_game"
echo   当前目录: %CD%

echo.
echo [6] 运行测试...
python -m pytest tests/ -q
if errorlevel 1 (
    echo   测试失败!
) else (
    echo   测试通过!
)

:end
echo.
echo === 诊断结束，请截图发给我 ===
pause
