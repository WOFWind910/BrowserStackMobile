using BrowserStack.Datatest;
using BrowserStack.Page.Controller;
using BrowserStack.Page.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserStack.Test
{
    internal class CartTest
    {
        protected static HomeController home = new HomeController();
        protected static CartController cart = new CartController();
        protected static LoginController login = new LoginController();

        public static void CheckOut()
        {
            home.GoToCart();
            cart.DecreaseQuantity(1);
            cart.CheckOut();
            login.LoginSuccess();
            cart.FillShippingAddress(ShippingAddress.GetShippingAddress());
            cart.FillCardInfor(CardInfor.getCard());
            cart.Order();
            cart.CheckSuccess();
        }
    }
}
