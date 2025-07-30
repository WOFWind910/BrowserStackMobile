using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserStack.Page.Locator
{
    internal class Login
    {
        public static By userNameField = By.Id("com.saucelabs.mydemoapp.android:id/nameET");
        public static By passwordField = By.Id("com.saucelabs.mydemoapp.android:id/passwordET");
        public static By loginBtn = By.Id("com.saucelabs.mydemoapp.android:id/loginBtn");
    }
}
