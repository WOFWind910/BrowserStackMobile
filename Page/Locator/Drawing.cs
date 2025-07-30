using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserStack.Page.Locator
{
    internal class Drawing
    {
        public static By clearBtn = MobileBy.AccessibilityId("Removes anything drawn on pad");
        public static By saveBtn = MobileBy.AccessibilityId("Save anything drawn on pad");
        public static By padToDraw = MobileBy.AccessibilityId("Pad to draw on");
        public static By allow = By.Id("com.android.permissioncontroller:id/permission_allow_button");
    }
}
