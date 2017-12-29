using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace TollBooth
{
    public class FindLicensePlateText
    {
        private readonly HttpClient _client;

        public FindLicensePlateText(HttpMessageHandler messageHandler = null)
        {
            _client = messageHandler == null ? new HttpClient() : new HttpClient(messageHandler);
        }

        public async Task<string> GetLicensePlate(byte[] imageBytes)
        {
            return await MakeOCRRequest(imageBytes);
        }

        private async Task<string> MakeOCRRequest(byte[] imageBytes)
        {
            var licensePlate = string.Empty;
            // Request parameters.
            const string requestParameters = "language=unk&detectOrientation=true";
            // Get the API URL and the API key from settings.
            var uriBase = ConfigurationManager.AppSettings["textSentimentApiUrl"];
            var apiKey = ConfigurationManager.AppSettings["textSentimentApiKey"];

            // Configure the HttpClient request headers.
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Assemble the URI for the REST API Call.
            var uri = uriBase + "?" + requestParameters;

            using (var content = new ByteArrayContent(imageBytes))
            {
                // Add application/octet-stream header for the content.
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                HttpResponseMessage response = await _client.PostAsync(uri, content);

                // Get the JSON response.
                var result = await response.Content.ReadAsAsync<OCRResult>();
                licensePlate = GetLicensePlateTextFromResult(result);
            }

            return licensePlate;
        }

        /// <summary>
        /// Applies a bit of logic to strip out extraneous text from the OCR
        /// data, like State names and invalid characters.
        /// </summary>
        /// <param name="result">The extracted text.</param>
        /// <returns></returns>
        private string GetLicensePlateTextFromResult(OCRResult result)
        {
            var text = new StringBuilder();
            if (result.Regions == null || result.Regions.Length == 0) return string.Empty;

            const string states = "ALABAMA,ALASKA,ARIZONA,ARKANSAS,CALIFORNIA,COLORADO,CONNECTICUT,DELAWARE,FLORIDA,GEORGIA,HAWAII,IDAHO,ILLINOIS,INDIANA,IOWA,KANSAS,KENTUCKY,LOUISIANA,MAINE,MARYLAND,MASSACHUSETTS,MICHIGAN,MINNESOTA,MISSISSIPPI,MISSOURI,MONTANA,NEBRASKA,NEVADA,NEW HAMPSHIRE,NEW JERSEY,NEW MEXICO,NEW YORK,NORTH CAROLINA,NORTH DAKOTA,OHIO,OKLAHOMA,OREGON,PENNSYLVANIA,RHODE ISLAND,SOUTH CAROLINA,SOUTH DAKOTA,TENNESSEE,TEXAS,UTAH,VERMONT,VIRGINIA,WASHINGTON,WEST VIRGINIA,WISCONSIN,WYOMING";
            string[] chars = { ",", ".", "/", "!", "@", "#", "$", "%", "^", "&", "*", "'", "\"", ";", "_", "(", ")", ":", "|", "[", "]" };
            var stateList = states.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // We are only interested in the first region found, and only the first two lines within the region.
            foreach (var line in result.Regions[0].Lines.Take(2))
            {
                // Exclude the state name.
                if (stateList.Contains(line.Words[0].Text.ToUpper())) continue;
                foreach (var word in line.Words)
                {
                    if (!string.IsNullOrWhiteSpace(word.Text))
                        text.Append(RemoveSpecialCharacters(word.Text)).Append(" "); // Spaces are valid in a license plate.
                }
            }

            return text.ToString().ToUpper().Trim();
        }

        /// <summary>
        /// Fast method to remove invalid special characters from the
        /// license plate text.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string RemoveSpecialCharacters(string str)
        {
            var buffer = new char[str.Length];
            int idx = 0;

            foreach (var c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')
                    || (c >= 'a' && c <= 'z') || (c == '-'))
                {
                    buffer[idx] = c;
                    idx++;
                }
            }

            return new string(buffer, 0, idx);
        }
    }

    public class OCRResult
    {
        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; }
        [JsonProperty(PropertyName = "textAngle")]
        public float TextAngle { get; set; }
        [JsonProperty(PropertyName = "orientation")]
        public string Orientation { get; set; }
        [JsonProperty(PropertyName = "regions")]
        public Region[] Regions { get; set; }
    }

    public class Region
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public string BoundingBox { get; set; }
        [JsonProperty(PropertyName = "lines")]
        public Line[] Lines { get; set; }
    }

    public class Line
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public string BoundingBox { get; set; }
        [JsonProperty(PropertyName = "words")]
        public Word[] Words { get; set; }
    }

    public class Word
    {
        [JsonProperty(PropertyName = "boundingBox")]
        public string BoundingBox { get; set; }
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }

}
