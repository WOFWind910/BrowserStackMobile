using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using  BrowserStack.Page.Controller;

namespace BrowserStack.Tests
{
    internal class LoginTest
    {
        protected static HomeController home = new HomeController();
        protected static LoginController login = new LoginController();
        public static void Login_Test()
        {
            home.GoToLogin();
            login.LoginWithoutName();
            login.LoginWithoutPassword();
            login.LoginSuccess();
            home.Logout();
        }
    }
}
