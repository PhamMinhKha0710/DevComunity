# DevComunity API Test Script - Comprehensive Testing
# This script tests all API endpoints systematically

$BaseUrl = "http://localhost:5164/api"
$TestResults = @()
$PassCount = 0
$FailCount = 0

# Color output functions
function Write-TestPass { param($msg) Write-Host "  [PASS] $msg" -ForegroundColor Green }
function Write-TestFail { param($msg) Write-Host "  [FAIL] $msg" -ForegroundColor Red }
function Write-TestSection { param($msg) Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-TestSubsection { param($msg) Write-Host "`n--- $msg ---" -ForegroundColor Yellow }

# Test helper function
function Test-API {
    param(
        [string]$TestId,
        [string]$Description,
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$Token = "",
        [int]$ExpectedStatus = 200
    )
    
    $uri = "$BaseUrl$Endpoint"
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    
    try {
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $headers
            ErrorAction = "Stop"
        }
        if ($Body) { $params["Body"] = ($Body | ConvertTo-Json) }
        
        $response = Invoke-WebRequest @params
        $actualStatus = $response.StatusCode
        
        if ($actualStatus -eq $ExpectedStatus) {
            Write-TestPass "$TestId - $Description (Status: $actualStatus)"
            $script:PassCount++
            return @{ Success = $true; Status = $actualStatus; Body = ($response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue) }
        } else {
            Write-TestFail "$TestId - $Description (Expected: $ExpectedStatus, Got: $actualStatus)"
            $script:FailCount++
            return @{ Success = $false; Status = $actualStatus }
        }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        
        if ($statusCode -eq $ExpectedStatus) {
            Write-TestPass "$TestId - $Description (Status: $statusCode)"
            $script:PassCount++
            return @{ Success = $true; Status = $statusCode }
        } else {
            Write-TestFail "$TestId - $Description (Expected: $ExpectedStatus, Got: $statusCode) - $($_.Exception.Message)"
            $script:FailCount++
            return @{ Success = $false; Status = $statusCode; Error = $_.Exception.Message }
        }
    }
}

# ============================================
# TEST EXECUTION STARTS HERE
# ============================================

Write-Host "`n" -NoNewline
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "     DevComunity API Comprehensive Test Suite               " -ForegroundColor Magenta
Write-Host "     Testing 17 Controllers with 60+ Test Cases             " -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta

$startTime = Get-Date

# Global test data
$testUser1 = @{
    Username = "testuser_$(Get-Random -Maximum 9999)"
    Email = "test$(Get-Random -Maximum 9999)@example.com"
    Password = "Test@123456"
    DisplayName = "Test User 1"
}
$testUser2 = @{
    Username = "testuser_$(Get-Random -Maximum 9999)"
    Email = "test$(Get-Random -Maximum 9999)@example.com"
    Password = "Test@123456"
    DisplayName = "Test User 2"
}
$token1 = ""
$token2 = ""
$createdQuestionId = 0
$createdAnswerId = 0

# ============================================
# PHASE 1: AUTH CONTROLLER TESTS
# ============================================
Write-TestSection "PHASE 1: Auth Controller Tests"

Write-TestSubsection "Register Tests"

# TC-AUTH-001: Register with valid data
$result = Test-API -TestId "TC-AUTH-001" -Description "Register with valid data" `
    -Method "POST" -Endpoint "/auth/register" `
    -Body $testUser1 -ExpectedStatus 200
if ($result.Success -and $result.Body.accessToken) { 
    $token1 = $result.Body.accessToken 
    Write-Host "    -> Got token for user 1" -ForegroundColor Gray
}

# TC-AUTH-002: Register with existing email
Test-API -TestId "TC-AUTH-002" -Description "Register with existing email" `
    -Method "POST" -Endpoint "/auth/register" `
    -Body $testUser1 -ExpectedStatus 400

# TC-AUTH-003: Register second test user
$result = Test-API -TestId "TC-AUTH-003" -Description "Register second test user" `
    -Method "POST" -Endpoint "/auth/register" `
    -Body $testUser2 -ExpectedStatus 200
if ($result.Success -and $result.Body.accessToken) { 
    $token2 = $result.Body.accessToken 
    Write-Host "    -> Got token for user 2" -ForegroundColor Gray
}

# TC-AUTH-004: Register with invalid email format
Test-API -TestId "TC-AUTH-004" -Description "Register with invalid email" `
    -Method "POST" -Endpoint "/auth/register" `
    -Body @{ Username = "test"; Email = "invalid-email"; Password = "Test@123"; DisplayName = "Test" } `
    -ExpectedStatus 400

# TC-AUTH-005: Register with short password
Test-API -TestId "TC-AUTH-005" -Description "Register with short password" `
    -Method "POST" -Endpoint "/auth/register" `
    -Body @{ Username = "test2"; Email = "test2@test.com"; Password = "123"; DisplayName = "Test" } `
    -ExpectedStatus 400

# TC-AUTH-006: Register with empty body
Test-API -TestId "TC-AUTH-006" -Description "Register with empty body" `
    -Method "POST" -Endpoint "/auth/register" `
    -Body @{} -ExpectedStatus 400

Write-TestSubsection "Login Tests"

# TC-AUTH-008: Login with valid credentials
$result = Test-API -TestId "TC-AUTH-008" -Description "Login with valid credentials" `
    -Method "POST" -Endpoint "/auth/login" `
    -Body @{ Email = $testUser1.Email; Password = $testUser1.Password } `
    -ExpectedStatus 200
if ($result.Success -and $result.Body.accessToken) { 
    $token1 = $result.Body.accessToken 
}

# TC-AUTH-009: Login with wrong email
Test-API -TestId "TC-AUTH-009" -Description "Login with wrong email" `
    -Method "POST" -Endpoint "/auth/login" `
    -Body @{ Email = "wrong@email.com"; Password = "any" } `
    -ExpectedStatus 401

# TC-AUTH-010: Login with wrong password
Test-API -TestId "TC-AUTH-010" -Description "Login with wrong password" `
    -Method "POST" -Endpoint "/auth/login" `
    -Body @{ Email = $testUser1.Email; Password = "wrongpass" } `
    -ExpectedStatus 401

Write-TestSubsection "Auth Me Tests"

# TC-AUTH-018: Get current user with valid token
Test-API -TestId "TC-AUTH-018" -Description "Get current user with valid token" `
    -Method "GET" -Endpoint "/auth/me" `
    -Token $token1 -ExpectedStatus 200

# TC-AUTH-019: Get current user without token
Test-API -TestId "TC-AUTH-019" -Description "Get current user without token" `
    -Method "GET" -Endpoint "/auth/me" `
    -ExpectedStatus 401

# ============================================
# PHASE 1: QUESTIONS CONTROLLER TESTS  
# ============================================
Write-TestSection "Questions Controller Tests"

Write-TestSubsection "Get Questions Tests"

# TC-QUES-001: Get questions without params
Test-API -TestId "TC-QUES-001" -Description "Get questions without params" `
    -Method "GET" -Endpoint "/questions" -ExpectedStatus 200

# TC-QUES-002: Get questions with pagination
Test-API -TestId "TC-QUES-002" -Description "Get questions with pagination" `
    -Method "GET" -Endpoint "/questions?page=1" -ExpectedStatus 200

Write-TestSubsection "Create Question Tests"

# TC-QUES-011: Create question with valid data
$result = Test-API -TestId "TC-QUES-011" -Description "Create question with valid data" `
    -Method "POST" -Endpoint "/questions" `
    -Body @{ 
        Title = "Test Question $(Get-Random -Maximum 9999)"
        Body = "This is a test question body with enough content for testing purposes."
        TagNames = @("test", "api")
    } `
    -Token $token1 -ExpectedStatus 201
if ($result.Success -and $result.Body.questionId) {
    $createdQuestionId = $result.Body.questionId
    Write-Host "    -> Created question ID: $createdQuestionId" -ForegroundColor Gray
}

# TC-QUES-012: Create question without token
Test-API -TestId "TC-QUES-012" -Description "Create question without token" `
    -Method "POST" -Endpoint "/questions" `
    -Body @{ Title = "Test"; Body = "Test body" } `
    -ExpectedStatus 401

Write-TestSubsection "Get Question by ID Tests"

# TC-QUES-007: Get question by valid ID
if ($createdQuestionId -gt 0) {
    Test-API -TestId "TC-QUES-007" -Description "Get question by valid ID" `
        -Method "GET" -Endpoint "/questions/$createdQuestionId" -ExpectedStatus 200
}

# TC-QUES-008: Get question by non-existent ID
Test-API -TestId "TC-QUES-008" -Description "Get question by non-existent ID" `
    -Method "GET" -Endpoint "/questions/999999" -ExpectedStatus 404

# ============================================
# PHASE 1: ANSWERS CONTROLLER TESTS
# ============================================
Write-TestSection "Answers Controller Tests"

Write-TestSubsection "Create Answer Tests"

# TC-ANS-006: Create answer with valid data
if ($createdQuestionId -gt 0) {
    $result = Test-API -TestId "TC-ANS-006" -Description "Create answer with valid data" `
        -Method "POST" -Endpoint "/answers" `
        -Body @{ 
            QuestionId = $createdQuestionId
            Body = "This is a test answer body with sufficient content for testing."
        } `
        -Token $token1 -ExpectedStatus 201
    if ($result.Success -and $result.Body.answerId) {
        $createdAnswerId = $result.Body.answerId
        Write-Host "    -> Created answer ID: $createdAnswerId" -ForegroundColor Gray
    }
}

# TC-ANS-007: Create answer for non-existent question
Test-API -TestId "TC-ANS-007" -Description "Create answer for non-existent question" `
    -Method "POST" -Endpoint "/answers" `
    -Body @{ QuestionId = 999999; Body = "Test answer" } `
    -Token $token1 -ExpectedStatus 404

# TC-ANS-008: Create answer without token
Test-API -TestId "TC-ANS-008" -Description "Create answer without token" `
    -Method "POST" -Endpoint "/answers" `
    -Body @{ QuestionId = 1; Body = "Test answer" } `
    -ExpectedStatus 401

Write-TestSubsection "Get Answers Tests"

# TC-ANS-001: Get answers by questionId
if ($createdQuestionId -gt 0) {
    Test-API -TestId "TC-ANS-001" -Description "Get answers by valid questionId" `
        -Method "GET" -Endpoint "/answers/question/$createdQuestionId" -ExpectedStatus 200
}

# TC-ANS-004: Get answer by valid ID
if ($createdAnswerId -gt 0) {
    Test-API -TestId "TC-ANS-004" -Description "Get answer by valid ID" `
        -Method "GET" -Endpoint "/answers/$createdAnswerId" -ExpectedStatus 200
}

# TC-ANS-005: Get answer by non-existent ID
Test-API -TestId "TC-ANS-005" -Description "Get answer by non-existent ID" `
    -Method "GET" -Endpoint "/answers/999999" -ExpectedStatus 404

# ============================================
# PHASE 2: COMMENTS CONTROLLER TESTS
# ============================================
Write-TestSection "PHASE 2: Comments Controller Tests"

Write-TestSubsection "Add Comment Tests"

$createdCommentId = 0

# TC-CMT-001: Add comment to question
if ($createdQuestionId -gt 0) {
    $result = Test-API -TestId "TC-CMT-001" -Description "Add comment to question" `
        -Method "POST" -Endpoint "/comments/question/$createdQuestionId" `
        -Body @{ Body = "This is a test comment on a question." } `
        -Token $token1 -ExpectedStatus 201
    if ($result.Success -and $result.Body.commentId) {
        $createdCommentId = $result.Body.commentId
        Write-Host "    -> Created comment ID: $createdCommentId" -ForegroundColor Gray
    }
}

# TC-CMT-002: Add comment to non-existent question
Test-API -TestId "TC-CMT-002" -Description "Add comment to non-existent question" `
    -Method "POST" -Endpoint "/comments/question/999999" `
    -Body @{ Body = "Test comment" } `
    -Token $token1 -ExpectedStatus 404

# TC-CMT-003: Add comment without token
Test-API -TestId "TC-CMT-003" -Description "Add comment without token" `
    -Method "POST" -Endpoint "/comments/question/1" `
    -Body @{ Body = "Test comment" } `
    -ExpectedStatus 401

# TC-CMT-005: Add comment to answer
if ($createdAnswerId -gt 0) {
    Test-API -TestId "TC-CMT-005" -Description "Add comment to answer" `
        -Method "POST" -Endpoint "/comments/answer/$createdAnswerId" `
        -Body @{ Body = "This is a test comment on an answer." } `
        -Token $token1 -ExpectedStatus 201
}

# ============================================
# PHASE 2: VOTES CONTROLLER TESTS
# ============================================
Write-TestSection "Votes Controller Tests"

Write-TestSubsection "Vote on Question Tests"

# TC-VOTE-001: Upvote question
if ($createdQuestionId -gt 0) {
    Test-API -TestId "TC-VOTE-001" -Description "Upvote question" `
        -Method "POST" -Endpoint "/votes/question/$createdQuestionId" `
        -Body @{ VoteType = "up" } `
        -Token $token2 -ExpectedStatus 200
}

# TC-VOTE-003: Vote question without token
Test-API -TestId "TC-VOTE-003" -Description "Vote question without token" `
    -Method "POST" -Endpoint "/votes/question/1" `
    -Body @{ VoteType = "up" } `
    -ExpectedStatus 401

Write-TestSubsection "Vote on Answer Tests"

# TC-VOTE-007: Upvote answer
if ($createdAnswerId -gt 0) {
    Test-API -TestId "TC-VOTE-007" -Description "Upvote answer" `
        -Method "POST" -Endpoint "/votes/answer/$createdAnswerId" `
        -Body @{ VoteType = "up" } `
        -Token $token2 -ExpectedStatus 200
}

# ============================================
# PHASE 2: USERS CONTROLLER TESTS
# ============================================
Write-TestSection "Users Controller Tests"

# TC-USER-001: Get users list
Test-API -TestId "TC-USER-001" -Description "Get users list" `
    -Method "GET" -Endpoint "/users" -ExpectedStatus 200

# TC-USER-002: Get users with search
Test-API -TestId "TC-USER-002" -Description "Get users with search" `
    -Method "GET" -Endpoint "/users?search=test" -ExpectedStatus 200

# TC-USER-004: Get user by valid ID (assume ID 1 exists)
Test-API -TestId "TC-USER-004" -Description "Get user by ID=1" `
    -Method "GET" -Endpoint "/users/1" -ExpectedStatus 200

# TC-USER-005: Get user by non-existent ID
Test-API -TestId "TC-USER-005" -Description "Get user by non-existent ID" `
    -Method "GET" -Endpoint "/users/999999" -ExpectedStatus 404

# ============================================
# PHASE 3: SOCIAL FEATURES
# ============================================
Write-TestSection "PHASE 3: Social Features"

Write-TestSubsection "Follow Controller Tests"

# TC-FOLLOW-001: Get followers
Test-API -TestId "TC-FOLLOW-001" -Description "Get followers of user 1" `
    -Method "GET" -Endpoint "/follow/followers/1" `
    -Token $token1 -ExpectedStatus 200

# TC-FOLLOW-002: Get following
Test-API -TestId "TC-FOLLOW-002" -Description "Get following of user 1" `
    -Method "GET" -Endpoint "/follow/following/1" `
    -Token $token1 -ExpectedStatus 200

# TC-FOLLOW-003: Get follow stats
Test-API -TestId "TC-FOLLOW-003" -Description "Get follow stats" `
    -Method "GET" -Endpoint "/follow/stats/1" `
    -Token $token1 -ExpectedStatus 200

# TC-FOLLOW-004: Follow user (user2 follows user 1)
Test-API -TestId "TC-FOLLOW-004" -Description "Follow user" `
    -Method "POST" -Endpoint "/follow/1" `
    -Token $token2 -ExpectedStatus 200

Write-TestSubsection "Friendship Controller Tests"

# TC-FRIEND-001: Get friends list
Test-API -TestId "TC-FRIEND-001" -Description "Get friends list" `
    -Method "GET" -Endpoint "/friendship/friends" `
    -Token $token1 -ExpectedStatus 200

# TC-FRIEND-002: Get pending requests
Test-API -TestId "TC-FRIEND-002" -Description "Get pending friend requests" `
    -Method "GET" -Endpoint "/friendship/pending" `
    -Token $token1 -ExpectedStatus 200

# TC-FRIEND-003: Get sent requests
Test-API -TestId "TC-FRIEND-003" -Description "Get sent friend requests" `
    -Method "GET" -Endpoint "/friendship/sent" `
    -Token $token1 -ExpectedStatus 200

Write-TestSubsection "Chat Controller Tests"

# TC-CHAT-001: Get conversations
Test-API -TestId "TC-CHAT-001" -Description "Get conversations" `
    -Method "GET" -Endpoint "/chat/conversations" `
    -Token $token1 -ExpectedStatus 200

# TC-CHAT-002: Get conversations without token
Test-API -TestId "TC-CHAT-002" -Description "Get conversations without token" `
    -Method "GET" -Endpoint "/chat/conversations" `
    -ExpectedStatus 401

# ============================================
# PHASE 4: GROUPS AND CONTENT
# ============================================
Write-TestSection "PHASE 4: Groups and Content"

Write-TestSubsection "Groups Controller Tests"

# TC-GROUP-001: Get groups list
Test-API -TestId "TC-GROUP-001" -Description "Get groups list" `
    -Method "GET" -Endpoint "/groups" -ExpectedStatus 200

# TC-GROUP-003: Get my groups
Test-API -TestId "TC-GROUP-003" -Description "Get my groups" `
    -Method "GET" -Endpoint "/groups/my" `
    -Token $token1 -ExpectedStatus 200

# TC-GROUP-007: Create group
$createdGroupId = 0
$result = Test-API -TestId "TC-GROUP-007" -Description "Create group" `
    -Method "POST" -Endpoint "/groups" `
    -Body @{ Name = "Test Group $(Get-Random -Maximum 9999)"; Description = "Test group description" } `
    -Token $token1 -ExpectedStatus 201
if ($result.Success -and $result.Body.groupId) {
    $createdGroupId = $result.Body.groupId
    Write-Host "    -> Created group ID: $createdGroupId" -ForegroundColor Gray
}

Write-TestSubsection "Newsfeed Controller Tests"

# TC-FEED-001: Get personalized feed
Test-API -TestId "TC-FEED-001" -Description "Get personalized newsfeed" `
    -Method "GET" -Endpoint "/newsfeed" `
    -Token $token1 -ExpectedStatus 200

# TC-FEED-004: Create post
$result = Test-API -TestId "TC-FEED-004" -Description "Create post" `
    -Method "POST" -Endpoint "/newsfeed" `
    -Body @{ Content = "This is a test post content." } `
    -Token $token1 -ExpectedStatus 201

Write-TestSubsection "Notifications Controller Tests"

# TC-NOTI-001: Get notifications
Test-API -TestId "TC-NOTI-001" -Description "Get notifications" `
    -Method "GET" -Endpoint "/notifications" `
    -Token $token1 -ExpectedStatus 200

# TC-NOTI-003: Get unread count
Test-API -TestId "TC-NOTI-003" -Description "Get unread notification count" `
    -Method "GET" -Endpoint "/notifications/unread-count" `
    -Token $token1 -ExpectedStatus 200

# ============================================
# PHASE 5: SUPPORTING FEATURES
# ============================================
Write-TestSection "PHASE 5: Supporting Features"

Write-TestSubsection "Badges Controller Tests"

# TC-BADGE-001: Get all badges
Test-API -TestId "TC-BADGE-001" -Description "Get all badges" `
    -Method "GET" -Endpoint "/badges" -ExpectedStatus 200

# TC-BADGE-003: Get non-existent badge
Test-API -TestId "TC-BADGE-003" -Description "Get non-existent badge" `
    -Method "GET" -Endpoint "/badges/999999" -ExpectedStatus 404

Write-TestSubsection "Tags Controller Tests"

# TC-TAG-001: Get all tags
Test-API -TestId "TC-TAG-001" -Description "Get all tags" `
    -Method "GET" -Endpoint "/tags" -ExpectedStatus 200

# TC-TAG-002: Get tags with search
Test-API -TestId "TC-TAG-002" -Description "Get tags with search" `
    -Method "GET" -Endpoint "/tags?search=test" -ExpectedStatus 200

Write-TestSubsection "SavedItems Controller Tests"

# TC-SAVE-001: Get saved items
Test-API -TestId "TC-SAVE-001" -Description "Get saved items" `
    -Method "GET" -Endpoint "/saveditems" `
    -Token $token1 -ExpectedStatus 200

# TC-SAVE-003: Save question
if ($createdQuestionId -gt 0) {
    Test-API -TestId "TC-SAVE-003" -Description "Save question" `
        -Method "POST" -Endpoint "/saveditems/questions/$createdQuestionId" `
        -Token $token1 -ExpectedStatus 201
}

Write-TestSubsection "Search Controller Tests"

# TC-SEARCH-001: Search with valid query
Test-API -TestId "TC-SEARCH-001" -Description "Search with valid query" `
    -Method "GET" -Endpoint "/search?q=test" -ExpectedStatus 200

# TC-SEARCH-002: Search with empty query
Test-API -TestId "TC-SEARCH-002" -Description "Search with empty query" `
    -Method "GET" -Endpoint "/search?q=" -ExpectedStatus 200

Write-TestSubsection "Repositories Controller Tests"

# TC-REPO-001: Get public repositories
Test-API -TestId "TC-REPO-001" -Description "Get public repositories" `
    -Method "GET" -Endpoint "/repositories" -ExpectedStatus 200

# TC-REPO-003: Get non-existent repository
Test-API -TestId "TC-REPO-003" -Description "Get non-existent repository" `
    -Method "GET" -Endpoint "/repositories/999999" -ExpectedStatus 404

# ============================================
# SUMMARY
# ============================================
$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "`n" -NoNewline
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "                    TEST SUMMARY                            " -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "  Total Tests: $($PassCount + $FailCount)" -ForegroundColor White
Write-Host "  Passed: $PassCount" -ForegroundColor Green
Write-Host "  Failed: $FailCount" -ForegroundColor $(if($FailCount -gt 0){"Red"}else{"Green"})
Write-Host "  Duration: $($duration.TotalSeconds.ToString('F2'))s" -ForegroundColor White
Write-Host "  Pass Rate: $([math]::Round(($PassCount/[math]::Max(1,($PassCount+$FailCount)))*100, 1))%" -ForegroundColor White
Write-Host "============================================================" -ForegroundColor Magenta

if ($FailCount -eq 0) {
    Write-Host "`n  All tests passed! API is working correctly." -ForegroundColor Green
} else {
    Write-Host "`n  Some tests failed. Please review the errors above." -ForegroundColor Yellow
}
