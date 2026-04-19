using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class WizzairPageTests
{
    private const int InteractionPauseMs = 10000;
    private const int KeyPauseMs = 180;

    private IWebDriver? driver;

    [SetUp]
    public void SetupTest()
    {
        var options = new ChromeOptions();
        options.AddArgument("--lang=en-GB");
        options.AddArgument("--start-maximized");

        driver = new ChromeDriver(options);
    }

    [TearDown]
    public void TeardownTest()
    {
        if (driver is null)
        {
            return;
        }

        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch
        {
            // ignore errors
        }
    }

    [Test]
    public void Wizzair_NextWeek_BucharestToBudapest_ShouldHaveAtLeastTwoFlights()
    {
        Assert.That(driver, Is.Not.Null);

        var wait = new WebDriverWait(driver!, TimeSpan.FromSeconds(30));

        driver.Navigate().GoToUrl("https://www.wizzair.com/en-gb");
        wait.Until(d => d.FindElement(By.TagName("body")).Displayed);
        Pause(60000);

        ClickWithPause(wait, By.XPath("//body"));

        var autocompleteInputs = wait.Until(d =>
        {
            var inputs = d.FindElements(By.CssSelector("input[id^='wa-autocomplete-input-']"))
                .Where(input => input.Displayed)
                .ToList();
            return inputs.Count >= 2 ? inputs : null;
        });

        TypeWithPause(autocompleteInputs[0], "buc");
        ClickWithPause(
            wait,
            By.XPath("//div[starts-with(@id,'wa-autocomplete-option-')]//label//*[self::strong or self::small][contains(translate(normalize-space(.),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'bucharest')]"));

        TypeWithPause(autocompleteInputs[1], "bu");
        ClickWithPause(
            wait,
            By.XPath("//div[starts-with(@id,'wa-autocomplete-option-')]//label//*[self::strong or self::small][contains(translate(normalize-space(.),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'budapest')]"));

        TryClickWithPause(driver, By.XPath("//div[@id='popper-panel-14']/div/div[2]/div/div/div[2]/div[35]/span"), timeoutSeconds: 5);
        TryClickWithPause(driver, By.XPath("//div[@id='popper-panel-14']/div/div[2]/div/div[2]/div[2]/div[14]/span"), timeoutSeconds: 5);

        if (!TryClickWithPause(driver, By.XPath("//div[@id='app']/div/main/div/div/div/div/div[2]/div[2]/div[2]/div/div/form/div/button/span/span"), timeoutSeconds: 10))
        {
            ClickWithPause(wait, By.XPath("//button[@type='submit' or .//span[contains(.,'Search')]]"));
        }

        if (driver.WindowHandles.Count > 1)
        {
            driver.SwitchTo().Window(driver.WindowHandles.Last());
            Pause();
        }

        wait.Until(d =>
        {
            string bodyText = d.FindElement(By.TagName("body")).Text;
            return !string.IsNullOrWhiteSpace(bodyText);
        });

        string currentPageHtml = driver.PageSource;

        if (currentPageHtml.Contains("Human Verification", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive("Wizzair returned Human Verification during automated UI testing.");
        }

        int flightsFound = Regex.Matches(currentPageHtml, @"\bW6\s?\d{3,4}\b")
            .Select(match => match.Value.Replace(" ", string.Empty))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        Assert.That(
            flightsFound,
            Is.GreaterThanOrEqualTo(2),
            $"Expected at least 2 OTP->BUD flights on the booking results page, but found {flightsFound}.");
    }

    private static void Pause(int milliseconds = InteractionPauseMs)
    {
        Thread.Sleep(milliseconds);
    }

    private static void TypeWithPause(IWebElement input, string text)
    {
        input.Click();
        Pause();

        input.SendKeys(Keys.Control + "a");
        Pause(120);
        input.SendKeys(Keys.Delete);
        Pause(120);

        foreach (char c in text)
        {
            input.SendKeys(c.ToString());
            Pause(KeyPauseMs);
        }

        Pause();
    }

    private static void ClickWithPause(WebDriverWait wait, By selector)
    {
        var element = wait.Until(d =>
        {
            try
            {
                var found = d.FindElement(selector);
                return found.Displayed && found.Enabled ? found : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        });

        element.Click();
        Pause();
    }

    private static bool TryClickWithPause(IWebDriver browser, By selector, int timeoutSeconds)
    {
        try
        {
            var shortWait = new WebDriverWait(browser, TimeSpan.FromSeconds(timeoutSeconds));
            var element = shortWait.Until(d =>
            {
                try
                {
                    var found = d.FindElement(selector);
                    return found.Displayed && found.Enabled ? found : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            });

            element.Click();
            Pause();
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
}
