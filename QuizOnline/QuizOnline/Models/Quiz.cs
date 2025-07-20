using System.ComponentModel.DataAnnotations;
using QuizOnline.Models;

namespace QuizOnline.Models;

public class Quiz 
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tytuł quizu jest wymagany.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Tytuł musi mieć od 3 do 150 znaków.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

  
    public string UserId { get; set; } = string.Empty;
   

 
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
