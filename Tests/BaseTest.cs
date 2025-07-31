using NUnit.Framework;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Support.UI;
using BrowserStack.Helper;
using TestStatus = NUnit.Framework.Interfaces.TestStatus;

namespace BrowserStack.Test
{
    public class BaseTest
    {
        protected static TestStatus status = TestContext.CurrentContext.Result.Outcome.Status;
        protected static AndroidDriver<AndroidElement> driver;
        protected static WebDriverWait wait;
        protected static AventStack.ExtentReports.ExtentReports extent;

        [SetUp]
        public void Init()
        {
            extent = ExtentManager.GetInstance();
            AppiumOptions appiumOptions = new AppiumOptions();
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.DeviceName, "Samsung Galaxy S20");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformName, "Android");
            appiumOptions.AddAdditionalCapability(MobileCapabilityType.PlatformVersion, "12");
            driver = new AndroidDriver<AndroidElement>(new Uri("http://127.0.0.1:4723/wd/hub"), appiumOptions);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            ExtentManager.SetTest(TestContext.CurrentContext.Test.Name);
        }

        public static AndroidDriver<AndroidElement> GetDriver()
        {
            return driver;
        }

        public static WebDriverWait GetDriverWait()
        {
            return wait;
        }

        [TearDown]
        public void Cleanup()
        {
            driver?.Dispose();
            extent.Flush();
        }

    }
}
