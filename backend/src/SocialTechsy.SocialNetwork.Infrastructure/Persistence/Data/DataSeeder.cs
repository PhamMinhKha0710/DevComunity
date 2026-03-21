using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

public class DataSeeder
{
    private readonly SocialTechsySocialNetworkDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        SocialTechsySocialNetworkDbContext context,
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
                User.Create("admin", "admin@SocialTechsy.SocialNetwork.com", _passwordHasher.HashPassword("Admin@123"), "Administrator"),
                User.Create("jdoe", "john.doe@example.com", _passwordHasher.HashPassword("User@123"), "John Doe"),
                User.Create("alice", "alice.smith@example.com", _passwordHasher.HashPassword("User@123"), "Alice Smith")
            };

            foreach (var u in users.Take(1))
            {
                u.UpdateProfile("Administrator", "System Administrator", "Hanoi, Vietnam", null);
            }
            foreach (var u in users.Skip(1).Take(1))
            {
                u.UpdateProfile("John Doe", "Full-stack Developer | React & .NET", "New York, USA", null);
            }
            foreach (var u in users.Skip(2).Take(1))
            {
                u.UpdateProfile("Alice Smith", "Data Scientist & Python Enthusiast", "London, UK", null);
            }

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // 2. Seed Tags
            var tags = new List<Tag>
            {
                new Tag { TagName = "javascript", Description = "Popular web programming language", UsageCount = 0 },
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
                Question.Create(user1.UserId, "How to use useEffect in React?", "<p>I'm trying to understand how <code>useEffect</code> works. When exactly does it run?</p><p>Does it run on every render?</p>"),
                Question.Create(user2.UserId, "Dependency Injection in .NET Core not working", "<p>I registered my service as Scoped but I get a runtime error saying it cannot be resolved.</p><pre><code>services.AddScoped<IMyService, MyService>();</code></pre><p>Any ideas?</p>")
            };

            await _context.Questions.AddRangeAsync(questions);
            await _context.SaveChangesAsync();

            questions[0].QuestionTags.Add(new QuestionTag { QuestionId = questions[0].QuestionId, TagId = jsTag.TagId });
            questions[0].QuestionTags.Add(new QuestionTag { QuestionId = questions[0].QuestionId, TagId = reactTag.TagId });
            questions[1].QuestionTags.Add(new QuestionTag { QuestionId = questions[1].QuestionId, TagId = csharpTag.TagId });

            await _context.Questions.AddRangeAsync(questions);
            await _context.SaveChangesAsync();

            // 4. Seed Answers
            var q1 = questions[0];
            var answers = new List<Answer>
            {
                Answer.Create(q1.QuestionId, user2.UserId, "<p><code>useEffect</code> runs after every render by default. However, you can pass a dependency array as the second argument to control when it runs.</p><ul><li><code>[]</code>: Runs only once on mount</li><li><code>[prop]</code>: Runs when prop changes</li></ul>")
            };
            answers[0].Accept();

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
