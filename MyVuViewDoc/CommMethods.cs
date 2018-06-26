using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.IO;
using System.Collections.Generic;

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace SeleniumExtension
{
    class CommMethods
    {
        private static String curent_dir = Environment.CurrentDirectory;//Get current path
        private static DirectoryInfo folder_info = Directory.GetParent(curent_dir).Parent.Parent;//get parent folder info
        private static String driver_dir = folder_info.FullName;//convert to full path

        /* Get parameters from the text file
         */
        public static Dictionary<string, string> ReadParam()
        {
            string[] lines = File.ReadAllLines(driver_dir + @"\parameters.txt");
            Dictionary<string, string> mysettings = new Dictionary<string, string>();
            foreach (string line in lines)
            {
                if (line.Contains("="))
                {
                    string[] keyAndValue = line.Split(new char[] { '=' });
                    mysettings.Add(keyAndValue[0].Trim(), keyAndValue[1].Trim());
                }
            }
            return mysettings;
        }

        /* Set Capabilities for the driver
         */
        public static DesiredCapabilities SetCapabilities()
        {
            Dictionary<string, string> mysettings = ReadParam();
            //Set the capabilities
            DesiredCapabilities capabilities = new DesiredCapabilities();
            capabilities.SetCapability("automationName", mysettings["automationName"]); //Define the automation engine, default should be Appium.//UiAutomator2
            capabilities.SetCapability("newCommandTimeout", "60");//timeout for Apppium waiting for a new command from client

            capabilities.SetCapability("deviceName", mysettings["deviceName"]);
            capabilities.SetCapability("platformVersion", mysettings["platformVersion"]);

            capabilities.SetCapability("browserName", mysettings["browserName"]); //the browser name for Android
            capabilities.SetCapability("chromedriverExecutable", driver_dir + mysettings["chromedriverExecutable"]); //Special correct driver version for the tested chrome version
            capabilities.SetCapability("platformName", "Android");
            capabilities.SetCapability("resetkeyboard", "true");//reset
            capabilities.SetCapability("clearSystemFiles", "true");

            capabilities.SetCapability("noReset", "true");//Don't reset app state before this session
            capabilities.SetCapability("fullRest", "false");

            return capabilities;
        }

        /* Login to VU with valid username/pwd
         */
        public static void LoginVU(AppiumDriver<RemoteWebElement> driver, string Username, string Password)
        {
            Dictionary<string, string> mysettings = ReadParam();

            RemoteWebElement ele_username = driver.FindElement(By.Id("LoginId"));
            RemoteWebElement ele_password = driver.FindElement(By.Id("Password"));
            RemoteWebElement ele_loginButton = driver.FindElement(By.Id("myvuloginsubmit"));

            ele_username.Clear();
            ele_username.SendKeys(Username);
            ele_password.Clear();
            ele_password.SendKeys(Password);
            driver.HideKeyboard();
            ele_loginButton.Click();
            WaitUntilElementVisible(driver, By.CssSelector("button.button.block.ng-binding"));
            driver.FindElement(By.CssSelector("button.button.block.ng-binding")).Click();
            driver.FindElement(By.Id("myvuVerifySSN")).SendKeys(mysettings["sec_social_num"]);
            IWebElement checkbox = driver.FindElement(By.CssSelector("input.ng-pristine.ng-untouched.ng-valid.ng-not-empty"));//checkbox of "Remember me on this device"
            if (checkbox.Selected)
            {
                checkbox.Click();//uncheck to un-remember on the device
            }
            driver.FindElement(By.CssSelector("button.button.block.ng-binding")).Click();//click verify button
            WaitUntilElementVisible(driver, By.Id("AccountDocuments_0"));
        }

        /* Logout VU from the Profile Settings page
         * 
         */
        public static void LogoutVU(AppiumDriver<RemoteWebElement> driver)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("document.getElementById(\"wrapper\").scrollTop=500;");//swipe up
            Thread.Sleep(2000);
            driver.FindElement(By.Id("signoutButton")).Click();//Click logout button
            WaitForPageLoad(driver);
        }

        /* Reset username and password by self
         */
        public static void ResetUser(AppiumDriver<RemoteWebElement> driver, string original_username,
            string original_pwd, string changed_username, string changed_pwd)
        {
            LoginVU(driver, changed_username, original_pwd);

            RemoteWebElement ele_profile = driver.FindElement(By.Id("ProfileSettingsMobile"));
            ele_profile.Click();

            RemoteWebElement ele_link_changeName = driver.FindElements(By.CssSelector("button.button.basic.sm"))[2];//chang login id button
            ele_link_changeName.Click();
            RemoteWebElement ele_input_changedUsername = driver.FindElement(By.Id("myvuChangeUsername"));
            ele_input_changedUsername.Click();
            ele_input_changedUsername.SendKeys(original_username);//input the original login id to reset
            Thread.Sleep(3000);
            String scripts = "return window.getComputedStyle(document.querySelector('li.icon-field'), '::after').getPropertyValue('content')";
            String content = (String)((IJavaScriptExecutor)driver).ExecuteScript(scripts);
            Assert.AreEqual("\"✓ Available\"", content);//assert email available
            RemoteWebElement ele_button_saveUsername = driver.FindElement(By.Id("changeUsernameSubmitButton"));
            ele_button_saveUsername.Click();
            Thread.Sleep(5000);
            Assert.IsTrue(driver.FindElement(By.CssSelector("vu-icon.card-icon.success")).Displayed);//verify saving successful

            RemoteWebElement ele_button_finish = driver.FindElement(By.Id("username-change"));
            ele_button_finish.Click();//click finish button

            LogoutVU(driver);
        }

        /* Requires adding SeleniumExtras.WaitHelpers reference
         */
        public static void WaitUntilElementVisible(AppiumDriver<RemoteWebElement> driver, By locator)
        {
            Dictionary<string, string> settings = CommMethods.ReadParam();
            int defaultTimeout = int.Parse(settings["defaultTimeout"]);

            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(defaultTimeout));
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(locator));
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Element with locator: '" + locator + "' was not found.");
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Timeout waiting for element with locator: '" + locator + "' was not found.");
            }
        }

        public static void WaitForPageLoad(AppiumDriver<RemoteWebElement> driver)
        {
            Dictionary<string, string> settings = CommMethods.ReadParam();
            int defaultTimeout = int.Parse(settings["defaultTimeout"]);

            try
            {
                Thread.Sleep(3000); // wait 3sec always: give old page chance to unload
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(defaultTimeout));
                wait.Until<bool>((d) => (bool)((IJavaScriptExecutor)driver).ExecuteScript(
                        "return document.readyState").Equals("complete"));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Timeout waiting for Page Load Request to complete");
            }
        }

        /* Wait for expected element present until timeout
         */
        public static void WaitForElePresent(AppiumDriver<RemoteWebElement> driver, By locator)
        {
            Dictionary<string, string> settings = CommMethods.ReadParam();
            int defaultTimeout = int.Parse(settings["defaultTimeout"]);

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(defaultTimeout));
            wait.Until<IWebElement>((d) =>
            {
                IWebElement element = d.FindElement(locator);
                if (element.Displayed &&
                    element.Enabled)
                {
                    return element;
                }
                return null;
            });
        }

    }
}