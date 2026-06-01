@echo off
D:\anaconda3\condabin\conda.bat activate abyss-walker
python -m ai.training.train_agent --episodes 500 --grid-size 10 --save-every 50
pause
