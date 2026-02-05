# Multi-User Chat Test Script (Robust File-Based)
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

# 1. Register Alice
$aliceFile = "alice_$ts.json"
$bodyAlice = @{
    username = "alice$ts"
    email = "alice$ts@chat.com"
    password = "Test123!"
    confirmPassword = "Test123!"
    displayName = "Alice"
} | ConvertTo-Json
$bodyAlice | Set-Content $aliceFile

$cmd = "curl.exe -s -X POST $BaseUrl/auth/register -H ""Content-Type: application/json"" -d @$aliceFile"
$res = Exec-Curl $cmd "Register Alice"

$tokenA = ""
$idA = 0
if ($res -match '"accessToken":"([^"]+)"') { $tokenA = $matches[1] }
if ($res -match '"userId":(\d+)') { $idA = $matches[1] }
Write-Host "    Alice ID: $idA" -ForegroundColor Gray

# 2. Register Bob
$bobFile = "bob_$ts.json"
$bodyBob = @{
    username = "bob$ts"
    email = "bob$ts@chat.com"
    password = "Test123!"
    confirmPassword = "Test123!"
    displayName = "Bob"
} | ConvertTo-Json
$bodyBob | Set-Content $bobFile

$cmd = "curl.exe -s -X POST $BaseUrl/auth/register -H ""Content-Type: application/json"" -d @$bobFile"
$res = Exec-Curl $cmd "Register Bob"

$tokenB = ""
$idB = 0
if ($res -match '"accessToken":"([^"]+)"') { $tokenB = $matches[1] }
if ($res -match '"userId":(\d+)') { $idB = $matches[1] }
Write-Host "    Bob ID: $idB" -ForegroundColor Gray

if (-not $tokenA -or -not $tokenB) { 
    Write-Host "Failed to register users" -ForegroundColor Red
    # Cleanup
    Remove-Item $aliceFile -ErrorAction SilentlyContinue
    Remove-Item $bobFile -ErrorAction SilentlyContinue
    exit 
}

# 3. Alice Starts Conversation with Bob
Write-Host "`n--- Conversation Flow ---" -ForegroundColor Cyan
$convFile = "conv_$ts.json"
$bodyStart = @{
    recipientId = [int]$idB
    initialMessage = "Hello Bob, this is Alice."
} | ConvertTo-Json
$bodyStart | Set-Content $convFile

$cmd = "curl.exe -s -X POST $BaseUrl/chat/conversations -H ""Content-Type: application/json"" -H ""Authorization: Bearer $tokenA"" -d @$convFile"
$res = Exec-Curl $cmd "Alice starts conversation"

$convId = 0
if ($res -match '"conversationId":(\d+)') { 
    $convId = $matches[1] 
    Write-Host "    ConversationID: $convId" -ForegroundColor Yellow
}

# 4. Bob checks his conversations
$cmd = "curl.exe -s -X GET $BaseUrl/chat/conversations -H ""Authorization: Bearer $tokenB"""
$res = Exec-Curl $cmd "Bob gets inbox"
if ($res -match $convId) { 
    Write-Host "    Bob sees conversation $convId" -ForegroundColor Green 
} else {
    Write-Host "    Bob CANNOT see conversation $convId" -ForegroundColor Red
}

# 5. Bob replies
if ($convId -gt 0) {
    $msgFile = "msg_$ts.json"
    $bodyReply = @{
        content = "Hi Alice! Nice to meet you."
    } | ConvertTo-Json
    $bodyReply | Set-Content $msgFile

    $cmd = "curl.exe -s -X POST $BaseUrl/chat/conversations/$convId/messages -H ""Content-Type: application/json"" -H ""Authorization: Bearer $tokenB"" -d @$msgFile"
    $res = Exec-Curl $cmd "Bob replies"
    
    Remove-Item $msgFile -ErrorAction SilentlyContinue
}

# 6. Alice checks messages
if ($convId -gt 0) {
    $cmd = "curl.exe -s -X GET $BaseUrl/chat/conversations/$convId/messages -H ""Authorization: Bearer $tokenA"""
    $res = Exec-Curl $cmd "Alice reads messages"
    
    if ($res -match "Hi Alice") {
        Write-Host "    Alice received: 'Hi Alice! Nice to meet you.'" -ForegroundColor Green
    } else {
        Write-Host "    Alice did NOT receive reply." -ForegroundColor Red
    }
    
    # Mark as read
    $cmd = "curl.exe -s -X PUT $BaseUrl/chat/conversations/$convId/read -H ""Authorization: Bearer $tokenA"""
    Exec-Curl $cmd "Alice marks as read" | Out-Null
}

# Cleanup
Remove-Item $aliceFile -ErrorAction SilentlyContinue
Remove-Item $bobFile -ErrorAction SilentlyContinue
Remove-Item $convFile -ErrorAction SilentlyContinue

Write-Host "`n✅ Chat Test Completed" -ForegroundColor Cyan
