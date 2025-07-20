using QuizOnline.Models;
using System.Collections.Generic;

namespace QuizOnline.ViewModels
{
    public class QuizDetailsViewModel
    {
        public Quiz Quiz { get; set; } = null!;

        public Dictionary<int, int> AnswerSelectionStats { get; set; } = new Dictionary<int, int>();

        public Dictionary<int, int> QuestionTotalAnswers { get; set; } = new Dictionary<int, int>();

        public int TotalParticipants { get; set; }
        public double AverageScore { get; set; }
    }
}