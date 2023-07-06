# imdb-notion-movie-scraper
A C# console app (.NET Framework) that gets information about a list of movies/series from IMDb, then updates Notion page with the gathered new information.
Although the code works at the time of writing, this isn't a complete solution, mostly just to look at the code and use it as an example to make your own system.

Notes:
* Since this is a web scraper, IMDb site HTML elements might change, so it might be needed to update the CSS Selectors.
* Since the Notion property names are hardcoded, so they must match exactly in Notion. I might make a template someday if needed.
