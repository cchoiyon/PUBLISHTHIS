using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Project3.Shared.Models.Domain;
using Project3.Shared.Utilities;

namespace Project3.Shared.Repositories
{
    public class PhotoRepository
    {
        private readonly Connection _dbConnection;
        private readonly ILogger<PhotoRepository> _logger;

        public PhotoRepository(Connection dbConnection, ILogger<PhotoRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        /// <summary>
        /// Gets all photos for a specific restaurant
        /// </summary>
        /// <param name="restaurantId">ID of the restaurant</param>
        /// <returns>List of photos</returns>
        public List<Photo> GetRestaurantPhotos(int restaurantId)
        {
            var photos = new List<Photo>();
            
            try
            {
                var cmd = new SqlCommand("TP_spGetRestaurantPhotos");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                
                var ds = _dbConnection.GetDataSetUsingCmdObj(cmd);
                
                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        photos.Add(MapRowToPhoto(row));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting photos for restaurant with ID {RestaurantId}", restaurantId);
            }
            
            return photos;
        }

        /// <summary>
        /// Adds a new photo for a restaurant
        /// </summary>
        /// <param name="photo">Photo to add</param>
        /// <returns>ID of the newly added photo</returns>
        public int AddPhoto(Photo photo)
        {
            try
            {
                var cmd = new SqlCommand("TP_spAddRestaurantPhoto");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", photo.RestaurantId);
                cmd.Parameters.AddWithValue("@PhotoURL", photo.PhotoURL);
                cmd.Parameters.AddWithValue("@Caption", (object)photo.Caption ?? DBNull.Value);
                
                var result = _dbConnection.ExecuteScalarUsingCmdObj(cmd);
                if (result != null && int.TryParse(result.ToString(), out int photoId))
                {
                    return photoId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding photo for restaurant with ID {RestaurantId}", photo.RestaurantId);
            }
            
            return 0;
        }

        /// <summary>
        /// Updates a photo's caption
        /// </summary>
        /// <param name="photoId">ID of the photo to update</param>
        /// <param name="caption">New caption (or null to keep existing)</param>
        /// <returns>Updated photo or null if update failed</returns>
        public Photo UpdatePhotoCaption(int photoId, string caption)
        {
            try
            {
                var cmd = new SqlCommand("TP_spUpdatePhotoCaption");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PhotoID", photoId);
                cmd.Parameters.AddWithValue("@Caption", (object)caption ?? DBNull.Value);
                
                var ds = _dbConnection.GetDataSetUsingCmdObj(cmd);
                
                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return MapRowToPhoto(ds.Tables[0].Rows[0]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating photo with ID {PhotoId}", photoId);
            }
            
            return null;
        }

        /// <summary>
        /// Deletes a photo and returns its URL for file deletion
        /// </summary>
        /// <param name="photoId">ID of the photo to delete</param>
        /// <returns>URL of the deleted photo or null if photo not found</returns>
        public string DeletePhoto(int photoId)
        {
            try
            {
                var cmd = new SqlCommand("TP_spDeletePhoto");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PhotoID", photoId);
                
                var ds = _dbConnection.GetDataSetUsingCmdObj(cmd);
                
                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var photoURL = ds.Tables[0].Rows[0]["PhotoURL"]?.ToString();
                    return photoURL;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo with ID {PhotoId}", photoId);
            }
            
            return null;
        }

        /// <summary>
        /// Maps a DataRow to a Photo object
        /// </summary>
        private Photo MapRowToPhoto(DataRow row)
        {
            return new Photo
            {
                PhotoId = Convert.ToInt32(row["PhotoID"]),
                RestaurantId = Convert.ToInt32(row["RestaurantID"]),
                PhotoURL = row["PhotoURL"].ToString(),
                Caption = row["Caption"]?.ToString(),
                UploadedDate = Convert.ToDateTime(row["UploadedDate"])
            };
        }
    }
} 