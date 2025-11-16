namespace ContestApi.Services;

 public class EmailOptions
    {
        public string FromAddress { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? AcsConnectionString { get; set; }
    }
