using System;
using System.Collections.Generic; // For List
using System.ComponentModel.DataAnnotations; // For Display attribute
namespace Project3.Shared.Models.ViewModels
{
    /// <summary>
    /// ViewModel for holding search criteria from the form.
    /// All fields are optional to allow flexible searching.
    /// </summary>
    [Serializable]
    public class SearchCriteriaViewModel
    {
        // Private backing fields
        private string _cuisineInput; // Single cuisine (to be replaced with multiple)
        private List<string> _selectedCuisines; // Multiple cuisines
        private string _city;
        private string _state;
        private List<string> _availableCuisines;

        // Public properties - all optional with no validation
        [Display(Name = "Cuisine Type")]
        public string CuisineInput 
        { 
            get { return _cuisineInput ?? string.Empty; } 
            set { _cuisineInput = value?.Trim(); } 
        }

        [Display(Name = "Cuisine Types")]
        public List<string> SelectedCuisines
        {
            get { return _selectedCuisines ?? (_selectedCuisines = new List<string>()); }
            set { _selectedCuisines = value ?? new List<string>(); }
        }

        [Display(Name = "City")]
        public string City 
        { 
            get { return _city ?? string.Empty; } 
            set { _city = value?.Trim(); } 
        }

        [Display(Name = "State")]
        public string State 
        { 
            get { return _state ?? string.Empty; } 
            set { _state = value?.Trim()?.ToUpper(); } 
        }

        public List<string> AvailableCuisines 
        { 
            get { return _availableCuisines ?? (_availableCuisines = new List<string>()); }
            set { _availableCuisines = value ?? new List<string>(); } 
        }

        // Constructor
        public SearchCriteriaViewModel() 
        {
            _availableCuisines = new List<string>();
            _selectedCuisines = new List<string>();
            _cuisineInput = string.Empty;
            _city = string.Empty;
            _state = string.Empty;
        }
    }
}
