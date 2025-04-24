namespace Project3.Shared.Models.Domain
{
    [Serializable]
    public class Restaurant
    {
        // Backing fields
        private int _restaurantID;
        private string _name;
        private string _address;
        private string _city;
        private string _state;
        private string _zipCode;
        private string _cuisine;
        private string _hours;
        private string _contact;
        private string _profilePhoto;
        private string _logoPhoto;
        private string _marketingDescription;
        private string _websiteURL;
        private string _socialMedia;
        private string _owner;
        private DateTime _createdDate;

        // Properties
        public int RestaurantID
        {
            get { return _restaurantID; }
            set { _restaurantID = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Address
        {
            get { return _address; }
            set { _address = value; }
        }

        public string City
        {
            get { return _city; }
            set { _city = value; }
        }

        public string State
        {
            get { return _state; }
            set { _state = value; }
        }

        public string ZipCode
        {
            get { return _zipCode; }
            set { _zipCode = value; }
        }

        public string Cuisine
        {
            get { return _cuisine; }
            set { _cuisine = value; }
        }

        public string Hours
        {
            get { return _hours; }
            set { _hours = value; }
        }

        public string Contact
        {
            get { return _contact; }
            set { _contact = value; }
        }

        public string ProfilePhoto
        {
            get { return _profilePhoto; }
            set { _profilePhoto = value; }
        }

        public string LogoPhoto
        {
            get { return _logoPhoto; }
            set { _logoPhoto = value; }
        }

        public string MarketingDescription
        {
            get { return _marketingDescription; }
            set { _marketingDescription = value; }
        }

        public string WebsiteURL
        {
            get { return _websiteURL; }
            set { _websiteURL = value; }
        }

        public string SocialMedia
        {
            get { return _socialMedia; }
            set { _socialMedia = value; }
        }

        public string Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }

        public DateTime CreatedDate
        {
            get { return _createdDate; }
            set { _createdDate = value; }
        }

        // Default constructor
        public Restaurant()
        {
            _createdDate = DateTime.Now;
        }

        // Constructor with parameters
        public Restaurant(string name, string address, string city, string state, string zipCode,
                          string cuisine, string hours, string contact, string profilePhoto,
                          string logoPhoto, string marketingDescription, string websiteURL,
                          string socialMedia, string owner)
        {
            _name = name;
            _address = address;
            _city = city;
            _state = state;
            _zipCode = zipCode;
            _cuisine = cuisine;
            _hours = hours;
            _contact = contact;
            _profilePhoto = profilePhoto;
            _logoPhoto = logoPhoto;
            _marketingDescription = marketingDescription;
            _websiteURL = websiteURL;
            _socialMedia = socialMedia;
            _owner = owner;
            _createdDate = DateTime.Now;
        }
    }
}