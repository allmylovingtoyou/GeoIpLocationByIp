using System.Linq;
using Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace HttpApiTests.Common
{
    [SetUpFixture]
    public abstract class AbstractIntegrationTests
    {
        protected ApplicationDbContext Db;

        [OneTimeSetUp]
        public void Init()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Port=5432;Database=location_tests;Username=location_test;Password=location_test_pass");

            var db = new ApplicationDbContext(options.Options);
            db.Database.EnsureDeleted();
            db.Database.Migrate();
            Db = db;
        }

        [SetUp]
        public virtual void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
            Db.IpRecords.RemoveRange(Db.IpRecords.ToList());
            Db.SaveChanges();
        }
        
        protected virtual ILogger<T> MockLogger<T>()
        {
            return new Mock<ILogger<T>>().Object;
        }
    }
}