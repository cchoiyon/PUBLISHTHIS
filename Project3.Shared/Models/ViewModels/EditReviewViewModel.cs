using System.ComponentModel.DataAnnotations; 

namespace Project3.Shared.Models.ViewModels
{
    //Edit Review form/action
    public class EditReviewViewModel
    {
      
        [Required]
        public int ReviewId { get; set; }

       
        [Display(Name = "Restaurant")]
        public string RestaurantName { get; set; } 

        // Editable field for the rating
        [Required(ErrorMessage = "Please enter a rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")] // Example range
       
       
        public decimal Rating { get; set; } 

        // Editable field for the comment
        [DataType(DataType.MultilineText)] 
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")] 
        public string Comment { get; set; }

       
    }
}
