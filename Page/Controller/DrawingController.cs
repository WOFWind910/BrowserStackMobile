using BrowserStack.Test;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.MultiTouch;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BrowserStack.Helper.DriverManage;
using static BrowserStack.Helper.ExtentManager;
using static BrowserStack.Page.Locator.Drawing;
namespace BrowserStack.Page.Controller
{
    internal class DrawingController : BaseTest
    {
        public void Drawing()
        {
            IWebElement pad = FindElement(padToDraw);
            int startX = pad.Location.X + 400;  // adjust offset as needed
            int startY = pad.Location.Y + 400;

            TouchAction touchAction = new TouchAction(GetDriver());

            touchAction
                .Press(startX, startY)
                .Wait(500)
                .MoveTo(500, 0)      
                .MoveTo(0, -1000)    
                .Release()
                .Perform();
            NoteReport(status, "Drawing", "Vẽ hoàn tất", "tranh_ve");
        }


        public void Clear()
        {
            ClickElement(clearBtn);
        }

        public void Save()
        {
            ClickElement(saveBtn);
        }

        public void Allow_Access_Device()
        {
            ClickElement(allow);
        }
    }
}
