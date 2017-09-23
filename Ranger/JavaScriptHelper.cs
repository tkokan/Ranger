using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.IO;
using static Ranger.Properties.Settings;
namespace Ranger
{
    public static class JavaScriptHelper
    {
        private const string fileName = "tmp.html";

        public static double ExecuteAndRead(string html, string elementName)
        {
            var valueInDict = ExecuteAndRead(html, new[] { elementName });
            return valueInDict[elementName];
        }

        public static Dictionary<string, double> ExecuteAndRead(string html, IEnumerable<string> elementNames)
        {
            var path = Path.Combine(Default.RangerFolder, fileName);

            // ToDo: Find a way to do this without writing to a file.
            File.WriteAllText(path, html);

            var service = PhantomJSDriverService.CreateDefaultService(Default.RangerFolder);
            service.HideCommandPromptWindow = true;

            var driver = new PhantomJSDriver(service);
            var filePath = Path.Combine("file:///", path);
            var url = new Uri(path);

            driver.Navigate().GoToUrl(url);

            var values = new Dictionary<string, double>();

            foreach (var elementName in elementNames)
            {
                var element = driver.FindElement(By.Id(elementName));
                var value = double.Parse(element.Text);
                values.Add(elementName, value);
            }

            driver.Quit();
            File.Delete(path);

            return values;
        }
    }
}