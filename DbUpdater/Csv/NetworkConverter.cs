using System;
using System.Linq;
using System.Net;
using TinyCsvParser.TypeConverter;

namespace DbUpdater.Csv
{
    public class NetworkConverter : NullableConverter<ValueTuple<IPAddress, int>>
    {
        protected override bool InternalConvert(string value, out (IPAddress, int) result)
        {
            var items = value.Split("/");

            var addrString = items.FirstOrDefault();
            var maskString = items.LastOrDefault();
            if (!int.TryParse(maskString, out var mask))
            {
                mask = 32;
            }

            if (string.IsNullOrWhiteSpace(addrString) || !IPAddress.TryParse(addrString, out var address))
            {
                result = ValueTuple.Create(IPAddress.None, 32);
                return false;
            }

            result = ValueTuple.Create(address, mask);
            return true;
        }
    }
}