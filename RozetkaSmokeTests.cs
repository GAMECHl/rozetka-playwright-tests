using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace RozetkaTests;

/// <summary>
/// Selectors live in one place, so if the site markup changes
/// you only need to fix them here.
/// </summary>
public static class Selectors
{
    public const string SearchInput = "input[name='search']";

    // Product page URLs on Rozetka look like /ua/some-product-name/p123456789/
    // CSS cannot match a regex, so we find the first such link with a small script.
    public const string FirstProductHrefScript =
        "() => { const a = [...document.querySelectorAll('a[href]')]" +
        ".find(x => /\\/p\\d+\\/?(\\?|$)/.test(x.getAttribute('href')));" +
        " return a ? a.href : null; }";
}

[TestFixture]
public class RozetkaSmokeTests : PageTest
{
    private const string BaseUrl = "https://rozetka.com.ua/ua/";

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        Locale = "uk-UA",
        ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
    };

    // Opens a page. If Cloudflare shows its "verify you are human" check,
    // we wait for it to pass and open the target page again, because
    // Cloudflare redirects to the home page afterwards.
    private async Task OpenAsync(string path)
    {
        await Page.GotoAsync(BaseUrl + path);

        if (await WaitForBotCheckToPassAsync())
        {
            await Page.GotoAsync(BaseUrl + path);
        }
    }

    private async Task<bool> WaitForBotCheckToPassAsync()
    {
        var challenge = Page.GetByText(new Regex("Підтвердьте, що ви людина|Verify you are human"));

        // GotoAsync waits for the page to load, so if the check is shown it is already visible.
        if (!await challenge.IsVisibleAsync())
        {
            return false; // no bot check on this page
        }

        try
        {
            await Expect(challenge).ToBeHiddenAsync(new() { Timeout = 20000 });
        }
        catch (PlaywrightException)
        {
            Assert.Ignore("Blocked by the Cloudflare bot check, test skipped.");
        }

        return true;
    }

    private async Task<string> GetFirstProductUrlAsync()
    {
        var handle = await Page.WaitForFunctionAsync(Selectors.FirstProductHrefScript);
        return await handle.JsonValueAsync<string>();
    }

    [Test]
    public async Task HomePage_Loads_AndShowsSearch()
    {
        await OpenAsync("");

        await Expect(Page).ToHaveURLAsync(new Regex(@"rozetka\.com\.ua"));
        await Expect(Page).ToHaveTitleAsync(new Regex("Rozetka|Розетка", RegexOptions.IgnoreCase));
        await Expect(Page.Locator(Selectors.SearchInput)).ToBeVisibleAsync();
    }

    [Test]
    public async Task CategoryPage_MobilePhones_Opens()
    {
        await OpenAsync("mobile-phones/c80003/");

        await Expect(Page.Locator("h1")).ToContainTextAsync("Мобільні телефони");
        Assert.That(await GetFirstProductUrlAsync(), Is.Not.Empty);
    }

    [Test]
    public async Task FooterLink_Contacts_OpensContactsPage()
    {
        await OpenAsync("");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Контакти" }).First.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/contacts/"));
    }
}
