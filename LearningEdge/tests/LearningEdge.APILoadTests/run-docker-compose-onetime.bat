@echo off
setlocal enabledelayedexpansion

rem Extract date parts (assuming locale is yyyy-mm-dd or similar)
set dt=%date%
set tm=%time%

rem Remove spaces and colons from time
set tm=%tm: =0%
set tm=%tm::=%

rem Build timestamp: YYYYMMDD_HHMMSS
set datetime=%dt:~10,4%%dt:~4,2%%dt:~7,2%_%tm:~0,2%%tm:~2,2%%tm:~4,2%
echo Timestamp: %datetime%

docker compose run k6 run /scripts/users_api_test.js --out csv=/scripts/results/results_%datetime%.csv
rem docker compose run k6 run /scripts/users_api_test.js --out json=/scripts/results/results_%datetime%.json

pause