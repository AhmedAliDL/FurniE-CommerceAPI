namespace App.Application.Common.Models
{
    public class EmailContent
    {
        public string Title { get; set; } = string.Empty;
        public string? GreetingName { get; set; }
        public string Intro { get; set; } = string.Empty;
        public IReadOnlyList<EmailDetail> Details { get; set; } = [];
        public string? Highlight { get; set; }
        public string? Note { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public string? Outro { get; set; }
    }

    public record EmailDetail(string Label, string Value);
}
