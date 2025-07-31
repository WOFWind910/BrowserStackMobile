using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.MultiTouch;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BrowserStack.Tests.BaseTest;
namespace BrowserStack.Helper
{
    internal class DriverManage
    {
        private static AndroidDriver<AndroidElement> driver;
        private static WebDriverWait wait;

        public static void Back()
        {
            driver = GetDriver();
            driver.Navigate().Back();
        }

        public static IWebElement FindElement(By by)
        {
            return wait.Until(ExpectedConditions.ElementToBeClickable(by));
        }
        public static void ClickElement(By by)
        {
            wait = GetDriverWait();
            FindElement(by).Click();
        }
        public static IWebElement ScrollToElementByResourceId( string resourceId)
        {
            driver= GetDriver();
            string uiAutomatorString =
                $"new UiScrollable(new UiSelector().scrollable(true)).scrollIntoView(new UiSelector().resourceId(\"{resourceId}\"))";

            var element = driver.FindElement(MobileBy.AndroidUIAutomator(uiAutomatorString));
            Console.WriteLine($"✅ Đã scroll tới phần tử có resource-id: {resourceId}");
            return element;
        }

        public static void SendKey(By by, String content)
        {
            FindElement(by).SendKeys(content);
        }
    }
}
