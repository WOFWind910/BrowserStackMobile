using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserStack.Datatest
{
    internal class CardInfor
    {
        public String fullname = "PTK";
        public String cardNum = "3258125782149482";
        public String date = "03/25";
        public String securityCode = "132";

        public static CardInfor getCard()
        {
            CardInfor card = new CardInfor();
            return card;
        }
    }
}
