using System.Collections.Generic;
using System.Net;

namespace Utils
{
    public class IPAddressEqualityComparer : IEqualityComparer<IPAddress>
    {
        public bool Equals(IPAddress b1, IPAddress b2)
        {
            return b1?.ToString() == b2?.ToString();
        }

        public int GetHashCode(IPAddress ip) => ip.ToString().GetHashCode();
    }
}
