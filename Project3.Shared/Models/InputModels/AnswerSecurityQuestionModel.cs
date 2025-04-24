namespace Project3.Shared.Models.InputModels // Updated namespace to match project structure
{
    
    [Serializable]
    public class AnswerSecurityQuestionModel
    {
        // Private backing fields
        private string _userId; // To identify the user
        private int _questionNumber; // Which question (1, 2, or 3) is being answered
        private string _questionText; // The text of the question being asked
        private string _answer; // The answer provided by the user

        // Public property for User ID (might be passed via hidden field or TempData)
        public string UserId // Or int
        {
            get { return _userId; }
            set { _userId = value; }
        }

        // Public property for the Question Number (might be passed via hidden field)
        public int QuestionNumber
        {
            get { return _questionNumber; }
            set { _questionNumber = value; }
        }

        // Public property for the Question Text (displayed to the user)
        public string QuestionText
        {
            get { return _questionText; }
            set { _questionText = value; } // Set by the controller before showing the view
        }

        // Public property for the Answer provided by the user (from form input)
        // NOTE: Controller should HASH this before validating against stored hash
        public string Answer
        {
            get { return _answer; }
            set { _answer = value; }
        }

        // Parameterless constructor
        public AnswerSecurityQuestionModel() { }

        // Optional: Parameterized constructor
        public AnswerSecurityQuestionModel(string userId, int questionNumber, string questionText, string answer)
        {
            _userId = userId;
            _questionNumber = questionNumber;
            _questionText = questionText;
            _answer = answer;
        }
    }
}
