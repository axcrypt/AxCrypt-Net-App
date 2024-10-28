namespace AxCrypt.App.Components.Models
{
    public class FeedbackViewModel
    {
        public FeedbackViewModel()
        {
            AllSubject = Enum.GetValues(typeof(FeedbackSubject))
                 .Cast<FeedbackSubject>()
                 .ToList();
        }

        public List<FeedbackSubject> AllSubject { get; private set; }
    }

    public enum FeedbackSubject
    {
        Idea,
        Question,
        Problem,
        Praise,
    }
}