using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Project3.Shared.Models.Domain;

namespace Project3.Shared.Utilities
{
    /// <summary>
    /// Custom XML serializer for restaurant entities
    /// </summary>
    public class SimpleRestaurantSerializer
    {
        /// <summary>
        /// Marshals restaurant object to XML format
        /// </summary>
        public string Serialize(Restaurant restaurant)
        {
            if (restaurant == null)
                throw new ArgumentNullException(nameof(restaurant));
                
            StringBuilder xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<Restaurant>");
            
            // Write restaurant properties
            xml.AppendLine($"  <ID>{restaurant.RestaurantID}</ID>");
            xml.AppendLine($"  <Name>{EscapeXml(restaurant.Name)}</Name>");
            xml.AppendLine($"  <Address>{EscapeXml(restaurant.Address)}</Address>");
            xml.AppendLine($"  <City>{EscapeXml(restaurant.City)}</City>");
            xml.AppendLine($"  <State>{EscapeXml(restaurant.State)}</State>");
            xml.AppendLine($"  <ZipCode>{EscapeXml(restaurant.ZipCode)}</ZipCode>");
            xml.AppendLine($"  <Cuisine>{EscapeXml(restaurant.Cuisine)}</Cuisine>");
            xml.AppendLine($"  <Hours>{EscapeXml(restaurant.Hours)}</Hours>");
            xml.AppendLine($"  <Contact>{EscapeXml(restaurant.Contact)}</Contact>");
            xml.AppendLine($"  <MarketingDescription>{EscapeXml(restaurant.MarketingDescription)}</MarketingDescription>");
            xml.AppendLine($"  <WebsiteURL>{EscapeXml(restaurant.WebsiteURL)}</WebsiteURL>");
            xml.AppendLine($"  <SocialMedia>{EscapeXml(restaurant.SocialMedia)}</SocialMedia>");
            xml.AppendLine($"  <Owner>{EscapeXml(restaurant.Owner)}</Owner>");
            xml.AppendLine($"  <ProfilePhoto>{EscapeXml(restaurant.ProfilePhoto)}</ProfilePhoto>");
            xml.AppendLine($"  <LogoPhoto>{EscapeXml(restaurant.LogoPhoto)}</LogoPhoto>");
            xml.AppendLine($"  <CreatedDate>{restaurant.CreatedDate:yyyy-MM-ddTHH:mm:ss}</CreatedDate>");
            
            xml.AppendLine("</Restaurant>");
            
            return xml.ToString();
        }
        
        /// <summary>
        /// Demarshals XML to restaurant object
        /// </summary>
        public Restaurant Deserialize(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                throw new ArgumentNullException(nameof(xml));
                
            Restaurant restaurant = new Restaurant();
            
            // Parse without XML DOM processing
            restaurant.RestaurantID = ExtractIntValue(xml, "ID");
            restaurant.Name = ExtractStringValue(xml, "Name");
            restaurant.Address = ExtractStringValue(xml, "Address");
            restaurant.City = ExtractStringValue(xml, "City");
            restaurant.State = ExtractStringValue(xml, "State");
            restaurant.ZipCode = ExtractStringValue(xml, "ZipCode");
            restaurant.Cuisine = ExtractStringValue(xml, "Cuisine");
            restaurant.Hours = ExtractStringValue(xml, "Hours");
            restaurant.Contact = ExtractStringValue(xml, "Contact");
            restaurant.MarketingDescription = ExtractStringValue(xml, "MarketingDescription");
            restaurant.WebsiteURL = ExtractStringValue(xml, "WebsiteURL");
            restaurant.SocialMedia = ExtractStringValue(xml, "SocialMedia");
            restaurant.Owner = ExtractStringValue(xml, "Owner");
            restaurant.ProfilePhoto = ExtractStringValue(xml, "ProfilePhoto");
            restaurant.LogoPhoto = ExtractStringValue(xml, "LogoPhoto");
            
            string createdDateStr = ExtractStringValue(xml, "CreatedDate");
            if (DateTime.TryParse(createdDateStr, out DateTime createdDate))
            {
                restaurant.CreatedDate = createdDate;
            }
            else
            {
                restaurant.CreatedDate = DateTime.Now;
            }
            
            return restaurant;
        }
        
        /// <summary>
        /// Serializes and writes restaurant to file
        /// </summary>
        public void ExportToFile(Restaurant restaurant, string filePath)
        {
            string xml = Serialize(restaurant);
            File.WriteAllText(filePath, xml);
        }
        
        /// <summary>
        /// Reads and deserializes restaurant from file
        /// </summary>
        public Restaurant ImportFromFile(string filePath)
        {
            string xml = File.ReadAllText(filePath);
            return Deserialize(xml);
        }
        
        // Utility methods
        private string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
        
        private string ExtractStringValue(string xml, string tagName)
        {
            string startTag = $"<{tagName}>";
            string endTag = $"</{tagName}>";
            
            int startPos = xml.IndexOf(startTag);
            if (startPos < 0)
                return null;
                
            startPos += startTag.Length;
            
            int endPos = xml.IndexOf(endTag, startPos);
            if (endPos < 0)
                return null;
                
            string value = xml.Substring(startPos, endPos - startPos);
            
            // Unescape XML entities
            return value
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'");
        }
        
        private int ExtractIntValue(string xml, string tagName)
        {
            string value = ExtractStringValue(xml, tagName);
            if (int.TryParse(value, out int result))
                return result;
                
            return 0;
        }
    }
} 