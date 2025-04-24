using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.InputModels
{
    public class SecurityQuestionResetModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; }
        
        [Display(Name = "Security Question 1")]
        public string SecurityQuestion1 { get; set; }
        
        [Display(Name = "Answer 1")]
        public string SecurityAnswer1 { get; set; }
        
        [Display(Name = "Security Question 2")]
        public string SecurityQuestion2 { get; set; }
        
        [Display(Name = "Answer 2")]
        public string SecurityAnswer2 { get; set; }
        
        [Display(Name = "Security Question 3")]
        public string SecurityQuestion3 { get; set; }
        
        [Display(Name = "Answer 3")]
        public string SecurityAnswer3 { get; set; }
    }
} 