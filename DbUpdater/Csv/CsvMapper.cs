using Domain;
using TinyCsvParser.Mapping;

namespace DbUpdater.Csv
{
    public class CsvMapper : CsvMapping<IpRecord>
    {
        public CsvMapper()
        {
            MapProperty(0, x => x.Network, new NetworkConverter());
            MapProperty(7, x => x.Latitude);
            MapProperty(8, x => x.Longitude);
        }
    }
}