using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserStack.Page.Locator
{
    internal class Home
    {
        public static By product = By.XPath("(//android.widget.ImageView[@content-desc=\"Product Image\"])[1]");
        public static By fiveStar = By.XPath("//android.widget.ImageView[@resource-id=\"com.saucelabs.mydemoapp.android:id/start5IV\"]");
        public static By continueBtn = By.XPath("//android.widget.Button[@content-desc=\"Closes review dialog\"]");
        public static By cart = By.Id("com.saucelabs.mydemoapp.android:id/cartIV");
        public static By increaseBtn = By.Id("com.saucelabs.mydemoapp.android:id/plusIV");
        public static By decreaseBtn = By.Id("com.saucelabs.mydemoapp.android:id/minusIV");
        public static By addToCart = By.Id("com.saucelabs.mydemoapp.android:id/cartBt");

        public static By menuSort = By.Id("com.saucelabs.mydemoapp.android:id/sortIV");
        public static By sort3rd = By.XPath("//android.widget.TextView[@text=\"Price - Ascending\"]");

        public static By menu = By.XPath("//android.widget.ImageView[@content-desc=\"View menu\"]");
        public static By about = By.XPath("//android.widget.TextView[@resource-id=\"com.saucelabs.mydemoapp.android:id/itemTV\" and @text=\"About\"]");
        public static By drawing = By.XPath("//android.widget.TextView[@resource-id=\"com.saucelabs.mydemoapp.android:id/itemTV\" and @text=\"Drawing\"]");
        public static By login = MobileBy.AccessibilityId("Login Menu Item");
        public static By logout = MobileBy.AccessibilityId("Logout Menu Item");
        public static By acceptLogout = By.Id("android:id/button1");
        public static By catalog = By.XPath("//android.widget.TextView[@resource-id=\"com.saucelabs.mydemoapp.android:id/itemTV\" and @text=\"Catalog\"]");
    }
}
