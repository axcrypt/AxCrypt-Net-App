using AxCrypt.Api.Model;

namespace AxCrypt.App.Components.Models;

public class SupportModel
{
    public SubscriptionLevel SubscriptionLevel { get; set; }

    public string PageTitle { get; set; } = "";

    public string PageDescription { get; set; } = "";

    public string LinkLabel { get; set; } = "";

    public string SupportInfo { get; set; } = "";

    public string Feedback { get; set; } = "";

    public bool SubmittedSuccess { get; set; } = false;

    public string Body { get; set; } = "";

    public string Subject { get; set; }
}