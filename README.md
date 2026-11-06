# Rozetka smoke tests (C# / .NET / Playwright / NUnit)

Three UI smoke tests for https://rozetka.com.ua/ua/:

1. Home page loads and shows the search field
2. Category page (mobile phones) opens and shows products
3. Footer link "Контакти" opens the contacts page

## Requirements

- .NET SDK 8.0 or newer
- PowerShell (needed once to install Playwright browsers)
- Google Chrome (used by `playwright.runsettings`)

## Run

```bash
dotnet restore
dotnet build

# install Playwright browsers once
# (use "powershell" instead of "pwsh" if PowerShell 7 is not installed)
pwsh bin/Debug/net8.0/playwright.ps1 install

# run tests (headless Chromium)
dotnet test

# optional: run in a visible real Chrome (requires Google Chrome installed)
dotnet test --settings playwright.runsettings
```

## Notes

- Selectors are kept in the `Selectors` class in `RozetkaSmokeTests.cs`.
- The tests run against the live site. Rozetka is protected by Cloudflare, which
  sometimes shows a "verify you are human" check to automated browsers. The tests
  wait for the check to pass, and skip the test if the site keeps blocking it.
- Tests that depend on search results and product pages were left out on purpose:
  in an automated browser the site's search page returned a 500 error, so such
  tests would be unreliable.
