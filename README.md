# IMDb-Notion Movies/Series Scraper

A C# app that gets information about movies/series from IMDb, then updates Notion page with the gathered new information.

### Prerequisites to run the code

- Google Chrome - You can change it in the code to Firefox or other browsers, when initializing Selenium.
- Visual Studio (not Code). Installer: ".NET desktop development", Template: "Console App (.NET Framework)" 
- Selenium.WebDriver & DotNetSeleniumExtras.WaitHelpers - Install through Visual Studio NuGet Package Manager

### How it works

- Using Notion API, the app gets all page names (movie or series names) from existing pages in a database.
    - Only updates movies/series that have "Want to Watch" selected as the Status.
    - Only updates movies/series that have the "⚠️" symbol in the "Year Text" formula property in Notion (meaning the movie/series is lacking some information).
- For each movie name, it goes to its IMDb page and gets the required data.
- Updates properties of movie/series page with gathered data from IMDb.

### Notion Setup

To use this app, your Notion database MUST have these properties (since the Notion property names are hard-coded, so they must match exactly in Notion):
[Imgur](https://imgur.com/5cqnmCq)

The “Year Text” formula property has this code:
```jsx
if(empty(prop("Released (Year)")) or empty(prop("Status")) or empty(prop("IMDb Genres")) or empty(prop("Directors")) and not (prop("Type") == "Series") or empty(prop("IMDb Link")) or empty(prop("IMDb Description")) or empty(prop("Stars")), format(prop("Released (Year)")) + " ⚠️", format(prop("Released (Year)")))
```
What this code does is it shows the movie year and adds the “⚠️” symbol when any information is missing. The C# app then searches for movies with that symbol to update them.

### Notes

- Since this is a web scraper, IMDb site elements might change, so it might be needed to update the CSS Selectors.
