using Newtonsoft.Json;

namespace Api.Dto.Location
{
    /// <summary>
    /// Запрос на получение координат по ip адресу.
    /// </summary>
    public class LocationRequestDto
    {
        [JsonProperty(Required = Required.Always)]
        public string IpAddress { get; set; }
    }
}