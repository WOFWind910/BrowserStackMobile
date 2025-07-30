#pragma warning disable
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Net;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using NLog;
using Serilog;
using Serilog;
using NLog.Config;
using NLog.Targets;
using BrowserStackSDK.TestObservability.Serilog.Sink;
using BrowserStackSDK.TestObservability.NLog.Appender;
using BrowserStackSDK.TestObservability.ConsoleAppender;
using BrowserStackSDK.TestObservability.TestCase.Model;
using BrowserStackSDK.TestObservability.Models.BDD;
using BrowserStackSDK.Utils;
using BrowserStackSDK.Automation;
using BrowserstackSDK.v2;
using BrowserstackSDK.v2.Framework;
using BrowserstackSDK.v2.Framework.State;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;








using NUnit.Framework;


using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;


internal static class Initializer
{
    public static List<MethodBase> patchMethods = new List<MethodBase>();
    [ModuleInitializer]
    internal static void Run() {
        try
        {
            BrowserstackSDK.v2.BrowserstackCLI.Instance.Bootstrap();
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(VanilaErrorHandler);
            BrowserStackSDK.Utils.ProcessHandler.HandleAdapterProcess();
        }
        catch (Exception ex) {
            BrowserstackPatcher.BrowserStackLog($"Exception while adding app domain {ex.ToString()}");
        }

        try
        {
            int index = int.Parse(Environment.GetEnvironmentVariable("index") ?? "0");
            string jsonText = File.ReadAllText(Environment.GetEnvironmentVariable("capabilitiesPath"));

            if (jsonText != null)
            {
                JArray json = JArray.Parse(jsonText);
                if(json.Count > 0)
                {
                    JObject jsonIndexed = (JObject)json[index];
                    BrowserStackSDK.Automation.Context.capabilitiesJson = jsonIndexed;
                }
            }
        }
        catch (Exception ex){
            BrowserstackPatcher.BrowserStackLog($"Error in initializer {ex.ToString()}");
        }

        Assembly assembly = Assembly.GetExecutingAssembly();
        BrowserStackSDK.Automation.Context.executingAssembly = assembly;
        string[] attributes = { "NUnit.Framework.TestAttribute", "NUnit.Framework.TestCaseAttribute", "NUnit.Framework.TestCaseSourceAttribute", "NUnit.Framework.TheoryAttribute" };
        var allTypes = assembly.GetTypes();
        foreach (var type in allTypes)
        {
            if (type.IsClass)
            {
                foreach (var method in type.GetMethods())
                {
                    foreach (var att in method.CustomAttributes)
                    {
                       if (attributes.Contains(att.Constructor.DeclaringType.ToString()) && (method.DeclaringType == method.ReflectedType))
                        {
                            patchMethods.Add(method);
                        }
                    }
                }
            }
        }
        if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_TOKEN")) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONSOLE_APPEANDER_CALLED")))
            Console.SetOut(new ConsoleAppender());
        BrowserstackPatcher.DoPatching();
    }

    static void VanilaErrorHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Exception e = (Exception)args.ExceptionObject;
        string filePath = Path.Join(Path.GetTempPath(), ".browserstack", "vanilaErrorFile_" + Environment.GetEnvironmentVariable("index"));
        var platformDetails = Environment.GetEnvironmentVariable("browserName") + " " + Environment.GetEnvironmentVariable("osVersion") + " " + Environment.GetEnvironmentVariable("os") + " " + Environment.GetEnvironmentVariable("browserVersion") ;
        string[] fileContents = { platformDetails + "\n------------\n" + e.Message + "\n" + e.GetBaseException() + "\n"};
        File.WriteAllLines(filePath, fileContents);
    }
}

public class BrowserstackPatcher
{
    static string logLevel = Environment.GetEnvironmentVariable("BROWSERSTACK_LOG_LEVEL");
    //public static Configuration configs;
    // make sure DoPatching() is called at start either by
    // the mod loader or by your injector
    public static void DoPatching()
    {
        var harmony = new Harmony("com.browserstack.patch");
        harmony.PatchAllUncategorized(Assembly.GetExecutingAssembly());
        if (BrowserStackSDK.Accessibility.Injector.IsAccessibility()) {
            harmony.PatchCategory(Assembly.GetExecutingAssembly(), "accessibility");
            try
            {
                harmony.PatchCategory(Assembly.GetExecutingAssembly(), "accessibility-generic");
            }
            catch (System.Exception ex)
            {
                BrowserstackPatcher.BrowserStackLog($"Error in wrapping A11y generic commands {ex.ToString()}");
            }
            try
            {
                harmony.PatchCategory(Assembly.GetExecutingAssembly(), "accessibility-non-generic");
            }
            catch (System.Exception ex)
            {
                BrowserstackPatcher.BrowserStackLog("Error in wrapping A11y non-generic commands " + ex);
            }
        }

        try
        {
            if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_TOKEN"))) {
                harmony.PatchCategory(Assembly.GetExecutingAssembly(), "testObservability");
            }
        }
        catch (Exception ex)
        {
            BrowserstackPatcher.BrowserStackLog("Exception while patch connection to retrieve integrations data : "+ ex.ToString());
        }

        if(Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG") != "1")
        {
            harmony.Patch(typeof(RemoteWebDriver).GetMethod(nameof(RemoteWebDriver.Dispose), new Type[] {}), prefix: new HarmonyMethod(typeof(DisposePatch).GetMethod(nameof(DisposePatch.Prefix))));
            foreach (var method in Initializer.patchMethods)
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(PatchTest).GetMethod(nameof(PatchTest.Prefix))), finalizer: new HarmonyMethod(typeof(PatchTest).GetMethod(nameof(PatchTest.Finalizer))));
            }
        }

    }

    public static void BrowserStackLog(String message)
    {
        if (logLevel == "debug")
        {
            Console.WriteLine(message);
        }
    }

    public static Context AddOrGetReqnrollContext()
    {
        Context automationContext = Context.AddOrGet();
        if (automationContext.FrameworkName == "xunit")
        {
            automationContext = Context.AddOrGet(automationContext.sessionName, automationContext.DisplayName);
        }
        return automationContext;
    }
}

class BrowserStackException : Exception
{
    private string oldStackTrace;

    public BrowserStackException(string message, string stackTrace) : base(message)
    {
        this.oldStackTrace = stackTrace;
    }


    public override string StackTrace
    {
        get
        {
            return this.oldStackTrace;
        }
    }
}

class Store {
    public static void PersistHubUrl(string optimalHubUrl) {
        try {
            Environment.SetEnvironmentVariable("BROWSERSTACK_HUB_URL", optimalHubUrl);
            string filePath = Path.Combine(Path.GetTempPath(), ".browserstack", "hubUrlList.json");
            if (File.Exists(filePath)) {
                string hubUrls = File.ReadAllText(filePath);
                hubUrls += $"; {optimalHubUrl}";
                File.WriteAllText(filePath, hubUrls);
                return;
            }
            File.WriteAllText(filePath, optimalHubUrl);
            return;
        } catch (Exception ex) {
            BrowserstackPatcher.BrowserStackLog($"Error in HubUrl {ex.ToString()}");
        }
    }
}


class BrowserStackOptions : ChromeOptions
{
    private Dictionary<string, object> additionalCapabilities = new Dictionary<string, object>();
    public BrowserStackOptions(String browser_name, String browser_version = "latest")
    {
        if (browser_name != null){
            this.BrowserName = browser_name;
        }
        if (browser_version != null){
            this.BrowserVersion = browser_version;
        }
    }

    public void AddBrowserName(Object browser_name)
    {
        if(this.BrowserName == null)
            this.BrowserName = browser_name.ToString();
    }

    public void AddBrowserVersion(Object browser_version)
    {
        if(this.BrowserVersion == null)
            this.BrowserVersion = browser_version.ToString();
    }

    public override void AddAdditionalCapability(string capabilityName, object capabilityValue)
    {
        try
        {
            if (capabilityName == "platformName") {
                if (this.PlatformName == null) {
                    this.PlatformName = capabilityValue.ToString();
                }
                return;
            }
            else if (capabilityName == "unhandledPromptBehavior")
            {
                string stringValue = capabilityValue?.ToString();
                if (Enum.TryParse<UnhandledPromptBehavior>(stringValue, true, out var behavior))
                {
                    this.UnhandledPromptBehavior = behavior;
                    return;
                }
                else
                {
                    throw new ArgumentException($"Invalid value for UnhandledPromptBehavior: {stringValue}");
                }
            }
            this.AddAdditionalCapability(capabilityName, capabilityValue, true);
        }
        catch (Exception error) {
            BrowserstackPatcher.BrowserStackLog("Exception in adding cap " + capabilityName + " with error: " + error.ToString());
        }
    }
}

[HarmonyPatch(typeof(HttpCommandExecutor))]
[HarmonyPatch(MethodType.Constructor)]
[HarmonyPatch(new Type[] { typeof(Uri), typeof(TimeSpan), typeof(bool) })]
class ExecutorPatch
{
    static void Prefix(ref Uri addressOfRemoteServer, ref TimeSpan timeout)
    {
        var url = Environment.GetEnvironmentVariable("hubUrl") ?? "https://hub.browserstack.com/wd/hub";
        addressOfRemoteServer = new Uri(url);
        timeout = timeout.Add(TimeSpan.FromSeconds(900));
    }
    static void Postfix(HttpCommandExecutor __instance)
    {
        if (Environment.GetEnvironmentVariable("proxy") != null){
            __instance.Proxy = new WebProxy(Environment.GetEnvironmentVariable("proxy"), false);

        }
    }
}

[HarmonyPatch(typeof(Response))]
[HarmonyPatch(MethodType.Constructor)]
[HarmonyPatch(new Type[] { typeof(Dictionary<string, object>) })]
class ResponsePatch
{
    static void Postfix(Response __instance)
    {
        try
        {
            if (__instance.Value != null)
            {
                Type responseType = __instance.Value.GetType();
                if (responseType.Equals(typeof(Dictionary<string, object>)))
                {
                    Dictionary<string, object> value = (Dictionary<string, object>) __instance.Value;
                    if (value.ContainsKey("optimalHubUrl"))
                    {
                        var hubUrl = "https://" + value["optimalHubUrl"] + "/wd/hub/";
                        Environment.SetEnvironmentVariable("hubUrl", hubUrl);
                        Environment.SetEnvironmentVariable("optimalHubFlag", "true");
                        Store.PersistHubUrl((string)value["optimalHubUrl"]);
                    }
                }
            }
        } catch (Exception ex) {
            BrowserstackPatcher.BrowserStackLog("Error in postfix - " + ex);
        }
    }
}

[HarmonyPatch]
class CommandExecutorPatch
{
    public static MethodBase TargetMethod()
    {
        MethodBase executeAsync = typeof(HttpCommandExecutor).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (executeAsync != null) return executeAsync;
        MethodBase execute = typeof(HttpCommandExecutor).GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return execute;
    }
    static void Prefix(Command commandToExecute, ref Uri ___remoteServerUri, ref TimeSpan ___serverResponseTimeout)
    {
        if (Environment.GetEnvironmentVariable("optimalHubFlag") == "true" && commandToExecute.Name != "newSession")
        {
            ___serverResponseTimeout = ___serverResponseTimeout.Add(TimeSpan.FromSeconds(900));
            ___remoteServerUri = new Uri(Environment.GetEnvironmentVariable("hubUrl"));
            Environment.SetEnvironmentVariable("optimalHubFlag", "false");
        }
        if (BrowserstackSDK.v2.BrowserstackCLI.Instance.IsRunning()) {
            Dictionary<string, object> args = new Dictionary<string, object>
                {
                    { "command", commandToExecute },
                    { "hook", "PRE" },
                    { "frameworkState", "EXECUTE" }
                };
            BrowserstackSDK.v2.BrowserstackCLI.Instance.GetAutomationFramework()?.TrackEvent(AutomationFrameworkState.EXECUTE, HookState.PRE, args);
            Dictionary<string, object> postArgs = new Dictionary<string, object>();
            postArgs["isFileUpload"] = "true";

            BrowserstackSDK.v2.BrowserstackCLI.Instance.GetAutomationFramework()?.TrackEvent(AutomationFrameworkState.EXECUTE, HookState.POST, postArgs);
        }
        else {
            BrowserStackSDK.Accessibility.Injector.PerformScan(commandToExecute.Name);
            if (BrowserStackSDK.Automation.Context.AddOrGet().BrowserstackAutomation)
            {
                BrowserStackSDK.Percy.PercyEventHandler.BeforeExecute(commandToExecute.Name);
            }
        }
    }
}

[HarmonyPatch(typeof(DriverService))]
[HarmonyPatch(MethodType.Constructor)]
[HarmonyPatch(new Type[] { typeof(string), typeof(int), typeof(string), typeof(Uri) })]
class ServicePatch
{
    static void Prefix(ref string servicePath, ref string driverServiceExecutableName)
    {
        try
        {
            File.Create(Path.Join(Path.GetTempPath(), "WebDriver.exe"));
        }catch (Exception ex){
            BrowserstackPatcher.BrowserStackLog("Error in service patch " + ex);
        }


        servicePath = Path.GetTempPath();
        driverServiceExecutableName = "WebDriver.exe";
    }
}

[HarmonyPatch]
class FindDriverServiceExecutablePatch
{
    static MethodBase TargetMethod()
    {
        // refer to C# reflection documentation:
        return typeof(DriverService).GetMethod("FindDriverServiceExecutable", BindingFlags.NonPublic |  BindingFlags.Static);
    }
    static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(DriverService), nameof(DriverService.Start))]
class StartPatch
{
    static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(RemoteWebDriver))]
[HarmonyPatch(MethodType.Constructor)]
[HarmonyPatch(new Type[] { typeof(ICommandExecutor), typeof(ICapabilities) })]
class WebDriverPatch
{
    public static Dictionary<int, RemoteWebDriver> drivers__ = new Dictionary<int, RemoteWebDriver>();
    public static Dictionary<int, bool> quitFromDrivers = new Dictionary<int, bool>();
    public static Dictionary<int, bool> insideTestMethods = new Dictionary<int, bool>();
    public static Dictionary<string, List<string>> errorMessagesList = new Dictionary<string, List<string>>();
    public static bool localNotSetError = false;
    public static string urlForExceptionInResp = "";

    static bool Prefix(ref dynamic commandExecutor, ref ICapabilities desiredCapabilities)
    {
        var cli = BrowserstackCLI.Instance;
        if (cli.IsRunning())
        {
            try
            {
                // Check if BrowserstackCLI is running
                // Fetch capabilities from CLI (implement GetFinalDriverCaps as needed)
                cli.GetFinalDriverCaps(commandExecutor, desiredCapabilities); // Adjust parameters as needed
                var driverCapsObject = cli.GetAutomationFramework().GetDriverCaps();
                if (driverCapsObject != null && driverCapsObject is Dictionary<string, object> driverCapsDict && driverCapsDict.Count > 0)
                {
                    var finalCapabilities = ConvertDictToCapabilities(desiredCapabilities, driverCapsDict);
                    desiredCapabilities = finalCapabilities;
                    var capsDictionary = ((ReadOnlyDesiredCapabilities)desiredCapabilities).ToDictionary();
                    }
            }
            catch (Exception ex)
            {
                BrowserstackPatcher.BrowserStackLog($"Exception in WebDriverPatch Prefix: {ex}");
            }
            return true;
        }

        // Adding here, because currently we don't support hook for this
        
        
        Dictionary<string, object> browserstackOptions = new Dictionary<string, object>();

        int index = int.Parse(Environment.GetEnvironmentVariable("index"));
        var browserName = Environment.GetEnvironmentVariable("browserName");
        var browserVersion = Environment.GetEnvironmentVariable("browserVersion");
        var isLocal = Environment.GetEnvironmentVariable("isLocal");
        var localIdentifier = Environment.GetEnvironmentVariable("localIdentifier");
        var proxy = Environment.GetEnvironmentVariable("proxy");

        BrowserStackOptions finalOptions = new BrowserStackOptions(browserName, browserVersion);
        var capsType = desiredCapabilities.GetType();
        var w3c_enabled = false;
        var browserstackAutomationEnv = Environment.GetEnvironmentVariable("BROWSERSTACK_AUTOMATION") != "False";
        Dictionary<string, Object> existingKeys = new Dictionary<string, object>();
        bool isBrowserChrome = false; // Check for non-browserstack a11y check
        if (capsType.ToString() == "OpenQA.Selenium.Appium.AppiumCapabilities")
        {
            AppiumCapabilities appiumCapabilities = (AppiumCapabilities)desiredCapabilities;
            var dic = appiumCapabilities.ToDictionary();
            foreach (var cap in dic)
            {
                try
                {
                    if (cap.Key == "browserName")
                    {
                        finalOptions.AddBrowserName(cap.Value);
                    }
                    else if (cap.Key == "browserVersion")
                    {
                        finalOptions.AddBrowserVersion(cap.Value);
                    }
                    else
                    {
                        finalOptions.AddAdditionalCapability(cap.Key, cap.Value);
                        BrowserstackPatcher.BrowserStackLog("Adding existing key " + cap.Key.ToString() + " value -> " + cap.Value.ToString());
                        existingKeys.TryAdd(cap.Key, cap.Value);
                    }

                    if (!browserstackAutomationEnv && cap.Value != null)
                    {
                        try
                        {
                            if(cap.Key == "bstack:options")
                            {
                                w3c_enabled = true;
                                string jsonValue = JsonConvert.SerializeObject(cap.Value);
                                browserstackOptions = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonValue);
                            }
                            else if (cap.Key == "browserstack.useW3C" || cap.Key == "browserstack.use_w3c")
                            {
                                if(cap.Value.ToString().ToLower() == "true")
                                {
                                    w3c_enabled = true;
                                }
                            }
                        }
                        catch(Exception e) {
                            BrowserstackPatcher.BrowserStackLog("Error in processing bstack:options and w3c_enabled for SDK caps " + e);
                        }
                    }
                }
                catch (Exception e)
                {
                    BrowserstackPatcher.BrowserStackLog("Exception in processing cap: " + e.ToString());
                }
            }
        } else
        {
            try
            {
                ReadOnlyDesiredCapabilities dc = (ReadOnlyDesiredCapabilities)desiredCapabilities;
                var dic = dc.ToDictionary();
                foreach (var cap in dic)
                {
                    try
                    {
                        if (cap.Key == "browserName")
                        {
                            if (cap.Value.ToString().ToLower() == "chrome")
                            {
                                isBrowserChrome = true;
                            }
                            finalOptions.AddBrowserName(cap.Value);
                        }
                        else if (cap.Key == "browserVersion")
                        {
                            finalOptions.AddBrowserVersion(cap.Value);
                        }
                        else
                        {
                            finalOptions.AddAdditionalCapability(cap.Key, cap.Value);
                            existingKeys.TryAdd(cap.Key, cap.Value);
                        }

                        if (!browserstackAutomationEnv && cap.Value != null)
                        {
                            try
                            {
                                if(cap.Key == "bstack:options")
                                {
                                    w3c_enabled = true;
                                    string jsonValue = JsonConvert.SerializeObject(cap.Value);
                                    browserstackOptions = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonValue);
                                }
                                else if (cap.Key == "browserstack.useW3C" || cap.Key == "browserstack.use_w3c")
                                {
                                    if(cap.Value.ToString().ToLower() == "true")
                                    {
                                        w3c_enabled = true;
                                    }
                                }
                            }
                            catch(Exception e) {
                                BrowserstackPatcher.BrowserStackLog("Error in processing bstack:options and w3c_enabled for SDK caps " + e);
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }catch{
                DesiredCapabilities dc = (DesiredCapabilities)desiredCapabilities;
                Type t = typeof(DesiredCapabilities);
                MethodInfo[] methods = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

                var internalMethod = methods.FirstOrDefault
                   ((m) => m.Name.Equals("AsReadOnly",
                   StringComparison.InvariantCulture));

                ReadOnlyDesiredCapabilities rdc = (ReadOnlyDesiredCapabilities) internalMethod.Invoke(dc, null);
                var dic = rdc.ToDictionary();
                foreach (var cap in dic)
                {
                    try
                    {
                        if (cap.Key == "browserName")
                        {
                            finalOptions.AddBrowserName(cap.Value);
                        }
                        else if (cap.Key == "browserVersion")
                        {
                            finalOptions.AddBrowserVersion(cap.Value);
                        }
                        else
                        {
                            finalOptions.AddAdditionalCapability(cap.Key, cap.Value);
                            existingKeys.TryAdd(cap.Key, cap.Value);
                        }

                        if (!browserstackAutomationEnv && cap.Value != null)
                        {
                            try
                            {
                                if(cap.Key == "bstack:options")
                                {
                                    w3c_enabled = true;
                                    string jsonValue = JsonConvert.SerializeObject(cap.Value);
                                    browserstackOptions = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonValue);
                                }
                                else if (cap.Key == "browserstack.useW3C" || cap.Key == "browserstack.use_w3c")
                                {
                                    if(cap.Value.ToString().ToLower() == "true")
                                    {
                                        w3c_enabled = true;
                                    }
                                }
                            }
                            catch(Exception e) {
                                BrowserstackPatcher.BrowserStackLog("Error in processing bstack:options and w3c_enabled for SDK caps " + e);
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }

        }

        String jsonText = null;
        try
        {
            jsonText = File.ReadAllText(Environment.GetEnvironmentVariable("capabilitiesPath"));
        }
        catch
        { }
        if (jsonText != null)
        {
            JArray json = JArray.Parse(jsonText);
            JObject options = null;
            if (!browserstackAutomationEnv)
            {
                try
                {
                    JObject jsonIndexed = (JObject)json[0];
                    var remoteServerUri = commandExecutor.GetType().GetField("remoteServerUri", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(commandExecutor);
                    if(remoteServerUri != null && remoteServerUri.ToString().Contains("browserstack.com") && json.Count > 0)
                    {
                        if (w3c_enabled)
                        {
                            JObject sdkCaps = (JObject)jsonIndexed.GetValue("w3c");
                            JObject bstackOptions = (JObject)sdkCaps.GetValue("bstack:options");
                            if (bstackOptions != null)
                            {
                                foreach (var cap in bstackOptions)
                                {
                                    if (browserstackOptions.ContainsKey(cap.Key))
                                    {
                                        browserstackOptions[cap.Key] = cap.Value;
                                    }
                                    else
                                    {
                                        browserstackOptions.Add(cap.Key, cap.Value);
                                    }
                                }
                            }

                            JObject a11ySdkCaps = (JObject)jsonIndexed.GetValue("a11yW3c");
                            JObject a11yBstackOptions = a11ySdkCaps != null ? (JObject)a11ySdkCaps.GetValue("bstack:options") : null;
                            if (a11yBstackOptions != null && isBrowserChrome)
                            {
                                foreach (var cap in a11yBstackOptions)
                                {
                                    if (browserstackOptions.ContainsKey(cap.Key))
                                    {
                                    browserstackOptions[cap.Key] = cap.Value;
                                    }
                                    else
                                    {
                                    browserstackOptions.Add(cap.Key, cap.Value);
                                    }
                                }
                            }
                            finalOptions.AddAdditionalCapability("bstack:options", browserstackOptions);
                        }
                        else
                        {
                            JObject sdkCaps = (JObject)jsonIndexed.GetValue("nonw3c");
                            foreach (var cap in sdkCaps)
                            {
                                finalOptions.AddAdditionalCapability(cap.Key, cap.Value);
                            }

                            JObject a11ySdkNonW3CCaps = (JObject)jsonIndexed.GetValue("a11yNonW3c");
                            foreach (var cap in a11ySdkNonW3CCaps)
                            {
                                finalOptions.AddAdditionalCapability(cap.Key, cap.Value);
                            }
                        }
                    }
                    else
                    {
                        JObject a11ySdkCaps = (JObject)jsonIndexed.GetValue("a11yW3c");
                        JObject a11yChromeOptions = a11ySdkCaps != null ? (JObject)a11ySdkCaps.GetValue("goog:chromeOptions") : null;
                        if (a11yChromeOptions != null && isBrowserChrome)
                        {
                        finalOptions.AddAdditionalCapability("goog:chromeOptions", a11yChromeOptions);
                        }
                    }
                }
                catch(Exception e) {
                    BrowserstackPatcher.BrowserStackLog("Error in processing SDK caps " + e);
                }
            }
            else
            {
                if(json.Count > 0)
                {
                    JObject jsonIndexed = (JObject)json[index];
                    options = (JObject)jsonIndexed.GetValue("bstack:options");
                    if (options != null)
                    {
                        foreach (var item in options)
                        {
                            browserstackOptions.Add(item.Key, item.Value);
                        }

                        if (isLocal == "true")
                        {
                            browserstackOptions.Add("local", true);
                            if (localIdentifier != "")
                                browserstackOptions.Add("localIdentifier", localIdentifier);
                        }

                        bool addedCaps = false;
                        try {
                            if (existingKeys.ContainsKey("bstack:options")) {
                                JObject existingBStackOptions = (JObject)JToken.FromObject(existingKeys["bstack:options"]);
                                JObject values = (JObject)JToken.FromObject(browserstackOptions);
                                existingBStackOptions.Merge(values);
                                finalOptions.AddAdditionalCapability("bstack:options", existingBStackOptions);
                                addedCaps = true;
                            }
                        } catch (Exception e) {
                            BrowserstackPatcher.BrowserStackLog("Exception in merging bstack:options " + e.ToString());
                        }

                        if (!addedCaps) finalOptions.AddAdditionalCapability("bstack:options", browserstackOptions);
                        jsonIndexed.Remove("bstack:options");
                    }
                    foreach (var item in jsonIndexed)
                    {
                        try
                        {
                            if (item.Value != null && existingKeys.ContainsKey(item.Key))
                            {
                                try
                                {
                                    JToken existingToken = JToken.FromObject(existingKeys[item.Key]);
                                    JToken newToken = item.Value;

                                    if (existingToken.Type == JTokenType.Object && newToken.Type == JTokenType.Object)
                                    {
                                        JObject ex = (JObject)existingToken;
                                        JObject values = (JObject)newToken;
                                        ex.Merge(values);
                                        finalOptions.AddAdditionalCapability(item.Key, ex);
                                    }
                                    else if (existingToken.Type == JTokenType.Array && newToken.Type == JTokenType.Array)
                                    {
                                        JArray exArray = (JArray)existingToken;
                                        JArray newArray = (JArray)newToken;
                                        foreach (var element in newArray)
                                        {
                                            exArray.Add(element);
                                        }
                                        finalOptions.AddAdditionalCapability(item.Key, exArray);
                                    }
                                    else
                                    {
                                        finalOptions.AddAdditionalCapability(item.Key, newToken);
                                    }
                                }
                                catch (Exception e)
                                {
                                    BrowserstackPatcher.BrowserStackLog("Exception in merging " + item.Key + " exception: " + e.ToString());
                                    finalOptions.AddAdditionalCapability(item.Key, item.Value);
                                }
                            }
                            else
                            {
                                finalOptions.AddAdditionalCapability(item.Key, item.Value);
                            }
                        }
                        catch (Exception e)
                        {
                            BrowserstackPatcher.BrowserStackLog("Exception in adding cap " + item.Key + " with error: " + e.ToString());
                        }
                    }
                }
                if (options == null && isLocal == "true")
                {
                    finalOptions.AddAdditionalCapability("browserstack.local", true);
                    if (localIdentifier != "")
                        finalOptions.AddAdditionalCapability("browserstack.localIdentifier", localIdentifier);
                }

            }
        }
        try
        {
            desiredCapabilities = finalOptions.ToCapabilities();
        }
        catch (Exception ex) {
            BrowserstackPatcher.BrowserStackLog("Error in initializer " + ex);
        }
        return true;
    }

    static void Postfix(RemoteWebDriver __instance)
    {
        if (BrowserstackSDK.v2.BrowserstackCLI.Instance.IsRunning())
        {
            BrowserstackSDK.v2.BrowserstackCLI.Instance.PersistDriverInstance(__instance);
            return;
        }

        drivers__[Thread.CurrentThread.ManagedThreadId] = __instance;
        BrowserStackSDK.Automation.Context automationContext = BrowserStackSDK.Automation.Context.AddOrGet();
        BrowserStackSDK.TestObservability.EventsHandler.LogEventsHandler.ReceiveDriver(__instance);

        if (Environment.GetEnvironmentVariable("BROWSERSTACK_AUTOMATION") == "False")
        {
            automationContext.BrowserstackAutomation = false;
            automationContext.AddDriver(__instance);
            BrowserStackSDK.Accessibility.Injector.InsideWebDriver();
        }
        else
        {
            automationContext.AddDriver(__instance);
        }
    }

    private static ICapabilities ConvertDictToCapabilities(ICapabilities originalCaps, Dictionary<string, object> capsDict)
    {
        var browserName = Environment.GetEnvironmentVariable("browserName");
        var browserVersion = Environment.GetEnvironmentVariable("browserVersion");
        // Similar to finalOptions.ToCapabilities() pattern from temp.txt
        var originalType = originalCaps.GetType();
        
        if (originalType.Name.Contains("ChromeOptions") || originalType == typeof(ChromeOptions))
        {
            var chromeOptions = new ChromeOptions();
            foreach (var kv in capsDict)
            {
                chromeOptions.AddAdditionalCapability(kv.Key, kv.Value);
            }
            return chromeOptions.ToCapabilities();
        }
        else if (originalType.Name.Contains("AppiumOptions"))
        {
            // For Appium - create AppiumOptions and add capabilities
            var appiumOptions = Activator.CreateInstance(originalType);
            var addCapabilityMethod = originalType.GetMethod("AddAdditionalCapability", new[] { typeof(string), typeof(object) });
            
            if (addCapabilityMethod != null)
            {
                foreach (var kv in capsDict)
                {
                    addCapabilityMethod.Invoke(appiumOptions, new object[] { kv.Key, kv.Value });
                }
            }
            
            return (ICapabilities)appiumOptions;
        }
        else
        {
            // Fallback: Use the pattern from temp.txt/backup files
            BrowserStackOptions finalOptions = new BrowserStackOptions(browserName, browserVersion);
            
            foreach (var kv in capsDict)
            {
                if (kv.Key == "browserName")
                {
                    finalOptions.AddBrowserName(kv.Value);
                }
                else if (kv.Key == "browserVersion")
                {
                    finalOptions.AddBrowserVersion(kv.Value);
                }
                else
                {
                    finalOptions.AddAdditionalCapability(kv.Key, kv.Value);
                }
            }
            
            return finalOptions.ToCapabilities();
        }
    }
}

class TestObservabilityReflector {
    private static Type? assemblyType;

    private static MethodInfo? LoadMethodInfo(string methodName, bool isTestContext = false) {
        string listenerDll = System.IO.Path.Join(System.AppDomain.CurrentDomain.BaseDirectory, "BrowserstackListener.dll");
        if (string.IsNullOrEmpty(listenerDll)) return null;

        Assembly assembly = Assembly.LoadFrom(listenerDll);
        string className = isTestContext ? "TestEventsHandler" : "LogEventsHandler";
        assemblyType = assembly.GetType($"TestObservability.EventsHandler.{className}");
        MethodInfo? method = assemblyType.GetMethod(methodName);
        return method;
    }

    public static void SendDriver(object __instance) {
        object[] webDriver = new object[1] {__instance};
        MethodInfo? receiveDriver = LoadMethodInfo("ReceiveDriver");
        if (receiveDriver != null) {
            receiveDriver.Invoke(null!, webDriver);
        }
    }

    public static void SendScreenshot(Screenshot __screenshot, Type screenShotType = null) {
        string[] screenshot = new string[1] {__screenshot.AsBase64EncodedString};
        MethodInfo? receiveScreenshot = LoadMethodInfo("ReceiveScreenshot");
        if (receiveScreenshot != null)
            receiveScreenshot.Invoke(null!, screenshot);
    }

    public static void SendTestContext(object testContext, string eventType, string sessionId = "") {
        object[] testContexts = new object[3] {testContext, eventType, sessionId};
        MethodInfo? receiveTestContext = LoadMethodInfo("ReceiveTestContext", true);
        if (receiveTestContext != null)
            receiveTestContext.Invoke(null!, testContexts);
    }
}

class DisposePatch {
    public static bool Prefix(RemoteWebDriver __instance)
    {
        try {
            
            Console.Out.Flush();
            Console.Out.Close();
            

            if (WebDriverPatch.insideTestMethods.GetValueOrDefault(Thread.CurrentThread.ManagedThreadId, false)) {
                WebDriverPatch.quitFromDrivers[Thread.CurrentThread.ManagedThreadId] = true;
                return false;
            } else {
                                var cli = BrowserstackCLI.Instance;
               if (cli.IsRunning())
               {
                   var testContext = TestContext.CurrentContext;
                   if (testContext != null && testContext.Test.FullName.Contains("AdhocTestMethod"))
                   {
                       testContext = PatchTest.GetStoredTestContext(__instance.SessionId.ToString());
                   }
                   if (testContext != null && !testContext.Test.FullName.Contains("AdhocTestMethod"))
                   {
                       cli.GetTestFramework()?.TrackEvent(TestFrameworkState.LOG_REPORT, HookState.PRE, testContext);
                       cli.GetTestFramework()?.TrackEvent(TestFrameworkState.LOG_REPORT, HookState.POST, testContext);
                       cli.GetTestFramework()?.TrackEvent(TestFrameworkState.TEST, HookState.POST, testContext);
                       Dictionary<string, object> args = new Dictionary<string, object>();
                       args["hook"] = "PRE";
                       args["frameworkState"] = "QUIT";
                       cli.GetAutomationFramework().TrackEvent(BrowserstackSDK.v2.Framework.State.AutomationFrameworkState.QUIT, BrowserstackSDK.v2.Framework.State.HookState.PRE, args);
                   }
                   BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId} CLI is running - skipping JavaScript session status setting in dispose");
                   return true;
               }
                BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId}  BROWSERSTACK_SKIP_SESSION_STATUS: {Environment.GetEnvironmentVariable("BROWSERSTACK_SKIP_SESSION_STATUS").ToLower()}");
                BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId}  __instance: {__instance?.ToString() ?? "null"} session_id: {__instance?.SessionId?.ToString() ?? "null"}");
                if (Environment.GetEnvironmentVariable("BROWSERSTACK_SKIP_SESSION_STATUS").ToLower() != "true" && __instance != null && __instance.SessionId != null) {
                    // Final session marking.
                    if (WebDriverPatch.errorMessagesList.GetValueOrDefault(__instance.SessionId.ToString(), new List<string>()).Count > 0) {
                        BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId}  Got error: {String.Join(", ", WebDriverPatch.errorMessagesList.GetValueOrDefault(__instance.SessionId.ToString(), new List<string>()))}");
                        ((IJavaScriptExecutor)__instance).ExecuteScript("browserstack_executor: {\"action\": \"setSessionStatus\", \"arguments\": {\"status\":\"failed\", \"reason\": " + JsonConvert.SerializeObject(String.Join(", ", WebDriverPatch.errorMessagesList.GetValueOrDefault(__instance.SessionId.ToString(), new List<string>()))) + "}}");
                        BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId}  Session marked as fail with reason");
                    }
                    else {
                        BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId}  Session marked as passed with reason: Passed");
                        ((IJavaScriptExecutor)__instance).ExecuteScript("browserstack_executor: {\"action\": \"setSessionStatus\", \"arguments\": {\"status\":\"passed\", \"reason\": \"Passed\"}}");
                    }
                }
            }
        } catch(Exception e) {
            BrowserstackPatcher.BrowserStackLog("Error in marking session status " + e);
        }
        return true;

    }
}


[HarmonyPatch]
[HarmonyPatch(typeof(RemoteWebDriver))]
[HarmonyPatch("GetScreenshot")]
class ScreenshotPatch {
    static void Postfix(ref Screenshot __result) {
        if (BrowserstackSDK.v2.BrowserstackCLI.Instance.IsRunning())
{
    string screenshot =  __result.AsBase64EncodedString;
    Dictionary<string, object> args = new Dictionary<string, object>();
 args["isScreenshot"] = "true";
    args["screenshot"] = screenshot;
    BrowserstackSDK.v2.BrowserstackCLI.Instance.GetAutomationFramework().TrackEvent(BrowserstackSDK.v2.Framework.State.AutomationFrameworkState.EXECUTE, BrowserstackSDK.v2.Framework.State.HookState.POST, args);
}
else {
    TestObservabilityReflector.SendScreenshot(__result, typeof(Screenshot));
}
        BrowserStackSDK.Percy.PercyEventHandler.OnEvent("driver.screenshot");
    }
}

[HarmonyPatch]
[HarmonyPatch(typeof(OpenQA.Selenium.Remote.RemoteWebElement))]
[HarmonyPatch("SendKeys")]
class SendKeysPatch {
    static void Postfix() {
        BrowserStackSDK.Percy.PercyEventHandler.OnEvent("element.sendKeys");
    }
}

[HarmonyPatch]
[HarmonyPatch(typeof(OpenQA.Selenium.Remote.RemoteWebElement))]
[HarmonyPatch("Click")]
class ClickPatch {
    static void Postfix() {
        BrowserStackSDK.Percy.PercyEventHandler.OnEvent("element.click");
    }
}

[HarmonyPatch]
class GoToUrlPatch
{
    public static List<MethodBase> accessMethods = new List<MethodBase>();
    public static List<string> validDomainOrIPs = new List<string>() { "^localhost$", "^bs-local.com$", "^127\\.", "^10\\.", "^172\\.1[6-9]\\.", "^172\\.2[0-9]\\.", "^172\\.3[0-1]\\.", "^192\\.168\\." };
    static List<MethodBase> TargetMethods()
    {
        MethodBase patchStringGoToUrl = AccessTools.Method(AccessTools.TypeByName("OpenQA.Selenium.Remote.RemoteNavigator"), "GoToUrl", new Type[] { typeof(String) });
        MethodBase patchUriGoToUrl = AccessTools.Method(AccessTools.TypeByName("OpenQA.Selenium.Remote.RemoteNavigator"), "GoToUrl", new Type[] { typeof(Uri) });
        accessMethods.Add(patchStringGoToUrl);
        accessMethods.Add(patchUriGoToUrl);
        return accessMethods;
    }
    static void Prefix(ref dynamic url)
    {
        String goToUrl = Convert.ToString(url);
        WebDriverPatch.urlForExceptionInResp = goToUrl;
        getNudgeLocalNotSetError(goToUrl);
    }

    static void getNudgeLocalNotSetError(String url)
    {
        try {
            var isLocal = Environment.GetEnvironmentVariable("isLocal");
            if (isLocal == "true" || WebDriverPatch.localNotSetError.Equals(true))
            {
                return;
            }
            string hostname = GetHostName(url);
            bool isPrivate = IsPrivateDomainOrIP(hostname);
                if (isPrivate)
            {
                string browserstackFolderPath = GetTempDir();
                if (!Directory.Exists(browserstackFolderPath))
                {
                    Directory.CreateDirectory(browserstackFolderPath);
                }
                string filePath = Path.Join(browserstackFolderPath, ".local-not-set.json");
                if (File.Exists(browserstackFolderPath))
                {
                    WebDriverPatch.localNotSetError = true;
                    return;
                }

                WebDriverPatch.localNotSetError = true;
                JObject j = new JObject();
                j.Add("hostname", hostname);
                File.WriteAllText(filePath, JsonConvert.SerializeObject(j));
            }
        } catch (Exception ex)
        {
            BrowserstackPatcher.BrowserStackLog("Error in nudge local error " + ex);
        }
    }

    static String GetHostName(String url)
    {
        String hostName = "";
        try
        {
            var uriObject = new Uri(url);
            hostName = uriObject.Host;

        }
        catch (Exception ex)
        {
            BrowserstackPatcher.BrowserStackLog("Error in get host name " + ex);
        }

        return hostName;
    }

    static bool IsPrivateDomainOrIP(String hostName)
    {
        bool isPrivate = false;
        if (!String.IsNullOrEmpty(hostName))
        {
            try
            {
                foreach (String reg in validDomainOrIPs)
                {
                    Regex regex = new Regex(reg);
                    if (regex.IsMatch(hostName))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                BrowserstackPatcher.BrowserStackLog("Error in private domain or ip " + ex);
            }
        }
        return isPrivate;
    }

    public static string GetTempDir()
    {
        string browserstackFolderPath = Path.Join(Path.GetTempPath(), ".browserstack");
        try
        {
            if (!Directory.Exists(browserstackFolderPath))
            {
                Directory.CreateDirectory(browserstackFolderPath);
            }
            return browserstackFolderPath;
        }
        catch (Exception ex)
        {
            BrowserstackPatcher.BrowserStackLog("Error in get temp directory " + ex);
        }
        return browserstackFolderPath;
    }

    static Exception Finalizer(Exception __exception)
    {
        try
        {
            var driver = WebDriverPatch.drivers__.GetValueOrDefault(Thread.CurrentThread.ManagedThreadId, null);
            if (driver != null)
            {
                var sessionName = TestContext.CurrentContext.Test.FullName;
var status = TestContext.CurrentContext.Result.Outcome.Status;
var message = TestContext.CurrentContext.Result.Message;
 
                if (message != null) message = message.ToString();
                if (__exception != null)
                {
                    message = __exception.Message;
                }
                if (message != null && (message.Contains("ERR_FAILED") || message.Contains("ERR_TIMED_OUT") || message.Contains("ERR_BLOCKED_BY_CLIENT") || message.Contains("ERR_NETWORK_CHANGED") || message.Contains("ERR_SOCKET_NOT_CONNECTED") || message.Contains("ERR_CONNECTION_CLOSED") || message.Contains("ERR_CONNECTION_RESET") || message.Contains("ERR_CONNECTION_REFUSED") || message.Contains("ERR_CONNECTION_ABORTED") || message.Contains("ERR_CONNECTION_FAILED") || message.Contains("ERR_ADDRESS_INVALID") || message.Contains("ERR_ADDRESS_UNREACHABLE") || message.Contains("ERR_TUNNEL_CONNECTION_FAILED") || message.Contains("ERR_CONNECTION_TIMED_OUT") || message.Contains("ERR_SOCKS_CONNECTION_FAILED") || message.Contains("ERR_SOCKS_CONNECTION_HOST_UNREACHABLE") || message.Contains("ERR_PROXY_CONNECTION_FAILED") || message.Contains("ERR_NAME_NOT_RESOLVED") || message.Contains("ERR_NAME_RESOLUTION_FAILED") || message.Contains("ERR_MANDATORY_PROXY_CONFIGURATION_FAILED")))
                {
                    try {
                        string hostName = GetHostName(WebDriverPatch.urlForExceptionInResp);
                        var isLocal = Environment.GetEnvironmentVariable("isLocal");
                        if (!(isLocal == "true" || WebDriverPatch.localNotSetError.Equals(true)))
                        {
                            string browserstackFolderPath = Path.Join(Path.GetTempPath(), ".browserstack");
                            try
                            {
                                if (!Directory.Exists(browserstackFolderPath))
                                {
                                    Directory.CreateDirectory(browserstackFolderPath);
                                }
                            }
                            catch (Exception ex)
                            {
                                BrowserstackPatcher.BrowserStackLog("Error in finalizer " + ex);
                            }
                            if (!Directory.Exists(browserstackFolderPath))
                            {
                                Directory.CreateDirectory(browserstackFolderPath);
                            }
                            string filePath = Path.Join(browserstackFolderPath, ".local-not-set.json");
                            if (File.Exists(browserstackFolderPath))
                            {
                                WebDriverPatch.localNotSetError = true;
                            }
                            else
                            {
                                WebDriverPatch.localNotSetError = true;
                                JObject j = new JObject();
                                j.Add("hostname", hostName);
                                File.WriteAllText(filePath, JsonConvert.SerializeObject(j));
                            }
                        }
                    } catch (Exception ex)
                    {
                        BrowserstackPatcher.BrowserStackLog("Error in finalizer " + ex);
                    }
                }
                
            }
        }
        catch(Exception ex)
        {
            BrowserstackPatcher.BrowserStackLog("Exception while running finalizer: "+ ex.ToString());
        }

        return __exception;
    }
}


[HarmonyPatch(typeof(NUnit.Framework.Internal.TestResult), nameof(NUnit.Framework.Internal.TestResult.SetResult))]
[HarmonyPatch(new Type[] { typeof(NUnit.Framework.Interfaces.ResultState), typeof(string), typeof(string) })]
class Patch07
{
static bool Prefix(ref string stackTrace)
{
if (stackTrace != null)
stackTrace = Regex.Replace(stackTrace, @"(_Patch)(\d+)(?!.*\d)", "");
return true;
}
}


class PatchTest
{

    

    private static readonly ConcurrentDictionary<string, TestContext> _testContexts = new();

    // Method to retrieve stored test context
    public static TestContext GetStoredTestContext(string sessionId)
    {
        return _testContexts.GetValueOrDefault(sessionId, null);
    }

    public static void Prefix(MethodBase __originalMethod, params object[] __args)
    {
        if(BrowserstackSDK.v2.BrowserstackCLI.Instance.IsRunning()) {
           BrowserstackSDK.v2.BrowserstackCLI.Instance.GetTestFramework().TrackEvent(
               BrowserstackSDK.v2.Framework.State.TestFrameworkState.INIT_TEST,
               BrowserstackSDK.v2.Framework.State.HookState.PRE,  new object[] { TestContext.CurrentContext }
           );
       }
        // Adding here, because currently we don't support hook for this and also as fallback logic for non-webdriver flow
        


        BrowserStackSDK.Automation.Context.ThreadNameAsync.Value = Thread.CurrentThread.ManagedThreadId;
        BrowserStackSDK.Automation.Context automationContext = BrowserStackSDK.Automation.Context.AddOrGet();
        // Observability specific handling, JWT_TOKEN = O11Y JWT Token
        if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_TOKEN"))) {
            Console.SetOut(new ConsoleAppender());
        }
        var driver = WebDriverPatch.drivers__.GetValueOrDefault(Thread.CurrentThread.ManagedThreadId, null);
        if (!BrowserstackSDK.v2.BrowserstackCLI.Instance.IsRunning())
{
        TestObservabilityReflector.SendDriver(driver);
}
        if (BrowserstackSDK.v2.BrowserstackCLI.Instance.IsRunning())
        {
            var instance = BrowserstackSDK.v2.Framework.AutomationFramework.GetTrackedInstance();
            BrowserStackDriver? instDriver = (BrowserStackDriver)instance?.GetDriver();
            dynamic? driverObj = instDriver?.GetDriver();
            if (driverObj?.SessionId?.ToString() != null)
                _testContexts[driverObj?.SessionId?.ToString()] = TestContext.CurrentContext;
            BrowserstackSDK.v2.BrowserstackCLI.Instance.GetTestFramework().TrackEvent(BrowserstackSDK.v2.Framework.State.TestFrameworkState.TEST, BrowserstackSDK.v2.Framework.State.HookState.PRE, new object[] {TestContext.CurrentContext, driverObj?.SessionId?.ToString() ?? ""});
            return;
        }
        else
        {
        TestObservabilityReflector.SendTestContext(TestContext.CurrentContext, "TestRunStarted", driver?.SessionId?.ToString() ?? "");
        }
        
        var sessionName = TestContext.CurrentContext.Test.FullName;
var status = TestContext.CurrentContext.Result.Outcome.Status;
var message = TestContext.CurrentContext.Result.Message;
 

        WebDriverPatch.insideTestMethods[Thread.CurrentThread.ManagedThreadId] = true;
        
        automationContext.sessionName = sessionName;
        automationContext.insideTestMethods = true;

        BrowserStackSDK.Accessibility.Injector.BeforeTest();
    }

    public static Exception FinalizerAsync(Exception __exception, Task __result, MethodBase __originalMethod, params object[] __args)
    {
        try
        {
            __result.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            __exception = ex;
        }

        return PatchTest.Finalizer(__exception, __originalMethod, __args);
    }

    public static Exception Finalizer(Exception __exception, MethodBase __originalMethod, params object[] __args)
    {
        var driver = WebDriverPatch.drivers__.GetValueOrDefault(Thread.CurrentThread.ManagedThreadId, null);

                    var cli = BrowserstackCLI.Instance;
            if (cli.IsRunning()) {
                // Pass the NUnit test context as args (or wrap as needed)
                cli.GetTestFramework()?.TrackEvent(TestFrameworkState.LOG_REPORT, HookState.PRE, TestContext.CurrentContext);
                cli.GetTestFramework()?.TrackEvent(TestFrameworkState.LOG_REPORT, HookState.POST, TestContext.CurrentContext);
                cli.GetTestFramework()?.TrackEvent(TestFrameworkState.TEST, HookState.POST, TestContext.CurrentContext);
                BrowserStackSDK.Percy.PercyEventHandler.OnEvent("test.end");
                Dictionary<string, object> args = new Dictionary<string, object>();
                args["hook"] = "PRE";
                args["frameworkState"] = "QUIT";
                cli.GetAutomationFramework().TrackEvent(BrowserstackSDK.v2.Framework.State.AutomationFrameworkState.QUIT, BrowserstackSDK.v2.Framework.State.HookState.PRE, args);
                return __exception;
            }
            else {
                TestObservabilityReflector.SendTestContext(TestContext.CurrentContext, "TestRunFinished", driver?.SessionId?.ToString() ?? "");
            }

        BrowserStackSDK.Automation.Context automationContext = BrowserStackSDK.Automation.Context.AddOrGet();
        BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId} and Driver: {driver?.ToString() ?? "null"}");
        if (driver != null)
        {
            var sessionName = TestContext.CurrentContext.Test.FullName;
var status = TestContext.CurrentContext.Result.Outcome.Status;
var message = TestContext.CurrentContext.Result.Message;
 
            

            automationContext.sessionName = sessionName;
            if (message != null) message = message.ToString();
            BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId} and Exception: {__exception?.ToString() ?? "null"}");
            if (__exception != null)
            {
                status = NUnit.Framework.Interfaces.TestStatus.Failed;
                message = __exception.Message;
                automationContext.status = "failed";
                BrowserStackSDK.Utils.OrchestrationClient.ReportFailureAsync(sessionName).Wait();
            }
            else
            {
                automationContext.status = "passed";
            }

            BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId} and automationContext: {JsonConvert.SerializeObject(automationContext)}");
            if (automationContext.BrowserstackAutomation) {
                if(Environment.GetEnvironmentVariable("BROWSERSTACK_SKIP_SESSION_NAME").ToLower() != "true")
                    ((IJavaScriptExecutor)driver).ExecuteScript("browserstack_executor: {\"action\": \"setSessionName\", \"arguments\": {\"name\": " + JsonConvert.SerializeObject(sessionName) + "}}");
                if (status == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId}  Checking if errorMessagesList contains sessionId: {driver.SessionId.ToString()}");
                    if (WebDriverPatch.errorMessagesList.ContainsKey(driver.SessionId.ToString()))
                    {
                        BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId}  errorMessagesList contains sessionId: {driver.SessionId.ToString()} adding message: {message}");
                        WebDriverPatch.errorMessagesList[driver.SessionId.ToString()].Add(message);
                    }
                    else
                    {
                        BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId}  errorMessagesList does not conatin sessionId: {driver.SessionId.ToString()}");
                        WebDriverPatch.errorMessagesList.Add(driver.SessionId.ToString(), new List<string> { message });
                        BrowserstackPatcher.BrowserStackLog($"[SESSION_MARKING] Thread: {Thread.CurrentThread.ManagedThreadId}  errorMessagesList added sessionId: {driver.SessionId.ToString()} with message: {message}");
                    }
                    ((IJavaScriptExecutor)driver).ExecuteScript("browserstack_executor: {\"action\": \"annotate\", \"arguments\": {\"data\": " + JsonConvert.SerializeObject("Failed - " + message) + ", \"level\": \"error\"}}");
                }
                else
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("browserstack_executor: {\"action\": \"annotate\", \"arguments\": {\"data\": \"Passed\", \"level\": \"info\"}}");
                }
            }
            BrowserStackSDK.Accessibility.Injector.AfterTest();
            BrowserStackSDK.Percy.PercyEventHandler.OnEvent("test.end");
            WebDriverPatch.insideTestMethods[Thread.CurrentThread.ManagedThreadId] = false;
            if (WebDriverPatch.quitFromDrivers.GetValueOrDefault(Thread.CurrentThread.ManagedThreadId, false))
            {
                WebDriverPatch.quitFromDrivers[Thread.CurrentThread.ManagedThreadId] = false;
                driver.Quit();
            }

            
        }

        return __exception;
    }
}

[HarmonyPatch(typeof(LoggerConfiguration))]
[HarmonyPatch(MethodType.Constructor)]
class SeriLogPatch {
    static void Postfix(ref LoggerConfiguration __instance) {
        // Check if __instance is null
        if (__instance == null)
        {
            BrowserstackPatcher.BrowserStackLog("SeriLogPatch: __instance is null in Postfix.");
            return;
        }

        try
        {
            // Access the WriteTo property first
            var sinkConfiguration = __instance.WriteTo;

            // Check if sinkConfiguration is null (shouldn't be for a valid LoggerConfiguration)
            if (sinkConfiguration == null)
            {
                BrowserstackPatcher.BrowserStackLog("SeriLogPatch: __instance.WriteTo resulted in null.");
                return;
            }

            // Now call the actual method 
            sinkConfiguration.TestObservabilitySerilogSink();
        }
        catch (Exception ex)
        {
            // Log potential errors during patching/configuration
            BrowserstackPatcher.BrowserStackLog($"SeriLogPatch: Error applying TestObservability sink - {ex}");
        }
    }
}

[HarmonyPatch(typeof(LoggingConfiguration))]
[HarmonyPatch(MethodType.Constructor)]
class LoggingConfigurationPatch {
    static void Postfix(ref LoggingConfiguration __instance) {
      TestObservabilityNLogAppender nlogAppender = new TestObservabilityNLogAppender();
      __instance.AddTarget("custom", nlogAppender);
      LoggingRule rule = new("*", NLog.LogLevel.Debug, nlogAppender);
      __instance.LoggingRules.Add(rule);
    }
}

[HarmonyPatch]
[HarmonyPatch(typeof(Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers.TestRunEventsHandler))]
[HarmonyPatch("HandleTestRunComplete")]

class TestRunCompletePatch {
    static void Prefix() {
        try {
            if (BrowserstackSDK.v2.BrowserstackCLI.Instance.IsRunning()) {
                BrowserstackSDK.v2.Framework.Utils.SeleniumMethodUtils.PerformCleanup();
            }
            else {
                BrowserStackSDK.Automation.Context.PerformCleanup();
            }
        } catch (Exception ex)
        {
            BrowserstackPatcher.BrowserStackLog("Error in run complete patch " + ex);
        }
    }
}

[HarmonyPatch]
[HarmonyPatch(typeof(Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.PlatformEnvironment))]
[HarmonyPatch("Exit")]

class PlatformEnvironmentPatch {
    static void Prefix() {
        try {
            if (BrowserstackSDK.v2.BrowserstackCLI.Instance.IsRunning()) {
                BrowserstackSDK.v2.Framework.Utils.SeleniumMethodUtils.PerformCleanup();
            }
            else {
                BrowserStackSDK.Automation.Context.PerformCleanup();
            }
        } catch (Exception ex)
        {
            BrowserstackPatcher.BrowserStackLog("Error in platform environment patch " + ex);
        }
    }
}










#pragma warning restore
