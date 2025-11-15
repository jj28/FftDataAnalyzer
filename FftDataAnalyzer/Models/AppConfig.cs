namespace FftDataAnalyzer.Models
{
    /// <summary>
    /// Application configuration settings
    /// </summary>
    public class AppConfig
    {
        public string ConnectionString { get; set; }
        public string UploadPath { get; set; }
        public string SuccessPath { get; set; }
        public string FailPath { get; set; }
        public string StagingPath { get; set; }
        public int RetentionDays { get; set; }
        public ApiConfig ApiSettings { get; set; }

        public AppConfig()
        {
            UploadPath = @"C:\FFT\Upload";
            SuccessPath = @"C:\FFT\Upload\Success";
            FailPath = @"C:\FFT\Upload\Fail";
            StagingPath = @"C:\FFT\Upload\Staging";
            RetentionDays = 30;
            ApiSettings = new ApiConfig();
        }
    }
}
