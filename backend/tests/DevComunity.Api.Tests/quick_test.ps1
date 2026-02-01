# Quick API Test Script - Simplified
param([string]$BaseUrl = "http://localhost:5122/api")

$results = @()

function Test-API {
    param([string]$Name, [string]$Method, [string]$Url, [object]$Body, [string]$Token, [int]$Expected)
    
    try {
        $params = @{
            Method = $Method
            Uri = "$BaseUrl$Url"
            ContentType = "application/json"
            ErrorAction = "Stop"
        }
        if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 10) }
        if ($Token) { $params.Headers = @{ Authorization = "Bearer $Token" } }
        
        $r = Invoke-WebRequest @params
        $status = [int]$r.StatusCode
        $pass = ($status -eq $Expected)
        $data = try { $r.Content | ConvertFrom-Json } catch { $null }
    }
    catch {
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        $pass = ($status -eq $Expected)
        $data = $null
    }
    
    $result = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "$result | $Name | Expected: $Expected | Got: $status"
    return @{ Pass = $pass; Status = $status; Data = $data }
}

Write-Host "`n=== AUTH TESTS ===" 
$ts = Get-Date -Format "HHmmss"

# Register
$r = Test-API -Name "Register" -Method "POST" -Url "/auth/register" -Body @{
    username = "test$ts"
    email = "test$ts@test.com"
    password = "Test123!"
    confirmPassword = "Test123!"
    displayName = "Test $ts"
} -Expected 200

$token = $r.Data.accessToken

# Login
$r = Test-API -Name "Login" -Method "POST" -Url "/auth/login" -Body @{
    email = "test$ts@test.com"
    password = "Test123!"
} -Expected 200

if ($r.Data.accessToken) { $token = $r.Data.accessToken }

# Login Wrong
Test-API -Name "Login Wrong Pass" -Method "POST" -Url "/auth/login" -Body @{
    email = "test$ts@test.com"
    password = "wrong"
} -Expected 401

# Get Me - No Auth
Test-API -Name "GetMe No Auth" -Method "GET" -Url "/auth/me" -Expected 401

# Get Me - Auth
Test-API -Name "GetMe With Auth" -Method "GET" -Url "/auth/me" -Token $token -Expected 200

Write-Host "`n=== QUESTIONS TESTS ==="

# Get Questions
Test-API -Name "Get Questions" -Method "GET" -Url "/questions" -Expected 200

# Create Question
$r = Test-API -Name "Create Question" -Method "POST" -Url "/questions" -Token $token -Body @{
    title = "Test Question $ts"
    body = "This is a test question body"
    tags = @("test")
} -Expected 201

$qId = $r.Data.questionId
Write-Host "Created Question ID: $qId"

# Get Question
if ($qId) {
    Test-API -Name "Get Question $qId" -Method "GET" -Url "/questions/$qId" -Expected 200
}

# Get Non-existent
Test-API -Name "Get NonExistent Q" -Method "GET" -Url "/questions/99999" -Expected 404

Write-Host "`n=== ANSWERS TESTS ==="

# Create Answer
if ($qId) {
    $r = Test-API -Name "Create Answer" -Method "POST" -Url "/answers" -Token $token -Body @{
        questionId = $qId
        body = "This is a test answer"
    } -Expected 201
    $aId = $r.Data.answerId
    Write-Host "Created Answer ID: $aId"
    
    # Get Answers
    Test-API -Name "Get Answers" -Method "GET" -Url "/answers/question/$qId" -Expected 200
}

Write-Host "`n=== VOTES TESTS ==="

if ($qId) {
    Test-API -Name "Upvote Question" -Method "POST" -Url "/votes/question/$qId" -Token $token -Body @{ voteType = "up" } -Expected 200
}

Write-Host "`n=== USERS TESTS ==="
Test-API -Name "Get Users" -Method "GET" -Url "/users" -Expected 200
Test-API -Name "Get User 1" -Method "GET" -Url "/users/1" -Expected 200
Test-API -Name "Get User 99999" -Method "GET" -Url "/users/99999" -Expected 404

Write-Host "`n=== TAGS TESTS ==="
Test-API -Name "Get Tags" -Method "GET" -Url "/tags" -Expected 200

Write-Host "`n=== FOLLOW TESTS ==="
Test-API -Name "Get Followers" -Method "GET" -Url "/follow/followers/1" -Token $token -Expected 200
Test-API -Name "Get Follow Stats" -Method "GET" -Url "/follow/stats/1" -Expected 200

Write-Host "`n=== COMPLETE ==="
