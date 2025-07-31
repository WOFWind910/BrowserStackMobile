using System;
using System.Threading;
using System.Collections.ObjectModel;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Support.UI;
using BrowserStack.Tests;
using BrowserStack.Page.Controller;
using BrowserStack.Datatest;
using BrowserStack.Page.Locator;

namespace BrowserStack
{
  public class MainTest : BaseTest
    {
        [Test]
        public void TestApp()
        {
            LoginTest.Login_Test();
            HomeTest.ProductPageTest();
            CartTest.CheckOut_Test();
            DrawingTest.Drawing_Test();
        }
    }
}
