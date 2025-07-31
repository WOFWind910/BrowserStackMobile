using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BrowserStack.Helper.TakeScreenShot;
namespace BrowserStack.Helper
{
    public class ExtentManager
    {
        private static AventStack.ExtentReports.ExtentReports extent;
        private static ThreadLocal<ExtentTest> test = new ThreadLocal<ExtentTest>();

        public static void SetTest(String testName)
        {
            test.Value = extent.CreateTest(testName);
        }
        public static ExtentTest GetTest()
        {
            return test.Value;
        }

        public static void QuitTest()
        {
            test.Value = null;
        }

        public static void DisposeTest()
        {
            test.Dispose();
        }
        public static AventStack.ExtentReports.ExtentReports GetInstance()
        {
            if (extent == null)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\.."));
                string reportsFolder = Path.Combine(projectPath, "Reports");
                Directory.CreateDirectory(reportsFolder);
                string reportPath = Path.Combine(reportsFolder, "Task1_Spark.html");
                Console.WriteLine($"📄 Report path: {reportPath}");

                // ✨ Dùng SparkReporter thay vì HtmlReporter


                extent = new AventStack.ExtentReports.ExtentReports();
                var sparkReporter = new ExtentSparkReporter(reportPath);
                sparkReporter.Config.DocumentTitle = "Automation Test Report";
                sparkReporter.Config.ReportName = "Test Results";

                extent.AttachReporter(sparkReporter);
            }
            return extent;
        }

        public static ExtentTest AutoNoteTestCase(NUnit.Framework.Interfaces.TestStatus status, ExtentTest node, string message)
        {
            switch (status)
            {
                case NUnit.Framework.Interfaces.TestStatus.Passed:
                    return node.Pass(message);
                case NUnit.Framework.Interfaces.TestStatus.Failed:
                    return node.Fail(message);
                default:
                    return node.Pass(message);
            }
        }

        public static void NoteReport(NUnit.Framework.Interfaces.TestStatus status, String nameNode, String message, String nameImg)
        {
            AutoNoteTestCase(status, GetTest().CreateNode(nameNode), message).AddScreenCaptureFromPath(ScreenShot_And_GetPathImage(nameImg));
        }
    }
}
