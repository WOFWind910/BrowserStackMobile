using BrowserStack.Page.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using  BrowserStack.Page.Controller;

namespace BrowserStack.Test
{
    internal class HomeTest
    {
        protected static HomeController home = new HomeController();
        public static void AddToCart()
        {
            home.GoToCatalog();
            home.SelectSort();
            home.ClickProduct();
            home.IncreaseQuantity();
            home.IncreaseQuantity();
            home.AddToCart();
        }
    }
}
