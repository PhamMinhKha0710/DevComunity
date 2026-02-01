# ============================================================
# DevComunity API Comprehensive Test Script
# Author: Senior Backend Engineer & QA Automation Specialist
# Date: 2026-01-29
# ============================================================

param(
    [string]$BaseUrl = "http://localhost:5122/api",
    [switch]$Verbose,
    [string]$Phase = "all"
)

$script:TestResults = @()
$script:Token = $null
$script:UserId = 0
$script:QuestionId = 0
$script:AnswerId = 0

# ==================== HELPER FUNCTIONS ====================

function Write-TestHeader {
    param([string]$Title)
    Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
    Write-Host " $Title" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
}

function Test-Endpoint {
    param(
        [string]$TestId,
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus,
        [string]$Category = "General"
    )
    
    $fullUrl = if ($Url.StartsWith("http")) { $Url } else { "$BaseUrl$Url" }
    
    try {
        $params = @{
            Method = $Method
            Uri = $fullUrl
            ContentType = "application/json"
            ErrorAction = "Stop"
        }
        
        if ($Body) { 
            $params.Body = ($Body | ConvertTo-Json -Depth 10) 
        }
        if ($Headers.Count -gt 0) { 
            $params.Headers = $Headers 
        }
        
        $response = Invoke-WebRequest @params
        $actualStatus = [int]$response.StatusCode
        $passed = ($actualStatus -eq $ExpectedStatus)
        $responseContent = try { $response.Content | ConvertFrom-Json } catch { $response.Content }
        
        $result = @{
            TestId = $TestId
            Name = $Name
            Category = $Category
            Passed = $passed
            Expected = $ExpectedStatus
            Actual = $actualStatus
            Response = $responseContent
            Error = $null
        }
    }
    catch {
        $actualStatus = 0
        if ($_.Exception.Response) {
            $actualStatus = [int]$_.Exception.Response.StatusCode
        }
        $passed = ($actualStatus -eq $ExpectedStatus)
        
        $result = @{
            TestId = $TestId
            Name = $Name
            Category = $Category
            Passed = $passed
            Expected = $ExpectedStatus
            Actual = $actualStatus
            Response = $null
            Error = $_.Exception.Message
        }
    }
    
    # Display result
    $status = if ($result.Passed) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($result.Passed) { "Green" } else { "Red" }
    Write-Host "  $status [$TestId] $Name" -ForegroundColor $color
    
    if ($Verbose -or -not $result.Passed) {
        Write-Host "         Expected: $($result.Expected) | Actual: $($result.Actual)" -ForegroundColor Gray
        if ($result.Error) {
            Write-Host "         Error: $($result.Error)" -ForegroundColor DarkYellow
        }
    }
    
    $script:TestResults += $result
    return $result
}

function Get-AuthHeaders {
    if ($script:Token) {
        return @{ Authorization = "Bearer $script:Token" }
    }
    return @{}
}

# ==================== PHASE 1: AUTHENTICATION ====================

function Test-AuthAPIs {
    Write-TestHeader "PHASE 1: AUTHENTICATION APIs"
    
    $timestamp = Get-Date -Format "HHmmss"
    $testEmail = "testuser$timestamp@example.com"
    $testPassword = "SecurePass123!"
    
    # A01: Register - Valid
    $result = Test-Endpoint -TestId "A01" `
        -Name "Register with valid data" `
        -Method "POST" `
        -Url "/auth/register" `
        -Body @{
            username = "testuser$timestamp"
            email = $testEmail
            password = $testPassword
            displayName = "Test User $timestamp"
        } `
        -ExpectedStatus 200 `
        -Category "Auth"
    
    if ($result.Response.accessToken) {
        $script:Token = $result.Response.accessToken
        $script:UserId = $result.Response.user.userId
        Write-Host "         → Token acquired, UserId: $($script:UserId)" -ForegroundColor DarkGreen
    }
    
    # A02: Register - Missing Email
    Test-Endpoint -TestId "A02" `
        -Name "Register without email" `
        -Method "POST" `
        -Url "/auth/register" `
        -Body @{
            username = "nomail$timestamp"
            password = "test123"
        } `
        -ExpectedStatus 400 `
        -Category "Auth"
    
    # A03: Register - Invalid Email Format
    Test-Endpoint -TestId "A03" `
        -Name "Register with invalid email" `
        -Method "POST" `
        -Url "/auth/register" `
        -Body @{
            username = "badmail$timestamp"
            email = "notanemail"
            password = "test123"
        } `
        -ExpectedStatus 400 `
        -Category "Auth"
    
    # A04: Register - Duplicate Email
    Test-Endpoint -TestId "A04" `
        -Name "Register with duplicate email" `
        -Method "POST" `
        -Url "/auth/register" `
        -Body @{
            username = "duplicate$timestamp"
            email = $testEmail
            password = "test123"
        } `
        -ExpectedStatus 400 `
        -Category "Auth"
    
    # A05: Login - Valid
    $result = Test-Endpoint -TestId "A05" `
        -Name "Login with valid credentials" `
        -Method "POST" `
        -Url "/auth/login" `
        -Body @{
            email = $testEmail
            password = $testPassword
        } `
        -ExpectedStatus 200 `
        -Category "Auth"
    
    if ($result.Response.accessToken) {
        $script:Token = $result.Response.accessToken
    }
    
    # A06: Login - Wrong Password
    Test-Endpoint -TestId "A06" `
        -Name "Login with wrong password" `
        -Method "POST" `
        -Url "/auth/login" `
        -Body @{
            email = $testEmail
            password = "wrongpassword"
        } `
        -ExpectedStatus 401 `
        -Category "Auth"
    
    # A07: Login - Non-existent User
    Test-Endpoint -TestId "A07" `
        -Name "Login with non-existent user" `
        -Method "POST" `
        -Url "/auth/login" `
        -Body @{
            email = "nonexistent@example.com"
            password = "test123"
        } `
        -ExpectedStatus 401 `
        -Category "Auth"
    
    # A08: Get Me - Without Token
    Test-Endpoint -TestId "A08" `
        -Name "Get current user without token" `
        -Method "GET" `
        -Url "/auth/me" `
        -ExpectedStatus 401 `
        -Category "Auth"
    
    # A09: Get Me - With Valid Token
    Test-Endpoint -TestId "A09" `
        -Name "Get current user with valid token" `
        -Method "GET" `
        -Url "/auth/me" `
        -Headers (Get-AuthHeaders) `
        -ExpectedStatus 200 `
        -Category "Auth"
    
    # A10: Logout
    Test-Endpoint -TestId "A10" `
        -Name "Logout with valid token" `
        -Method "POST" `
        -Url "/auth/logout" `
        -Headers (Get-AuthHeaders) `
        -ExpectedStatus 200 `
        -Category "Auth"
}

# ==================== PHASE 2: Q&A SYSTEM ====================

function Test-QuestionsAPIs {
    Write-TestHeader "PHASE 2: QUESTIONS APIs"
    
    # Q01: Get Questions List
    Test-Endpoint -TestId "Q01" `
        -Name "Get questions list" `
        -Method "GET" `
        -Url "/questions" `
        -ExpectedStatus 200 `
        -Category "Questions"
    
    # Q02: Get Questions with Pagination
    Test-Endpoint -TestId "Q02" `
        -Name "Get questions with pagination" `
        -Method "GET" `
        -Url "/questions?page=1&pageSize=5" `
        -ExpectedStatus 200 `
        -Category "Questions"
    
    # Q03: Create Question - Without Auth
    Test-Endpoint -TestId "Q03" `
        -Name "Create question without auth" `
        -Method "POST" `
        -Url "/questions" `
        -Body @{
            title = "Test Question"
            body = "Test body"
        } `
        -ExpectedStatus 401 `
        -Category "Questions"
    
    # Q04: Create Question - Valid
    $result = Test-Endpoint -TestId "Q04" `
        -Name "Create question with auth" `
        -Method "POST" `
        -Url "/questions" `
        -Headers (Get-AuthHeaders) `
        -Body @{
            title = "Test Question $(Get-Date -Format 'HHmmss')"
            body = "This is a comprehensive test question body that satisfies the minimum length requirement of 30 characters for API testing."
            tags = @("test", "api", "automation")
        } `
        -ExpectedStatus 201 `
        -Category "Questions"
    
    if ($result.Response.questionId) {
        $script:QuestionId = $result.Response.questionId
        Write-Host "         → Created QuestionId: $($script:QuestionId)" -ForegroundColor DarkGreen
    }
    
    # Q05: Get Question by ID
    if ($script:QuestionId -gt 0) {
        Test-Endpoint -TestId "Q05" `
            -Name "Get question by ID" `
            -Method "GET" `
            -Url "/questions/$($script:QuestionId)" `
            -ExpectedStatus 200 `
            -Category "Questions"
    }
    
    # Q06: Get Non-existent Question
    Test-Endpoint -TestId "Q06" `
        -Name "Get non-existent question" `
        -Method "GET" `
        -Url "/questions/99999" `
        -ExpectedStatus 404 `
        -Category "Questions"
    
    # Q07: Update Question
    if ($script:QuestionId -gt 0) {
        Test-Endpoint -TestId "Q07" `
            -Name "Update own question" `
            -Method "PUT" `
            -Url "/questions/$($script:QuestionId)" `
            -Headers (Get-AuthHeaders) `
            -Body @{
                title = "Updated Question Title"
                body = "Updated question body"
            } `
            -ExpectedStatus 200 `
            -Category "Questions"
    }
}

function Test-AnswersAPIs {
    Write-TestHeader "PHASE 2: ANSWERS APIs"
    
    if ($script:QuestionId -eq 0) {
        Write-Host "  ⚠ Skipping - No question created" -ForegroundColor Yellow
        return
    }
    
    # AN01: Create Answer
    $result = Test-Endpoint -TestId "AN01" `
        -Name "Create answer" `
        -Method "POST" `
        -Url "/answers" `
        -Headers (Get-AuthHeaders) `
        -Body @{
            questionId = $script:QuestionId
            body = "This is a detailed test answer body that satisfies the minimum length requirement of 30 characters."
        } `
        -ExpectedStatus 201 `
        -Category "Answers"
    
    if ($result.Response.answerId) {
        $script:AnswerId = $result.Response.answerId
        Write-Host "         → Created AnswerId: $($script:AnswerId)" -ForegroundColor DarkGreen
    }
    
    # AN02: Get Answers by Question
    Test-Endpoint -TestId "AN02" `
        -Name "Get answers by question" `
        -Method "GET" `
        -Url "/answers/question/$($script:QuestionId)" `
        -ExpectedStatus 200 `
        -Category "Answers"
    
    # AN03: Get Answer by ID
    if ($script:AnswerId -gt 0) {
        Test-Endpoint -TestId "AN03" `
            -Name "Get answer by ID" `
            -Method "GET" `
            -Url "/answers/$($script:AnswerId)" `
            -ExpectedStatus 200 `
            -Category "Answers"
    }
}

function Test-VotesAPIs {
    Write-TestHeader "PHASE 2: VOTES APIs"
    
    if ($script:QuestionId -eq 0) {
        Write-Host "  ⚠ Skipping - No question created" -ForegroundColor Yellow
        return
    }
    
    # V01: Upvote Question
    Test-Endpoint -TestId "V01" `
        -Name "Upvote question" `
        -Method "POST" `
        -Url "/votes/question/$($script:QuestionId)" `
        -Headers (Get-AuthHeaders) `
        -Body @{ voteType = "up" } `
        -ExpectedStatus 200 `
        -Category "Votes"
    
    # V02: Remove Vote
    Test-Endpoint -TestId "V02" `
        -Name "Remove question vote" `
        -Method "DELETE" `
        -Url "/votes/question/$($script:QuestionId)" `
        -Headers (Get-AuthHeaders) `
        -ExpectedStatus 200 `
        -Category "Votes"
    
    if ($script:AnswerId -gt 0) {
        # V03: Vote Answer
        Test-Endpoint -TestId "V03" `
            -Name "Upvote answer" `
            -Method "POST" `
            -Url "/votes/answer/$($script:AnswerId)" `
            -Headers (Get-AuthHeaders) `
            -Body @{ voteType = "up" } `
            -ExpectedStatus 200 `
            -Category "Votes"
    }
}

function Test-CommentsAPIs {
    Write-TestHeader "PHASE 2: COMMENTS APIs"
    
    if ($script:QuestionId -eq 0) {
        Write-Host "  ⚠ Skipping - No question created" -ForegroundColor Yellow
        return
    }
    
    # C01: Add Comment to Question
    $result = Test-Endpoint -TestId "C01" `
        -Name "Add comment to question" `
        -Method "POST" `
        -Url "/comments/question/$($script:QuestionId)" `
        -Headers (Get-AuthHeaders) `
        -Body @{ body = "This is a detailed test comment to satisfy any potential length requirements." } `
        -ExpectedStatus 201 `
        -Category "Comments"
    
    $commentId = $result.Response.commentId
    
    # C02: Update Comment
    if ($commentId) {
        Test-Endpoint -TestId "C02" `
            -Name "Update comment" `
            -Method "PUT" `
            -Url "/comments/$commentId" `
            -Headers (Get-AuthHeaders) `
            -Body @{ body = "Updated comment text." } `
            -ExpectedStatus 200 `
            -Category "Comments"
    }
}

# ==================== PHASE 3: SOCIAL FEATURES ====================

function Test-FollowAPIs {
    Write-TestHeader "PHASE 3: FOLLOW APIs"
    
    # F01: Get Followers
    Test-Endpoint -TestId "F01" `
        -Name "Get followers" `
        -Method "GET" `
        -Url "/follow/followers/1" `
        -ExpectedStatus 200 `
        -Category "Follow"
    
    # F02: Get Following
    Test-Endpoint -TestId "F02" `
        -Name "Get following" `
        -Method "GET" `
        -Url "/follow/following/1" `
        -ExpectedStatus 200 `
        -Category "Follow"
    
    # F03: Get Follow Stats
    Test-Endpoint -TestId "F03" `
        -Name "Get follow stats" `
        -Method "GET" `
        -Url "/follow/stats/1" `
        -ExpectedStatus 200 `
        -Category "Follow"
    
    # F04: Follow Self (should fail)
    if ($script:UserId -gt 0) {
        Test-Endpoint -TestId "F04" `
            -Name "Follow self (should fail)" `
            -Method "POST" `
            -Url "/follow/$($script:UserId)" `
            -Headers (Get-AuthHeaders) `
            -ExpectedStatus 400 `
            -Category "Follow"
    }
}

function Test-UsersAPIs {
    Write-TestHeader "PHASE 3: USERS APIs"
    
    # U01: Get Users List
    Test-Endpoint -TestId "U01" `
        -Name "Get users list" `
        -Method "GET" `
        -Url "/users" `
        -ExpectedStatus 200 `
        -Category "Users"
    
    # U02: Get User by ID
    Test-Endpoint -TestId "U02" `
        -Name "Get user by ID" `
        -Method "GET" `
        -Url "/users/1" `
        -ExpectedStatus 200 `
        -Category "Users"
    
    # U03: Get Non-existent User
    Test-Endpoint -TestId "U03" `
        -Name "Get non-existent user" `
        -Method "GET" `
        -Url "/users/99999" `
        -ExpectedStatus 404 `
        -Category "Users"
}

function Test-TagsAPIs {
    Write-TestHeader "PHASE 3: TAGS APIs"
    
    # T01: Get Tags
    Test-Endpoint -TestId "T01" `
        -Name "Get tags list" `
        -Method "GET" `
        -Url "/tags" `
        -ExpectedStatus 200 `
        -Category "Tags"
    
    # T02: Get Tag by Name
    Test-Endpoint -TestId "T02" `
        -Name "Get tag by name" `
        -Method "GET" `
        -Url "/tags/test" `
        -ExpectedStatus 200 `
        -Category "Tags"
}

# ==================== PHASE 4: SECURITY TESTS ====================

function Test-SecurityTests {
    Write-TestHeader "PHASE 4: SECURITY TESTS"
    
    # S01: SQL Injection in Username
    Test-Endpoint -TestId "S01" `
        -Name "SQL Injection in username" `
        -Method "POST" `
        -Url "/auth/register" `
        -Body @{
            username = "admin'--"
            email = "sqli$(Get-Random)@test.com"
            password = "test123!"
        } `
        -ExpectedStatus 400 `
        -Category "Security"
    
    # S02: SQL Injection in Email
    Test-Endpoint -TestId "S02" `
        -Name "SQL Injection in email" `
        -Method "POST" `
        -Url "/auth/register" `
        -Body @{
            username = "sqlitest$(Get-Random)"
            email = "test@test.com'; DROP TABLE Users;--"
            password = "test123!"
        } `
        -ExpectedStatus 400 `
        -Category "Security"
    
    # S03: XSS in Username
    Test-Endpoint -TestId "S03" `
        -Name "XSS payload in username" `
        -Method "POST" `
        -Url "/auth/register" `
        -Body @{
            username = "<script>alert('xss')</script>"
            email = "xss$(Get-Random)@test.com"
            password = "test123!"
        } `
        -ExpectedStatus 400 `
        -Category "Security"
    
    # S04: Mass Assignment
    Test-Endpoint -TestId "S04" `
        -Name "Mass assignment attempt" `
        -Method "POST" `
        -Url "/auth/register" `
        -Body @{
            username = "massassign$(Get-Random)"
            email = "mass$(Get-Random)@test.com"
            password = "test123!"
            isAdmin = $true
            reputation = 10000
        } `
        -ExpectedStatus 200 `
        -Category "Security"
}

# ==================== RESULTS SUMMARY ====================

function Show-ResultsSummary {
    Write-TestHeader "TEST RESULTS SUMMARY"
    
    $passed = ($script:TestResults | Where-Object { $_.Passed }).Count
    $failed = ($script:TestResults | Where-Object { -not $_.Passed }).Count
    $total = $script:TestResults.Count
    
    # Group by category
    $categories = $script:TestResults | Group-Object Category
    
    foreach ($cat in $categories) {
        $catPassed = ($cat.Group | Where-Object { $_.Passed }).Count
        $catTotal = $cat.Group.Count
        $pct = if ($catTotal -gt 0) { [math]::Round(($catPassed / $catTotal) * 100, 1) } else { 0 }
        $color = if ($catPassed -eq $catTotal) { "Green" } elseif ($catPassed -gt 0) { "Yellow" } else { "Red" }
        Write-Host "  $($cat.Name): $catPassed/$catTotal ($pct%)" -ForegroundColor $color
    }
    
    Write-Host "`n" + ("-" * 40) -ForegroundColor Gray
    
    $overallPct = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 1) } else { 0 }
    $overallColor = if ($failed -eq 0) { "Green" } elseif ($passed -gt $failed) { "Yellow" } else { "Red" }
    
    Write-Host "  TOTAL: $passed PASSED / $failed FAILED / $total TESTS ($overallPct%)" -ForegroundColor $overallColor
    
    if ($failed -gt 0) {
        Write-Host "`n  Failed Tests:" -ForegroundColor Red
        $failedTests = $script:TestResults | Where-Object { -not $_.Passed }
        foreach ($ft in $failedTests) {
            Write-Host "    [$($ft.TestId)] $($ft.Name) (Expected: $($ft.Expected), Got: $($ft.Actual))" -ForegroundColor Red
        }
    }
    
    Write-Host "`n"
}

# ==================== MAIN EXECUTION ====================

Write-Host "`n"
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║     DevComunity API Comprehensive Test Suite             ║" -ForegroundColor Magenta
Write-Host "║     Base URL: $BaseUrl                        ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta

try {
    switch ($Phase.ToLower()) {
        "auth" { 
            Test-AuthAPIs 
        }
        "questions" { 
            Test-AuthAPIs  # Need auth first
            Test-QuestionsAPIs 
        }
        "qa" {
            Test-AuthAPIs
            Test-QuestionsAPIs
            Test-AnswersAPIs
            Test-VotesAPIs
            Test-CommentsAPIs
        }
        "social" {
            Test-AuthAPIs
            Test-FollowAPIs
            Test-UsersAPIs
            Test-TagsAPIs
        }
        "security" {
            Test-SecurityTests
        }
        default {
            # All phases
            Test-AuthAPIs
            Test-QuestionsAPIs
            Test-AnswersAPIs
            Test-VotesAPIs
            Test-CommentsAPIs
            Test-FollowAPIs
            Test-UsersAPIs
            Test-TagsAPIs
            Test-SecurityTests
        }
    }
    
    Show-ResultsSummary
}
catch {
    Write-Host "`n❌ Test execution failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}