namespace AxCrypt.App.Shared.Models;

public class ManageAccountModel
{
    public ManageAccountModel(string email, string timestamp)
    {
        EmailAddress = email;
        Timestamp = timestamp;
    }

    public string EmailAddress { get; set; }

    public string Timestamp { get; set; }
}