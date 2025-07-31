using BrowserStack.Page.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserStack.Tests
{
    internal class DrawingTest
    {
        protected static HomeController home = new HomeController();
        protected static DrawingController drawing = new DrawingController();
        public static void Drawing_Test()
        {
            home.GoToDrawing();
            drawing.Allow_Access_Device();
            drawing.Drawing();
            drawing.Clear();
        }
    }
}
