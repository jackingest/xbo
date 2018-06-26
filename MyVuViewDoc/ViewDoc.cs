using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Appium.Android;
using System.Collections.Generic;

using SeleniumExtension;

namespace MyVuViewDoc
{
    [TestClass]
    public class TestViewDoc
    {
        public static AppiumDriver<RemoteWebElement> driver;

        private static Dictionary<string, string> settings = CommMethods.ReadParam();
        private static string username = settings["username"];
        private static string pwd = settings["pwd"];
        private static string sec_social_num = settings["sec_social_num"];
        private static string serverUrl = settings["serverUrl"];
        private static string doc_name = settings["doc_name"];

        private static int defaultTimeout = int.Parse(settings["defaultTimeout"]);
        private static int implicitWait = int.Parse(settings["implicitWait"]);

        [TestInitialize]
        public void Setup()
        {
            //Set the capabilities
            DesiredCapabilities capabilities = new DesiredCapabilities();
            capabilities = CommMethods.SetCapabilities();

            Uri serverUri = new Uri("http://127.0.0.1:4723/wd/hub");
            driver = new AndroidDriver<RemoteWebElement>(serverUri, capabilities, TimeSpan.FromSeconds(240));
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(implicitWait);//implicit wait, default is 0
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(defaultTimeout);//Set page load timeout, the default is 30s
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            driver.Quit();
        }

        [TestMethod]
        public void ViewDoc()
        {
            driver.Navigate().GoToUrl(serverUrl);
            CommMethods.LoginVU(driver, username, pwd);//Login MyVU

            RemoteWebElement ele_link_document = driver.FindElement(By.Id("AccountDocuments_0"));
            ele_link_document.Click();
            driver.FindElement(By.PartialLinkText(doc_name)).Click();

            driver.Context = "NATIVE_APP";
            driver.FindElement(By.Id("android:id/button1")).Click();//continue button
            driver.FindElement(By.Id("com.android.packageinstaller:id/permission_allow_button")).Click();//allow button
            try
            {
                //If the file was already downloaded, will prompt if download again
                driver.FindElement(By.Id("com.android.chrome:id/button_primary")).Click();//download button
            }
            catch (NoSuchElementException)
            {
                Console.Write("The first download the file, doesn't have the download again prompt!!!");
            }
            finally
            {
                Thread.Sleep(5000);
                RemoteWebElement ele_img = driver.FindElement(By.XPath("//android.view.ViewGroup[@resource-id='com.google.android.apps.pdfviewer:id/pdf_view']/android.view.ViewGroup"));
                Assert.IsNotNull(ele_img);//verify the image exists
                driver.FindElement(By.XPath("//android.widget.ImageButton[@content-desc=\"Navigate up\"]")).Click();//click back button
                Thread.Sleep(6000);

                driver.Context = "CHROMIUM";//Switch back to webview
                /*Log out*/
                ((IJavaScriptExecutor)driver).ExecuteScript("document.getElementById(\"wrapper\").scrollTop=500;");//swipe up
                driver.FindElement(By.Id("ProfileSettingsMobile")).Click();
                CommMethods.LogoutVU(driver);
            }
        }
    }
}