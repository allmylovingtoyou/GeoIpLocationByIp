namespace Api.Dto.Location
{
    /// <summary>
    /// Ответ на запрос получения местоположения по ip.
    /// </summary>
    public class LocationResponseDto
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}