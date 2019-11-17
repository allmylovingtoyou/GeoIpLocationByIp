using System.Threading.Tasks;
using Api.Dto.Location;

namespace HttpApi.Services.Interfaces
{
    /// <summary>
    /// Сервис определения местоположения по ip адресу.
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Получить координаты по ip адресу
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<LocationResponseDto> GetLocation(LocationRequestDto request);
    }
}