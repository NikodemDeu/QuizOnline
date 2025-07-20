using QuizOnline.Models;

namespace QuizOnline.ViewModels
{
    public class AttemptDetailsViewModel
    {
        public QuizAttempt Attempt { get; set; } = null!;
        public int UserPoints { get; set; }
        public int MaxPossiblePoints { get; set; }
    }
}