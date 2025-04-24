using System;

namespace Project3.Shared.Models.ViewModels
{
    public class ErrorViewModel
    {
        private string _requestId;
        private bool _showRequestId;
        private string _errorMessage;
        
        public string RequestId 
        { 
            get { return _requestId; } 
            set { _requestId = value; } 
        }
        
        public bool ShowRequestId 
        { 
            get { return _showRequestId; } 
            set { _showRequestId = value; } 
        }
        
        public string ErrorMessage 
        { 
            get { return _errorMessage; } 
            set { _errorMessage = value; } 
        }

        public ErrorViewModel()
        {
        }
    }
}
