namespace FftDataAnalyzer.Models
{
    /// <summary>
    /// Configuration for API settings
    /// </summary>
    public class ApiConfig
    {
        public string BaseUrl { get; set; }
        public int TimeoutSeconds { get; set; }
        public string AuthToken { get; set; }
        public bool UseHttps { get; set; }

        public ApiConfig()
        {
            TimeoutSeconds = 30;
            UseHttps = true;
        }
    }
}
