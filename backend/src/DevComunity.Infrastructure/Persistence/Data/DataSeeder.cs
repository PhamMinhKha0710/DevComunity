using DevComunity.Application.Interfaces.Services;
using DevComunity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevComunity.Infrastructure.Persistence.Data;

public class DataSeeder
{
    private readonly DevComunityDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        DevComunityDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.MigrateAsync();
            }

            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Database already seeded.");
                return;
            }

            _logger.LogInformation("Seeding database...");

            // 1. Seed Users
            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    Email = "admin@devcomunity.com",
                    DisplayName = "Administrator",
                    PasswordHash = _passwordHasher.HashPassword("Admin@123"),
                    Bio = "System Administrator",
                    Location = "Hanoi, Vietnam",
                    ReputationPoints = 1000,
                    IsEmailVerified = true,
                    CreatedDate = DateTime.UtcNow
                },
                new User
                {
                    Username = "jdoe",
                    Email = "john.doe@example.com",
                    DisplayName = "John Doe",
                    PasswordHash = _passwordHasher.HashPassword("User@123"),
                    Bio = "Full-stack Developer | React & .NET",
                    Location = "New York, USA",
                    ReputationPoints = 150,
                    IsEmailVerified = true,
                    CreatedDate = DateTime.UtcNow
                },
                new User
                {
                    Username = "alice",
                    Email = "alice.smith@example.com",
                    DisplayName = "Alice Smith",
                    PasswordHash = _passwordHasher.HashPassword("User@123"),
                    Bio = "Data Scientist & Python Enthusiast",
                    Location = "London, UK",
                    ReputationPoints = 320,
                    IsEmailVerified = true,
                    CreatedDate = DateTime.UtcNow
                }
            };

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // 2. Seed Tags
            var tags = new List<Tag>
            {
                new Tag { TagName = "javascript", Description = "Programming language of the web", UsageCount = 0 },
                new Tag { TagName = "csharp", Description = "Multi-paradigm programming language by Microsoft", UsageCount = 0 },
                new Tag { TagName = "python", Description = "Interpreted, high-level programming language", UsageCount = 0 },
                new Tag { TagName = "react", Description = "A JavaScript library for building user interfaces", UsageCount = 0 },
                new Tag { TagName = "dotnet", Description = "Free, open-source developer platform", UsageCount = 0 },
                new Tag { TagName = "sql", Description = "Standard language for storing, manipulating and retrieving data", UsageCount = 0 },
                new Tag { TagName = "css", Description = "Style sheet language used for describing the presentation of a document", UsageCount = 0 },
                new Tag { TagName = "html", Description = "Standard markup language for documents designed to be displayed in a web browser", UsageCount = 0 }
            };

            await _context.Tags.AddRangeAsync(tags);
            await _context.SaveChangesAsync();

            // 3. Seed Questions
            var user1 = users[1]; // John
            var user2 = users[2]; // Alice
            var jsTag = tags.First(t => t.TagName == "javascript");
            var reactTag = tags.First(t => t.TagName == "react");
            var csharpTag = tags.First(t => t.TagName == "csharp");

            var questions = new List<Question>
            {
                new Question
                {
                    UserId = user1.UserId,
                    Title = "How to use useEffect in React?",
                    Body = "<p>I'm trying to understand how <code>useEffect</code> works. When exactly does it run?</p><p>Does it run on every render?</p>",
                    Score = 5,
                    ViewCount = 42,
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    QuestionTags = new List<QuestionTag>
                    {
                        new QuestionTag { Tag = jsTag },
                        new QuestionTag { Tag = reactTag }
                    }
                },
                new Question
                {
                    UserId = user2.UserId,
                    Title = "Dependency Injection in .NET Core not working",
                    Body = "<p>I registered my service as Scoped but I get a runtime error saying it cannot be resolved.</p><pre><code>services.AddScoped<IMyService, MyService>();</code></pre><p>Any ideas?</p>",
                    Score = 3,
                    ViewCount = 15,
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    QuestionTags = new List<QuestionTag>
                    {
                        new QuestionTag { Tag = csharpTag }
                    }
                }
            };

            await _context.Questions.AddRangeAsync(questions);
            await _context.SaveChangesAsync();

            // 4. Seed Answers
            var q1 = questions[0];
            var answers = new List<Answer>
            {
                new Answer
                {
                    QuestionId = q1.QuestionId,
                    UserId = user2.UserId, // Alice answers John
                    Body = "<p><code>useEffect</code> runs after every render by default. However, you can pass a dependency array as the second argument to control when it runs.</p><ul><li><code>[]</code>: Runs only once on mount</li><li><code>[prop]</code>: Runs when prop changes</li></ul>",
                    Score = 8,
                    IsAccepted = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-4)
                }
            };

            await _context.Answers.AddRangeAsync(answers);
            
            // Update counts
            jsTag.UsageCount++;
            reactTag.UsageCount++;
            csharpTag.UsageCount++;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
