# Final Comprehensive API Test
param([string]$BaseUrl = "http://127.0.0.1:5122/api")

$ErrorActionPreference = "Stop"
Import-Module Microsoft.PowerShell.Utility

function Test-Step {
    param($Name, $Action)
    Write-Host "`n[$Name]..." -NoNewline
    try {
        $result = & $Action
        Write-Host " PASS" -ForegroundColor Green
        return $result
    } catch {
        Write-Host " FAIL" -ForegroundColor Red
        Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Yellow
        if ($_.Exception.Response) {
             $reader = New-Object System.IO.StreamReader $_.Exception.Response.GetResponseStream()
             $respBody = $reader.ReadToEnd()
             Write-Host "    Response: $respBody" -ForegroundColor DarkGray
        }
        return $null
    }
}

$ts = Get-Date -Format "HHmmss"
$bodyLong = "This is a meaningful text body that is definitely longer than thirty characters to meet the validation requirements of the Application commands."

# 1. Register
$token = Test-Step "Register" {
    $body = @{
        username = "user$ts"
        email = "user$ts@test.com"
        password = "Test123!"
        confirmPassword = "Test123!"
        displayName = "User $ts"
    } | ConvertTo-Json
    
    $r = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $body -ContentType "application/json"
    return $r.accessToken
}

if (-not $token) { exit }
$headers = @{ Authorization = "Bearer $token" }

# 2. Get User
Test-Step "Get Me" {
    $r = Invoke-RestMethod -Uri "$BaseUrl/auth/me" -Method Get -Headers $headers
    Write-Host "    User: $($r.username)" -ForegroundColor Gray
    return $r
}

# 3. Create Question
$qId = Test-Step "Create Question" {
    $body = @{
        title = "Question Title $ts"
        body = $bodyLong
        tags = @("api", "test")
    } | ConvertTo-Json
    
    $r = Invoke-RestMethod -Uri "$BaseUrl/questions" -Method Post -Body $body -ContentType "application/json" -Headers $headers
    Write-Host "    ID: $($r.questionId)" -ForegroundColor Gray
    return $r.questionId
}

# 4. Create Answer
if ($qId) {
    $aId = Test-Step "Create Answer" {
        $body = @{
            questionId = $qId
            body = "Answer: $bodyLong"
        } | ConvertTo-Json
        
        $r = Invoke-RestMethod -Uri "$BaseUrl/answers" -Method Post -Body $body -ContentType "application/json" -Headers $headers
        Write-Host "    ID: $($r.answerId)" -ForegroundColor Gray
        return $r.answerId
    }
    
    # 5. Vote Question
    Test-Step "Vote Question" {
        $body = @{ voteType = "up" } | ConvertTo-Json
        Invoke-RestMethod -Uri "$BaseUrl/votes/question/$qId" -Method Post -Body $body -ContentType "application/json" -Headers $headers
    }
    
    # 6. Comment on Question
    Test-Step "Comment Question" {
        $body = @{ body = "This is a comment test." } | ConvertTo-Json
        Invoke-RestMethod -Uri "$BaseUrl/comments/question/$qId" -Method Post -Body $body -ContentType "application/json" -Headers $headers
    }
}

# 7. Get Users List
Test-Step "Get Users" {
    $r = Invoke-RestMethod -Uri "$BaseUrl/users" -Method Get
    Write-Host "    Count: $($r.totalCount)" -ForegroundColor Gray
}

# 8. Follow User (target ID 1 - admin usually)
Test-Step "Follow User 1" {
    Invoke-RestMethod -Uri "$BaseUrl/follow/1" -Method Post -Headers $headers
}

# 9. Get Feed
Test-Step "Get Newsfeed" {
    Invoke-RestMethod -Uri "$BaseUrl/newsfeed" -Method Get -Headers $headers
}

Write-Host "`n✅ All Tests Completed" -ForegroundColor Cyan
