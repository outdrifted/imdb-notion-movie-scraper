# imdb-notion-movie-scraper
A C# console app (.NET Framework) that gets information about a list of movies/series from IMDb, then updates Notion page with the gathered new information.
Although the code works at the time of writing, this isn't a complete solution, mostly just to look at the code and use it as an example to make your own system.

How it works:
* Using Notion API, the app gets movie names from existing pages.
  * Filters out movies
    * Only updates movies/series that have "Want to Watch" selected as the Status.
    * Only updates movies/series that have the "⚠️" symbol in the "Year Text" formula property in Notion (meaning the movie/series is lacking some information)
* For each movie name, it goes to its imdb page and gets the required data.
* Finds the page of the movie/series in Notion and updates properties with gathered data from IMDb

Notes:
* Since this is a web scraper, IMDb site elements might change, so it might be needed to update the CSS Selectors.
* Since the Notion property names are hardcoded, so they must match exactly in Notion. I might make a template someday if needed.
