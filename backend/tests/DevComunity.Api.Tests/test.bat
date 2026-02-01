@echo off
set "BASE_URL=http://localhost:5122/api"
set "TS=%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%"
set "TS=%TS: =0%"

echo [1] Registering User...
curl -s -X POST "%BASE_URL%/auth/register" -H "Content-Type: application/json" -d "{\"username\": \"batuser%TS%\", \"email\": \"bat%TS%@test.com\", \"password\": \"Test123!\", \"confirmPassword\": \"Test123!\", \"displayName\": \"Bat User\"}" > register.json
type register.json
echo.

echo [2] Login...
curl -s -X POST "%BASE_URL%/auth/login" -H "Content-Type: application/json" -d "{\"email\": \"bat%TS%@test.com\", \"password\": \"Test123!\"}" > login.json
type login.json
echo.

REM Extract token (simple approximation)
for /f "tokens=4 delims=:," %%a in ('type login.json ^| find "accessToken"') do set "TOKEN=%%~a"
set TOKEN=%TOKEN:"=%
REM Remove potential leading/trailing spaces
set TOKEN=%TOKEN: =%

echo Token: %TOKEN:~0,10%...
echo.

echo [3] Create Question...
curl -s -X POST "%BASE_URL%/questions" -H "Content-Type: application/json" -H "Authorization: Bearer %TOKEN%" -d "{\"title\": \"Bat Question %TS%\", \"body\": \"This is a detailed question body that satisfies the 30 char limit requirement.\", \"tags\": [\"bat\", \"test\"]}" > question.json
type question.json
echo.

echo [4] Get Users...
curl -s -X GET "%BASE_URL%/users"
echo.

echo Done.
