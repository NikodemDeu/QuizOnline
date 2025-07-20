using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizOnline.Models;

public class AttemptAnswer
{
    [Key]
    public int Id { get; set; }

    public int QuizAttemptId { get; set; }

    [ForeignKey("QuizAttemptId")]
    public QuizAttempt QuizAttempt { get; set; } = null!; 

    public int SelectedAnswerId { get; set; }

    [ForeignKey("SelectedAnswerId")]
    public Answer SelectedAnswer { get; set; } = null!; 
}