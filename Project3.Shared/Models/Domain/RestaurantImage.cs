using System;

namespace Project3.Shared.Models.Domain
{
    [Serializable]
    public class RestaurantImage
    {
        // Backing fields
        private int _imageID;
        private int _restaurantID;
        private string _imagePath;
        private string _caption;
        private DateTime _uploadDate;
        private int _displayOrder;

        // Properties
        public int ImageID
        {
            get { return _imageID; }
            set { _imageID = value; }
        }

        public int RestaurantID
        {
            get { return _restaurantID; }
            set { _restaurantID = value; }
        }

        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; }
        }

        public string Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }

        public DateTime UploadDate
        {
            get { return _uploadDate; }
            set { _uploadDate = value; }
        }

        public int DisplayOrder
        {
            get { return _displayOrder; }
            set { _displayOrder = value; }
        }

        // Default constructor
        public RestaurantImage()
        {
            _uploadDate = DateTime.Now;
            _displayOrder = 0;
        }

        // Constructor with parameters
        public RestaurantImage(int restaurantID, string imagePath, string caption)
        {
            _restaurantID = restaurantID;
            _imagePath = imagePath;
            _caption = caption;
            _uploadDate = DateTime.Now;
            _displayOrder = 0;
        }
    }
} 