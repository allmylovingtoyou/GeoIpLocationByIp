using Api.Dto.Location;
using Domain;

namespace HttpApi.Mappings
{
    public class IpRecordMapper
    {
        public LocationResponseDto ToDto(IpRecord entity)
        {
            if (entity == null)
            {
                return new LocationResponseDto();
            }
            
            return new LocationResponseDto
            {
                Latitude = entity.Latitude,
                Longitude = entity.Longitude
            };
        } 
    }
}