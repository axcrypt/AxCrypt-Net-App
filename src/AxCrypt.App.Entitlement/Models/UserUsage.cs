namespace AxCrypt.App.Entitlement.Models
{
    public class UsageLimit
    {
        public int UsedCount { get; set; }
        public int MaxCount { get; set; }
        public string ValidationMessage { get; set; }
    }
}