using BrowserStack.Tests;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserStack.Helper
{
    internal class TakeScreenShot
    {
        private static string CaptureScreenshot(string screenshotName)
        {
            IWebDriver driver = BaseTest.GetDriver();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string projectPath = Path.GetFullPath(Path.Combine(basePath, @"..\..\..\"));
            string folderPath = Path.Combine(projectPath, "Reports", "ScreenShot");
            Directory.CreateDirectory(folderPath);
            string fileName = screenshotName + ".png";
            string filePath = Path.Combine(folderPath, fileName);
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            ss.SaveAsFile(filePath);
            return Path.Combine("ScreenShot", fileName).Replace("\\", "/");
        }

        public static string ScreenShot_And_GetPathImage(String screenshotName)
        {
            return CaptureScreenshot(screenshotName);
        }

    }
}
