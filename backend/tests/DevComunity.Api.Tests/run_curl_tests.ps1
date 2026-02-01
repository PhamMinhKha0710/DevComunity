# Robust API Test using curl.exe
$BaseUrl = "http://localhost:5122/api"

function Exec-Curl($cmd, $desc) {
    Write-Host "`n[$desc]..." -NoNewline
    $output = cmd /c $cmd
    if ($LASTEXITCODE -eq 0) {
        Write-Host " PASS" -ForegroundColor Green
        return $output -join ""
    } else {
        Write-Host " FAIL" -ForegroundColor Red
        return $null
    }
}

$ts = Get-Date -Format "HHmmss"

# 1. Register
$body = '{\"username\": \"curluser' + $ts + '\", \"email\": \"curl' + $ts + '@test.com\", \"password\": \"Test123!\", \"confirmPassword\": \"Test123!\", \"displayName\": \"Curl User\"}'
$cmd = "curl.exe -s -X POST $BaseUrl/auth/register -H ""Content-Type: application/json"" -d ""$body"""
$res = Exec-Curl $cmd "Register"

if ($res -match '"accessToken":"([^"]+)"') {
    $token = $matches[1]
    Write-Host "    Token captured." -ForegroundColor Gray
} else {
    Write-Host "    Failed to get token." -ForegroundColor Red
    exit
}

# 2. Get Me
$cmd = "curl.exe -s -X GET $BaseUrl/auth/me -H ""Authorization: Bearer $token"""
$res = Exec-Curl $cmd "Get Me"
if ($res -match '"username":"([^"]+)"') {
    Write-Host "    User: $($matches[1])" -ForegroundColor Gray
}

# 3. Create Question
$bodyLong = "This is a detailed question body that satisfies the 30 char limit requirement for testing API endpoints."
$body = '{\"title\": \"Curl Question ' + $ts + '\", \"body\": \"' + $bodyLong + '\", \"tags\": [\"curl\", \"test\"]}'
$cmd = "curl.exe -s -X POST $BaseUrl/questions -H ""Content-Type: application/json"" -H ""Authorization: Bearer $token"" -d ""$body"""
$res = Exec-Curl $cmd "Create Question"

$qId = 0
if ($res -match '"questionId":(\d+)') {
    $qId = $matches[1]
    Write-Host "    QuestionID: $qId" -ForegroundColor Gray
} else {
    Write-Host "    Failed to get QuestionID. Response: $res" -ForegroundColor Red
}

# 4. Create Answer
if ($qId -gt 0) {
    $body = '{\"questionId\": ' + $qId + ', \"body\": \"Answer: ' + $bodyLong + '\"}'
    $cmd = "curl.exe -s -X POST $BaseUrl/answers -H ""Content-Type: application/json"" -H ""Authorization: Bearer $token"" -d ""$body"""
    $res = Exec-Curl $cmd "Create Answer"
    
    if ($res -match '"answerId":(\d+)') {
        Write-Host "    AnswerID: $($matches[1])" -ForegroundColor Gray
    }
    
    # 5. Vote
    $body = '{\"voteType\": \"up\"}'
    $cmd = "curl.exe -s -X POST $BaseUrl/votes/question/' + $qId + ' -H ""Content-Type: application/json"" -H ""Authorization: Bearer $token"" -d ""$body"""
    Exec-Curl $cmd "Vote Question" | Out-Null
}

# 6. Social - Follow
$cmd = "curl.exe -s -X POST $BaseUrl/follow/1 -H ""Authorization: Bearer $token"""
Exec-Curl $cmd "Follow User 1" | Out-Null

# 7. Get Users
$cmd = "curl.exe -s -X GET $BaseUrl/users"
$res = Exec-Curl $cmd "Get Users List"
if ($res -match '"totalCount":(\d+)') {
    Write-Host "    Total Users: $($matches[1])" -ForegroundColor Gray
}

Write-Host "`n✅ Test Suite Completed" -ForegroundColor Cyan
