using System;

namespace Project3.Shared.Models.Domain
{
    /// <summary>
    /// Represents a photo stored in the FileStorage system
    /// </summary>
    [Serializable]
    public class Photo
    {
        // Backing fields
        private int _photoId;
        private int _restaurantId;
        private string _photoURL;
        private string _caption;
        private DateTime _uploadedDate;

        // Properties
        public int PhotoId
        {
            get { return _photoId; }
            set { _photoId = value; }
        }

        public int RestaurantId
        {
            get { return _restaurantId; }
            set { _restaurantId = value; }
        }

        public string PhotoURL
        {
            get { return _photoURL; }
            set { _photoURL = value; }
        }

        public string Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }

        public DateTime UploadedDate
        {
            get { return _uploadedDate; }
            set { _uploadedDate = value; }
        }

        // Default constructor
        public Photo()
        {
            _uploadedDate = DateTime.Now;
            _photoURL = string.Empty;
            _caption = string.Empty;
        }

        // Constructor with parameters
        public Photo(int restaurantId, string photoURL, string caption = null)
        {
            _restaurantId = restaurantId;
            _photoURL = photoURL;
            _caption = caption ?? string.Empty;
            _uploadedDate = DateTime.Now;
        }
    }
} 