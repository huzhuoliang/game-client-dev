@echo off
cd /d %~dp0
scp server:/home/huzhuoliang/go-workspace/game-server/proto_csharp/* .
pause