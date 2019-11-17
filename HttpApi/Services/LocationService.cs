using System;
using System.Net;
using System.Threading.Tasks;
using Api.Dto.Location;
using Db;
using HttpApi.Exceptions;
using HttpApi.Mappings;
using HttpApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HttpApi.Services
{
    /// <inheritdoc />
    public class LocationService : ILocationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IpRecordMapper _mapper;
        private readonly ILogger<LocationService> _logger;

        public LocationService(ApplicationDbContext dbContext, IpRecordMapper mapper, ILogger<LocationService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<LocationResponseDto> GetLocation(LocationRequestDto request)
        {
            if (!IPAddress.TryParse(request.IpAddress, out var ip))
            {
                throw new ArgumentException("Can't parse ip from string: {0}", request.IpAddress);
            }

            var record = await _dbContext.IpRecords.FirstOrDefaultAsync(x => EF.Functions.ContainsOrEqual(x.Network, ip));
            if (record == null)
            {
                throw new InternalExceptions.NotFoundException(ip.ToString());
            }

            _logger.LogDebug("Found location: {0}, {1}, for ip: {3}", record.Latitude, record.Longitude, ip);
            return _mapper.ToDto(record);
        }
    }
}