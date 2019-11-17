using System;
using System.Net;

namespace Domain
{
    public class IpRecord
    {
        public long Id { get; set; }
        public ValueTuple<IPAddress, int> Network { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public long Hash { get; set; }
    }
}