using Notion.Client;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static IMDB_Scraper.Metadata;

namespace IMDB_Scraper
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string NotionToken = ""; // Notion api key
            string NotionDbId = ""; // Notion database id (from url)

            Console.WriteLine("-- Initiate web scraper");
            var options = new ChromeOptions();
            options.AddArgument("--headless=new"); // Make chrome not appear when getting imdb pages
            IWebDriver driver = new ChromeDriver(options);
            driver.Url = "https://www.imdb.com/";

            Console.WriteLine("-- Initiate Notion API");
            NotionClient Notion = NotionClientFactory.Create(new ClientOptions { AuthToken = NotionToken });
            Console.WriteLine();

            List<string> movieNames = await GetItemNames(Notion, NotionDbId);

            foreach (string movieName in movieNames)
            {
                Console.WriteLine("-- Item: " + movieName);
                LocatePage(driver, movieName);
                Metadata data = GetMetadata(driver);
                Page page = await GetPage(Notion, movieName, NotionDbId);
                await EditItem(Notion, page, data);
            }
        }

        /// <summary>
        /// Finds the Notion page of movie/series by the name.
        /// </summary>
        /// <param name="client">Notion API</param>
        /// <param name="movieName">Name of movie or series</param>
        /// <returns></returns>
        public async static Task<Page> GetPage(NotionClient client, string movieName, string databaseId)
        {
            Console.WriteLine("-- -- Finding Notion page of item");
            var database = await client.Databases.QueryAsync(databaseId, new DatabasesQueryParameters());
            for (int i = 0; i < database.Results.Count; i++)
            {
                Page page = database.Results[i];

                TitlePropertyValue properties = (TitlePropertyValue)database.Results[i].Properties["Name"];
                if (properties.Title[0].PlainText == movieName) return page;
            }
            return null;
        }

        /// <summary>
        /// Updates Notion page of movie/series with the metadata scraped from IMDb.
        /// </summary>
        /// <param name="client">Notion API</param>
        /// <param name="page">Page of movie/series</param>
        /// <param name="metadata">New information, scraped from imdb</param>
        /// <returns></returns>
        public async static Task EditItem(NotionClient client, Page page, Metadata metadata)
        {
            Console.WriteLine("-- -- Editing item in Notion");

            // imdb genres
            List<SelectOption> MultiSelect_genres = new List<SelectOption>();
            for (int i = 0; i < metadata.Genres.Count(); i++)
            {
                string genre = metadata.Genres[i];
                MultiSelect_genres.Add(new SelectOption { Name = genre });
            }

            // directors
            List<SelectOption> MultiSelect_dir = new List<SelectOption>();
            for (int i = 0; i < metadata.Directors.Count(); i++)
            {
                string d = metadata.Directors[i];
                MultiSelect_dir.Add(new SelectOption { Name = d });
            }

            // stars
            List<SelectOption> MultiSelect_star = new List<SelectOption>();
            for (int i = 0; i < metadata.Stars.Count(); i++)
            {
                string d = metadata.Stars[i];
                MultiSelect_star.Add(new SelectOption { Name = d });
            }

            Dictionary<string, PropertyValue> vals = new Dictionary<string, PropertyValue>()
                {
                    { "Released (Year)", new RichTextPropertyValue()
                        {
                            RichText = new List<RichTextBase>()
                            {
                                new RichTextText() { Text = new Text() { Content = metadata.Year } }
                            }
                        }
                    },
                    { "Name", new TitlePropertyValue()
                        {
                            Title = new List<RichTextBase>()
                            {
                                new RichTextText() { Text = new Text() { Content = metadata.Name } }
                            }
                        }
                    },
                    { "IMDb Description", new RichTextPropertyValue()
                        {
                            RichText = new List<RichTextBase>()
                            {
                                new RichTextText() { Text = new Text() { Content = metadata.Description } }
                            }
                        }
                    },
                    { "IMDb Link", new UrlPropertyValue()
                        {
                            Url = metadata.URL
                        }
                    },
                    { "Type", new SelectPropertyValue()
                        {
                            Select = new SelectOption
                            {
                                Name = metadata.Type.ToString()
                            }
                        }
                    },
                    { "IMDb Genres", new MultiSelectPropertyValue()
                        {
                            MultiSelect = MultiSelect_genres
                        }
                    },
                    { "Directors", new MultiSelectPropertyValue()
                        {
                            MultiSelect = MultiSelect_dir
                        }
                    },
                    { "Stars", new MultiSelectPropertyValue()
                        {
                            MultiSelect = MultiSelect_star
                        }
                    }
                };

            await client.Pages.UpdatePropertiesAsync(page.Id, vals);
        }

        /// <summary>
        /// Scans
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public async static Task<List<string>> GetItemNames(NotionClient client, string databaseId)
        {
            Console.WriteLine("-- Getting list of all movie/series names");
            List<string> result = new List<string>();
            var database = await client.Databases.QueryAsync(databaseId, new DatabasesQueryParameters());
            for (int i = 0; i < database.Results.Count; i++)
            {
                Page page = database.Results[i];

                // Filter - only "Want to watch" movies/series
                SelectPropertyValue test = (SelectPropertyValue)page.Properties["Status"];
                SelectOption selitem = (SelectOption)test.Select;
                if (selitem.Name != "Want to watch") continue;

                // Filter - only movies/series that are missing some information
                // a.k.a. Only items that include the ⚠️ symbol in the "Year Text" formula in Notion
                FormulaPropertyValue test2 = (FormulaPropertyValue)page.Properties["Year Text"];
                if (!test2.Formula.String.Contains("⚠️")) continue;

                TitlePropertyValue properties = (TitlePropertyValue)page.Properties["Name"];
                Console.WriteLine("-- -- " + properties.Title[0].PlainText);
                result.Add(properties.Title[0].PlainText);
            }
            Console.WriteLine();
            return result;
        }

        /// <summary>
        /// Navigates to page of movie/series by name.
        /// </summary>
        /// <param name="driver">Chrome client</param>
        /// <param name="title">Name of movie/series</param>
        public static void LocatePage(IWebDriver driver, string title)
        {
            Console.WriteLine("-- -- Navigating to imdb page");
            var searchBar = driver.FindElement(By.CssSelector("input#suggestion-search"));
            searchBar.Click();
            searchBar.SendKeys(title);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10)); // Wait for suggestions to load
            wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("a.searchResult--seeAll")));
            var thingToClick = driver.FindElement(By.CssSelector("div#react-autowhatever-navSuggestionSearch > div > ul.react-autosuggest__suggestions-list > li:nth-child(1)"));
            thingToClick.Click();
        }

        /// <summary>
        /// Scrapes data from imdb
        /// </summary>
        /// <param name="driver">Chrome client</param>
        /// <returns></returns>
        public static Metadata GetMetadata(IWebDriver driver)
        {
            Console.WriteLine("-- -- Scraping data from page");
            string name = "";
            string year = "";
            ContentType type = ContentType.Default;
            string description = "";
            string url = driver.Url;
            List<string> genres = new List<string>();
            List<string> directors = new List<string>();
            List<string> writers = new List<string>();
            List<string> stars = new List<string>();

            try
            {
                var classes = driver.FindElements(By.CssSelector("section > div > div[role=presentation] > ul.ipc-metadata-list > li > *:not([class$=icon-link])"));
                decimal metaCount = Math.Floor((decimal)classes.Count() / 2);
                Dictionary<string, List<string>> d = new Dictionary<string, List<string>>();
                for (int i = 0; i < metaCount; i++)
                {
                    int toAdd = i * 2;
                    var people = classes[toAdd + 1];
                    List<string> peopleList = new List<string>();
                    foreach (var p in people.FindElements(By.CssSelector("a")))
                    {
                        peopleList.Add(p.Text);
                    }
                    d.Add(classes[toAdd + 0].GetAttribute("innerHTML"), peopleList);
                }

                foreach (var item in d)
                {
                    if (item.Key.StartsWith("Director") || item.Key.StartsWith("Creator"))
                    {
                        directors = item.Value;
                    }
                    else if (item.Key.StartsWith("Writer"))
                    {
                        writers = item.Value;
                    }
                    else if (item.Key.StartsWith("Star"))
                    {
                        stars = item.Value;
                    }
                }

                var genres_content = driver.FindElements(By.CssSelector("section > div[data-testid=genres] > div > *"));

                foreach (var item in genres_content)
                {
                    if (item.Text != "") genres.Add(item.Text);
                }

                var desc_content = driver.FindElement(By.CssSelector("section > p[data-testid=plot] > span[data-testid=\"plot-xl\"]"));
                description = desc_content.GetAttribute("innerHTML");

                var movie_year = driver.FindElement(By.CssSelector("section.ipc-page-background > section > div:nth-child(2) > div:nth-child(1) > ul > li:nth-child(1)"));
                var series_year = driver.FindElement(By.CssSelector("section.ipc-page-background > section > div:nth-child(2) > div:nth-child(1) > ul > li:nth-child(2)"));

                var stuff1 = movie_year.Text;
                var stuff2 = series_year.Text;

                // On movie pages year is the first element, on serie pages it's the second element,
                // the first being "TV Series". So we're checking if it's that string, that's how we
                // determine if it's a movie or a series.
                if (movie_year.Text != "TV Series")
                {
                    year = movie_year.Text;
                    type = ContentType.Movie;
                }
                else
                {
                    year = series_year.Text;
                    type = ContentType.Series;
                }

                var name_content = driver.FindElement(By.CssSelector("section.ipc-page-background > section > div:nth-child(2) > div:nth-child(1) > h1[data-testid=hero__pageTitle] > span"));
                name = name_content.GetAttribute("innerHTML");
            }
            catch (Exception e) { Console.WriteLine("-- -- -- Exception occurred: " + e.Message); }

            return new Metadata(name, year, type, description, url, genres, directors, writers, stars);
        }
    }

    /// <summary>
    /// Stores data scraped from IMDb
    /// </summary>
    public class Metadata
    {
        public string Name { get; private set; }
        public string Year { get; private set; }
        public ContentType Type { get; private set; }
        public string Description { get; private set; }
        public string URL { get; private set; }
        public List<string> Genres { get; private set; } = new List<string>();
        public List<string> Directors { get; private set; } = new List<string>();
        public List<string> Writers { get; private set; } = new List<string>();
        public List<string> Stars { get; private set; } = new List<string>();

        public Metadata() { }

        public Metadata(string name, string year, ContentType type, string description, string url, List<string> genres, List<string> directors, List<string> writers, List<string> stars)
        {
            Name = name;
            Year = year;
            Type = type;
            Description = description;
            URL = url;

            for (int i = 0; i < genres.Count; i++) Genres.Add(genres[i]);
            for (int i = 0; i < directors.Count; i++) Directors.Add(directors[i]);
            for (int i = 0; i < writers.Count; i++) Writers.Add(writers[i]);
            for (int i = 0; i < stars.Count; i++) Stars.Add(stars[i]);
        }

        public enum ContentType { Movie, Series, Default }
    }
}
