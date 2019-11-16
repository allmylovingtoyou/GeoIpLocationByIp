using System;
using System.Net;
using System.Numerics;

namespace Domain
{
    public class IpRecord
    {
        public ValueTuple<IPAddress, int> Network { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public long Hash { get; set; }
    }
}