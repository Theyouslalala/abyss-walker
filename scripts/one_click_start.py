"""一键启动脚本 - 自动检测环境、安装依赖、运行测试、启动AI服务"""

import subprocess
import sys
import os
import shutil


def run(cmd, check=True):
    print(f"  > {cmd}")
    result = subprocess.run(cmd, shell=True, capture_output=True, text=True)
    if result.stdout:
        print(result.stdout.strip())
    if check and result.returncode != 0:
        print(f"  [错误] {result.stderr.strip()}")
        return False
    return True


def main():
    print("=" * 50)
    print("  Abyss Walker / 深渊行者 - 一键启动")
    print("=" * 50)
    print()

    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    os.chdir(project_root)

    # Step 1: 检查 conda
    print("[1/5] 检查 Conda 环境...")
    conda = shutil.which("conda")
    if not conda:
        # 尝试常见路径
        for path in [r"D:\anaconda3\condabin\conda.bat",
                     r"C:\Users\{}\anaconda3\condabin\conda.bat".format(os.getenv("USERNAME")),
                     r"C:\Users\{}\miniconda3\condabin\conda.bat".format(os.getenv("USERNAME"))]:
            if os.path.exists(path):
                conda = path
                break

    if not conda:
        print("  [错误] 未找到 Conda，请先安装 Anaconda 或 Miniconda")
        print("  下载地址: https://docs.conda.io/en/latest/miniconda.html")
        input("按回车键退出...")
        return

    print(f"  找到 Conda: {conda}")

    # Step 2: 检查/创建环境
    print("\n[2/5] 检查 abyss-walker 环境...")
    result = subprocess.run(f'"{conda}" env list', shell=True, capture_output=True, text=True)
    if "abyss-walker" not in result.stdout:
        print("  创建 abyss-walker 环境...")
        run(f'"{conda}" create -n abyss-walker python=3.10 -y')
    else:
        print("  环境已存在")

    # Step 3: 安装依赖
    print("\n[3/5] 检查 Python 依赖...")
    python_path = get_conda_python(conda)
    result = subprocess.run(f'"{python_path}" -c "import torch; import gymnasium"', shell=True, capture_output=True)
    if result.returncode != 0:
        print("  安装依赖...")
        run(f'"{python_path}" -m pip install torch --index-url https://download.pytorch.org/whl/cpu')
        run(f'"{python_path}" -m pip install numpy gymnasium pytest tensorboard -i https://pypi.tuna.tsinghua.edu.cn/simple')
    else:
        print("  依赖已安装")

    # Step 4: 运行测试
    print("\n[4/5] 运行测试...")
    result = subprocess.run(f'"{python_path}" -m pytest tests/ -q', shell=True, capture_output=True, text=True, cwd=project_root)
    print(result.stdout.strip())
    if "passed" in result.stdout:
        print("  测试全部通过!")
    else:
        print("  [警告] 部分测试失败，请检查")

    # Step 5: 启动 AI 服务
    print("\n[5/5] 启动 AI 服务...")
    print(f"  服务地址: 127.0.0.1:9999")
    print(f"  在 Unity 中点击 Play 即可连接")
    print()
    print("  按 Ctrl+C 停止服务")
    print("=" * 50)

    subprocess.run(f'"{python_path}" -m ai.server.socket_server', shell=True, cwd=project_root)


def get_conda_python(conda_path):
    """获取 conda 环境中的 Python 路径"""
    # 从 conda.bat 路径推断
    base = os.path.dirname(os.path.dirname(conda_path))
    python_path = os.path.join(base, "envs", "abyss-walker", "python.exe")
    if os.path.exists(python_path):
        return python_path
    # 备用路径
    for base_dir in [r"C:\Users\{}\miniconda3".format(os.getenv("USERNAME")),
                     r"C:\Users\{}\anaconda3".format(os.getenv("USERNAME"))]:
        p = os.path.join(base_dir, "envs", "abyss-walker", "python.exe")
        if os.path.exists(p):
            return p
    return "python"


if __name__ == "__main__":
    main()
