using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserStack.Datatest
{
    internal class ShippingAddress
    {
        public String fullname = "PTK";
        public String line1 = "Midlane";
        public String city = "uhaha";
        public String zipcode = "93213";
        public String country = "united";
        public static ShippingAddress GetShippingAddress()
        {
            ShippingAddress newC= new ShippingAddress();    
            return newC;
        }
    }
}
