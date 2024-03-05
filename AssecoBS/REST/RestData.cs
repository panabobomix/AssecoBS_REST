using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace AssecoBS.REST
{
    public class RestData
    {
        private const string clientId = "XXX";
        private const string clientSecret = "XXX";
        private readonly ILogger<RestData> _logger;
        
        public RestData(ILogger<RestData> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Download Token 
        /// </summary>
        /// <returns></returns>
        public async Task<string> DownloadToken()
        {
            try
            {

                HttpClient client = new HttpClient();


                string authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {authValue}");

         
                var postData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "USERAPI")
        };

            
                var content = new FormUrlEncodedContent(postData);
                HttpResponseMessage response = await client.PostAsync("https://oauth2.assecobs.pl/api/oauth2/token", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
                    _logger.LogInformation("Pobrano token");
                    return tokenResponse.access_token;

                }
                return null;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                throw;

            }

        }
        /// <summary>
        /// GetXMLData using token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<List<string>> GetXMLData(string token)
        {
            try
            {
                HttpClient client = new HttpClient();

   
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Pobranie pliku WADL
                HttpResponseMessage wadlResponse = await client.GetAsync("https://portalcloudapi-test.assecobs.pl/?wadl&DBC=rest");

         
                if (wadlResponse.IsSuccessStatusCode)
                {
           
                    string wadlContent = await wadlResponse.Content.ReadAsStringAsync();

                    // zapisanie zawartości pliku WADL
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wadlcontent.xml");
                    File.WriteAllText(filePath, wadlContent);
                    _logger.LogInformation("Zapisano plik xml");

                    // Wyodrębnienie wartości atrybutów "path"
                    List<string> paths = ExtractPathsFromXML(wadlContent);
                    if (paths.Count > 0)
                    {
                        return paths;
                    }
                    else
                    {
                        _logger.LogError($"Nie znaleziono path");
                        throw new ArgumentException($"Nie znaleziono path");
                    }



                }
                else
                {
                    _logger.LogError($"Wystąpił błąd. Status code: {wadlResponse.StatusCode}");

                    throw new ArgumentException($"Wystąpił błąd. Status code: {wadlResponse.StatusCode}");
                }


            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd podczas pobierania XML: {ex.Message}");
                throw new ArgumentException($"Błąd podczas pobierania XML: {ex.Message}");
            }
        }
        /// <summary>
        /// Parse and extract XML
        /// </summary>
        /// <param name="xmlContent"></param>
        /// <returns></returns>
        public  List<string> ExtractPathsFromXML(string xmlContent)
        {
            List<string> paths = new List<string>();

            try
            {
           
                XDocument doc = XDocument.Parse(xmlContent);

                IEnumerable<XElement> resourceElements = doc.Descendants("{http://wadl.dev.java.net/2009/02}resource");

                foreach (var resourceElement in resourceElements)
                {
                    string path = resourceElement.Attribute("path")?.Value;
                    if (!string.IsNullOrEmpty(path) && !path.Contains("/"))
                    {
                        paths.Add(path);
                        Console.WriteLine($"{path}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Wystąpił błąd podczas parsowania XML: {ex.Message}");
            }

            return paths;
        }

    }
}
