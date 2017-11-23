using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Cognitive.CustomVision;
using System.Threading.Tasks;
using Microsoft.Cognitive.CustomVision.Models;

namespace foods
{
    class Program
    {
        private static string[] food = new string[]
        {
            "hamburger", "hash browns", "hotdog", "hummus", "ice cream", "jelly",
            "jam", "jerky", "jalapeño", "kebab", "ketchup", "kiwi", "lobster", "Lamb", "Linguine", "Lasagna",
            "Meatballs", "Milkshake", "Noodles", "Pizza", "Pepperoni", "Pancakes", "Quesadilla", "Quiche", "Spinach",
            "Spaghetti", "Tater tots", "Toast", "Waffles", "Yogurt"
        };

        private const string API_KEY = "API_KEY_HERE";
        private const string ENDPOINT = "https://api.cognitive.microsoft.com/bing/v7.0/images/search";

        // Used to return image search results including relevant headers
        struct SearchResult
        {
            public String jsonResult;
            public Dictionary<String, String> relevantHeaders;
        }

        class FoodClassifier
        {
            private string f;
            private TrainingApi trainingApi;
            private ProjectModel project;

            public FoodClassifier(string _f, TrainingApi _api, ProjectModel pm)
            {
                f = _f;
                trainingApi = _api;
                project = pm;
            }

            public void Classify()
            {
                Console.WriteLine("thread starting for {0}", f);
                WebClient client = new WebClient();
                var thisFoodTag = trainingApi.CreateTag(project.Id, f);
                Console.WriteLine("training for {0}", f);
                SearchResult res = BingImageSearch(f);
                SearchResultJson result = JsonConvert.DeserializeObject<SearchResultJson>(res.jsonResult);
                List<Stream> ms = new List<Stream>();
                Console.WriteLine("downloading {0} images", result.value.Count);
                foreach (var g in result.value)
                {
                    string img = g.thumbnailUrl;
                    /*try
                    {*/
                        Console.WriteLine("url: {0}", img);
                        Stream s = new MemoryStream(client.DownloadData(img));
                        Console.WriteLine("{0} downloaded ok", img);
                        trainingApi.CreateImagesFromData(project.Id, s, new List<String> {thisFoodTag.Id.ToString()});
                        //trainingApi.TrainProject(project.Id);
                    /*}
                    catch (Exception ex)
                    {
                        Console.WriteLine("errored for {0}", img);
                    }*/
                }
                Console.WriteLine("download OK, training");
                
                Console.WriteLine("training done for {0}", f);
            }
        }

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            TrainingApiCredentials trainingCredentials = new TrainingApiCredentials("API_KEY_HERE");
            TrainingApi trainingApi = new TrainingApi(trainingCredentials);
            var project = trainingApi.GetProjects()[1];
            foreach (string f in food)
            {
                FoodClassifier g = new FoodClassifier(f, trainingApi, project);
                System.Threading.Thread t = new System.Threading.Thread(g.Classify);
                t.Start();
                System.Threading.Thread.Sleep(10000);
            }

            Console.Write("\nPress Enter to exit ");
            Console.ReadLine();
        }

        /// <summary>
        /// Performs a Bing Image search and return the results as a SearchResult.
        /// </summary>
        static SearchResult BingImageSearch(string searchQuery)
        {
            // Construct the URI of the search request
            var uriQuery = ENDPOINT + "?q=" + Uri.EscapeDataString(searchQuery);

            // Perform the Web request and get the response
            WebRequest request = HttpWebRequest.Create(uriQuery);
            request.Headers["Ocp-Apim-Subscription-Key"] = API_KEY;
            HttpWebResponse response = (HttpWebResponse)request.GetResponseAsync().Result;
            string json = new StreamReader(response.GetResponseStream()).ReadToEnd();

            // Create result object for return
            var searchResult = new SearchResult()
            {
                jsonResult = json,
                relevantHeaders = new Dictionary<String, String>()
            };

            // Extract Bing HTTP headers
            foreach (String header in response.Headers)
            {
                if (header.StartsWith("BingAPIs-") || header.StartsWith("X-MSEdge-"))
                    searchResult.relevantHeaders[header] = response.Headers[header];
            }

            return searchResult;
        }

        /// <summary>
        /// Formats the given JSON string by adding line breaks and indents.
        /// </summary>
        /// <param name="json">The raw JSON string to format.</param>
        /// <returns>The formatted JSON string.</returns>
        static string JsonPrettyPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            json = json.Replace(Environment.NewLine, "").Replace("\t", "");

            StringBuilder sb = new StringBuilder();
            bool quote = false;
            bool ignore = false;
            char last = ' ';
            int offset = 0;
            int indentLength = 2;

            foreach (char ch in json)
            {
                switch (ch)
                {
                    case '"':
                        if (!ignore) quote = !quote;
                        break;
                    case '\\':
                        if (quote && last != '\\') ignore = true;
                        break;
                }

                if (quote)
                {
                    sb.Append(ch);
                    if (last == '\\' && ignore) ignore = false;
                }
                else
                {
                    switch (ch)
                    {
                        case '{':
                        case '[':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', ++offset * indentLength));
                            break;
                        case '}':
                        case ']':
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', --offset * indentLength));
                            sb.Append(ch);
                            break;
                        case ',':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', offset * indentLength));
                            break;
                        case ':':
                            sb.Append(ch);
                            sb.Append(' ');
                            break;
                        default:
                            if (quote || ch != ' ') sb.Append(ch);
                            break;
                    }
                }
                last = ch;
            }

            return sb.ToString().Trim();
        }
    }
}
