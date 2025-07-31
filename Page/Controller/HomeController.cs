using BrowserStack.Tests;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BrowserStack.Page.Locator.Home;
using static BrowserStack.Helper.DriverManage;
using static BrowserStack.Helper.ExtentManager;
namespace BrowserStack.Page.Controller
{
    internal class HomeController : BaseTest
    {
        private AndroidDriver<AndroidElement> driver;
        public HomeController()
        {
            this.driver = BaseTest.GetDriver();
        }

        public void ClickProduct()
        {
            ClickElement(product);
            ScrollToElementByResourceId("com.saucelabs.mydemoapp.android:id/productHeightLightsTV");
        }

        public void RateProduct()
        {
           ClickElement(fiveStar);
            ClickElement(continueBtn);
        }

        public void GoToCart()
        {
            ClickElement(cart);
        }

        public void SelectSort()
        {
            ClickElement(menuSort);
            Thread.Sleep(1000);
            ClickElement(sort3rd);
        }

        public void AboutUs()
        {
            OpenMenu();
            ClickElement(about);
        }

        protected void OpenMenu()
        {
            ClickElement(menu);
            Thread.Sleep(1000);
        }

        public void IncreaseQuantity()
        {
            ClickElement(increaseBtn);
        }

        public void DecreaseQuantity()
        {
            ClickElement(decreaseBtn);
        }

        public void AddToCart()
        {
            ClickElement(addToCart);
            NoteReport(status, "Thêm sản phẩm vào giỏ hàng", "Thêm thành công", "them_thanh_cong");
        }

        public void GoToLogin()
        {
            OpenMenu();
            ClickElement(login);
        }

        public void GoToDrawing()
        {
            OpenMenu();
            ClickElement(drawing);
        }

        public void Logout()
        {
            OpenMenu();
            ClickElement(logout);
            ClickElement(acceptLogout);
        }

        public void GoToCatalog()
        {
            OpenMenu();
            ClickElement(catalog);
        }
    }
}
