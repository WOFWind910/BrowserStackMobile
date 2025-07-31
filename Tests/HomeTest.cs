using BrowserStack.Page.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using  BrowserStack.Page.Controller;

namespace BrowserStack.Tests
{
    internal class HomeTest
    {
        protected static HomeController home = new HomeController();
        public static void ProductPageTest()
        {
            home.GoToCatalog();
            home.SelectSort();
            home.ClickProduct();
            home.RateProduct();
            home.IncreaseQuantity();
            home.IncreaseQuantity();
            home.AddToCart();
        }
    }
}
