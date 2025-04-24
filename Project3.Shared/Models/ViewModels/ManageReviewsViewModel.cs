// Make sure this using statement is correct if ReviewDto is needed (it is for the List)
using Project3.Shared.Models.DTOs;
using System.Collections.Generic;


namespace Project3.Shared.Models.ViewModels
{
    // ---> Verify this class name exactly matches public class ManageReviewsViewModel <---
    public class ManageReviewsViewModel
    {
        // Property for the list of reviews
        public List<ReviewDto> Reviews { get; set; }

        // Constructor to initialize the list
        public ManageReviewsViewModel()
        {
            Reviews = new List<ReviewDto>();
        }
    }
}
