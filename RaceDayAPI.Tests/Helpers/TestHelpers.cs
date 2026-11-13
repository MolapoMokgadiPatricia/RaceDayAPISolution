using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using RaceDayAPI.Services;

namespace RaceDayAPI.Tests.Helpers
{
    public static class TestHelpers
    {
        public static RaceDayContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<RaceDayContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new RaceDayContext(options);
        }

        public static void SeedBasicData(RaceDayContext context)
        {
            var passwordService = new PasswordService();

            context.Users.AddRange(
                new User
                {
                    UserID = 1,
                    FirstName = "Org",
                    LastName = "One",
                    Email = "org@raceday.co.za",
                    PasswordHash = passwordService.HashPassword("Password123!"),
                    Role = "Organiser",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    UserID = 2,
                    FirstName = "Part",
                    LastName = "One",
                    Email = "part@raceday.co.za",
                    PasswordHash = passwordService.HashPassword("Password123!"),
                    Role = "Participant",
                    CreatedAt = DateTime.UtcNow
                });

            context.Organisers.Add(new Organiser
            {
                OrganiserID = 1,
                OrganisationName = "RaceDay Events"
            });

            context.Participants.Add(new Participant { ParticipantID = 2 });

            context.Events.Add(new Event
            {
                EventID = 1,
                OrganiserID = 1,
                Name = "Comrades 2026",
                Description = "Ultra marathon",
                EventDate = DateTime.UtcNow.AddMonths(2),
                Location = "Durban",
                DistanceKm = 89.0m,
                EventType = "run",
                CreatedAt = DateTime.UtcNow
            });

            context.Categories.Add(new Category
            {
                CategoryID = 1,
                EventID = 1,
                Name = "Senior",
                MinAge = 20,
                MaxAge = 39,
                DistanceKm = 89.0m
            });

            context.SaveChanges();
        }

        public static void SetSession(ControllerBase controller, int userId, string role)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<ISessionFeature>(new TestSessionFeature());

            if (userId > 0)
            {
                httpContext.Session.SetInt32("UserID", userId);
            }
            if (!string.IsNullOrEmpty(role))
            {
                httpContext.Session.SetString("Role", role);
            }

            controller.ControllerContext = new ControllerContext(
                new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()));
        }
    }

    public class TestSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = new TestSession();
    }

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value)
        {
            return _store.TryGetValue(key, out value!);
        }
    }
}