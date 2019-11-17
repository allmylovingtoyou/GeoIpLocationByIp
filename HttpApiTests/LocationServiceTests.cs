using System;
using System.Net;
using System.Threading.Tasks;
using Api.Dto.Location;
using Domain;
using HttpApi.Exceptions;
using HttpApi.Mappings;
using HttpApi.Services;
using HttpApi.Services.Interfaces;
using HttpApiTests.Common;
using NUnit.Framework;
using static NUnit.Framework.Is;

namespace HttpApiTests
{
    [TestFixture]
    public class Tests : AbstractIntegrationTests
    {
        private ILocationService _service;
        private IpRecord _record1;
        private IpRecord _record2;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _service = new LocationService(Db, new IpRecordMapper(), MockLogger<LocationService>());
            CreateTestData();
            Db.IpRecords.Add(_record1);
            Db.IpRecords.Add(_record2);
            Db.SaveChanges();
        }

        [Test]
        public async Task TestGetLocationBySingleIpInDb()
        {
            var request = new LocationRequestDto
            {
                IpAddress = "192.168.150.11"
            };

            var result = await _service.GetLocation(request);
            Assert.That(result, Not.Null);
            Assert.That(result.Latitude, Not.Null);
            Assert.That(result.Latitude, EqualTo(_record1.Latitude));
            Assert.That(result.Longitude, Not.Null);
            Assert.That(result.Longitude, EqualTo(_record1.Longitude));
        }

        [Test]
        public async Task TestGetLocationByNetworkInDb()
        {
            var request = new LocationRequestDto
            {
                IpAddress = "192.168.11.15"
            };

            var result = await _service.GetLocation(request);
            Assert.That(result, Not.Null);
            Assert.That(result.Latitude, Not.Null);
            Assert.That(result.Latitude, EqualTo(_record2.Latitude));
            Assert.That(result.Longitude, Not.Null);
            Assert.That(result.Longitude, EqualTo(_record2.Longitude));
        }

        [Test]
        public void TestGetLocationNotFound()
        {
            var request = new LocationRequestDto
            {
                IpAddress = "10.10.10.10"
            };

            Assert.ThrowsAsync<InternalExceptions.NotFoundException>(async () => await _service.GetLocation(request));
        }

        private void CreateTestData()
        {
            _record1 = new IpRecord
            {
                Network = ValueTuple.Create(IPAddress.Parse("192.168.150.11"), 32),
                Longitude = 45.15,
                Latitude = 789.1
            };

            _record2 = new IpRecord
            {
                Network = ValueTuple.Create(IPAddress.Parse("192.168.11.0"), 24),
                Longitude = 2.2,
                Latitude = 3.3
            };
        }
    }
}