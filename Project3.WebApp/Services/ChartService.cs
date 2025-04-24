using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Project3.WebApp.Services
{
    public class ChartService
    {
        // Generate a bar chart showing rating distribution
        public byte[] GenerateRatingDistributionChart(Dictionary<string, int> ratingCounts, string title)
        {
            int width = 700;
            int height = 400;
            int margin = 50;
            int barSpacing = 30;
            
            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Set up the graphics object
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                
                // Draw title
                using (Font titleFont = new Font("Arial", 16, FontStyle.Bold))
                {
                    g.DrawString(title, titleFont, Brushes.Black, 
                        new RectangleF(0, 10, width, 30), 
                        new StringFormat { Alignment = StringAlignment.Center });
                }
                
                // Define chart area
                Rectangle chartArea = new Rectangle(margin, margin + 20, width - (margin * 2), height - (margin * 2) - 20);
                
                // Find the maximum value for scaling
                int maxValue = 1; // Default to 1 to avoid division by zero
                foreach (var count in ratingCounts.Values)
                {
                    maxValue = Math.Max(maxValue, count);
                }
                
                // Draw axes
                using (Pen axisPen = new Pen(Color.Black, 2))
                {
                    // Y axis
                    g.DrawLine(axisPen, chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom);
                    // X axis
                    g.DrawLine(axisPen, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom);
                }
                
                // Draw Y-axis labels
                using (Font labelFont = new Font("Arial", 10))
                {
                    for (int i = 0; i <= 5; i++)
                    {
                        int value = (maxValue * i) / 5;
                        float y = chartArea.Bottom - ((float)i / 5) * chartArea.Height;
                        g.DrawString(value.ToString(), labelFont, Brushes.Black, 
                            new PointF(chartArea.Left - 30, y - 6));
                        
                        // Draw gridline
                        using (Pen gridPen = new Pen(Color.LightGray, 1))
                        {
                            gridPen.DashStyle = DashStyle.Dash;
                            g.DrawLine(gridPen, chartArea.Left, y, chartArea.Right, y);
                        }
                    }
                }
                
                // Calculate bar width
                int barCount = ratingCounts.Count;
                float barWidth = (chartArea.Width - (barSpacing * (barCount + 1))) / barCount;
                
                // Draw bars and X-axis labels
                int barIndex = 0;
                using (Font labelFont = new Font("Arial", 10))
                {
                    foreach (var entry in ratingCounts)
                    {
                        string category = entry.Key;
                        int value = entry.Value;
                        
                        // Calculate bar position
                        float x = chartArea.Left + barSpacing + barIndex * (barWidth + barSpacing);
                        float barHeight = ((float)value / maxValue) * chartArea.Height;
                        
                        // Cap bar height to chart area
                        barHeight = Math.Min(barHeight, chartArea.Height);
                        
                        // Draw the bar
                        using (Brush barBrush = GetBarBrush(barIndex))
                        {
                            g.FillRectangle(barBrush, x, chartArea.Bottom - barHeight, barWidth, barHeight);
                            g.DrawRectangle(Pens.Black, x, chartArea.Bottom - barHeight, barWidth, barHeight);
                        }
                        
                        // Draw bar value above the bar
                        g.DrawString(value.ToString(), labelFont, Brushes.Black, 
                            new PointF(x + (barWidth / 2) - 10, chartArea.Bottom - barHeight - 20));
                        
                        // Draw x-axis label
                        g.DrawString(category, labelFont, Brushes.Black, 
                            new PointF(x + (barWidth / 2) - 15, chartArea.Bottom + 10));
                        
                        barIndex++;
                    }
                }
                
                // Save the chart as a PNG
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
        
        // Generate a pie chart showing data distribution
        public byte[] GeneratePieChart(Dictionary<string, int> data, string title)
        {
            int width = 600;
            int height = 600;
            int margin = 50;
            
            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Set up the graphics object
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                
                // Draw title
                using (Font titleFont = new Font("Arial", 16, FontStyle.Bold))
                {
                    g.DrawString(title, titleFont, Brushes.Black, 
                        new RectangleF(0, 10, width, 30), 
                        new StringFormat { Alignment = StringAlignment.Center });
                }
                
                // Define chart area
                Rectangle chartArea = new Rectangle(
                    margin, margin + 20, 
                    width - (margin * 2), height - (margin * 2) - 20);
                
                // Calculate total value for percentage calculations
                int total = 0;
                foreach (var count in data.Values)
                {
                    total += count;
                }
                
                if (total == 0) total = 1; // Avoid division by zero
                
                // Start angle
                float currentAngle = 0;
                int index = 0;
                
                // Draw each pie slice
                foreach (var entry in data)
                {
                    string category = entry.Key;
                    int value = entry.Value;
                    
                    // Calculate percentage and angle
                    float percentage = (float)value / total;
                    float sweepAngle = percentage * 360f;
                    
                    // Draw pie slice
                    using (Brush sliceBrush = GetPieBrush(index))
                    {
                        g.FillPie(sliceBrush, chartArea, currentAngle, sweepAngle);
                        g.DrawPie(Pens.Black, chartArea, currentAngle, sweepAngle);
                    }
                    
                    // Calculate the position for the label
                    double midAngle = (currentAngle + (sweepAngle / 2)) * Math.PI / 180;
                    float labelRadius = chartArea.Width / 2 * 0.75f;
                    float labelX = chartArea.X + (chartArea.Width / 2) + (float)(Math.Cos(midAngle) * labelRadius);
                    float labelY = chartArea.Y + (chartArea.Height / 2) + (float)(Math.Sin(midAngle) * labelRadius);
                    
                    // Draw the percentage label inside the pie slice
                    using (Font labelFont = new Font("Arial", 10, FontStyle.Bold))
                    {
                        if (sweepAngle > 10) // Only show percentage for larger slices
                        {
                            string percentLabel = $"{percentage:P0}";
                            g.DrawString(percentLabel, labelFont, Brushes.Black, 
                                labelX - 15, labelY - 7.5f);
                        }
                    }
                    
                    currentAngle += sweepAngle;
                    index++;
                }
                
                // Draw the legend
                int legendX = width - 120;
                int legendY = margin + 30;
                int legendItemHeight = 25;
                
                using (Font legendFont = new Font("Arial", 10))
                {
                    index = 0;
                    foreach (var entry in data)
                    {
                        string category = entry.Key;
                        int value = entry.Value;
                        
                        // Draw legend color box
                        using (Brush brush = GetPieBrush(index))
                        {
                            g.FillRectangle(brush, legendX, legendY + (index * legendItemHeight), 15, 15);
                            g.DrawRectangle(Pens.Black, legendX, legendY + (index * legendItemHeight), 15, 15);
                        }
                        
                        // Draw legend text
                        g.DrawString($"{category} ({value})", legendFont, Brushes.Black, 
                            legendX + 20, legendY + (index * legendItemHeight));
                        
                        index++;
                    }
                }
                
                // Save the chart as a PNG
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
        
        // Generate a line chart for trend data
        public byte[] GenerateLineChart(Dictionary<string, int> data, string title)
        {
            int width = 700;
            int height = 400;
            int margin = 50;
            
            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Set up the graphics object
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                
                // Draw title
                using (Font titleFont = new Font("Arial", 16, FontStyle.Bold))
                {
                    g.DrawString(title, titleFont, Brushes.Black, 
                        new RectangleF(0, 10, width, 30), 
                        new StringFormat { Alignment = StringAlignment.Center });
                }
                
                // Define chart area
                Rectangle chartArea = new Rectangle(margin, margin + 20, width - (margin * 2), height - (margin * 2) - 20);
                
                // Find the maximum value for scaling
                int maxValue = 1; // Default to 1 to avoid division by zero
                foreach (var count in data.Values)
                {
                    maxValue = Math.Max(maxValue, count);
                }
                
                // Round max value to next nice number (multiple of 5 or 10) for better looking charts
                maxValue = ((maxValue / 5) + 1) * 5;
                
                // Draw axes
                using (Pen axisPen = new Pen(Color.Black, 2))
                {
                    // Y axis
                    g.DrawLine(axisPen, chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom);
                    // X axis
                    g.DrawLine(axisPen, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom);
                }
                
                // Draw Y-axis labels
                using (Font labelFont = new Font("Arial", 10))
                {
                    for (int i = 0; i <= 5; i++)
                    {
                        int value = (maxValue * i) / 5;
                        float y = chartArea.Bottom - ((float)i / 5) * chartArea.Height;
                        g.DrawString(value.ToString(), labelFont, Brushes.Black, 
                            new PointF(chartArea.Left - 30, y - 6));
                        
                        // Draw gridline
                        using (Pen gridPen = new Pen(Color.LightGray, 1))
                        {
                            gridPen.DashStyle = DashStyle.Dash;
                            g.DrawLine(gridPen, chartArea.Left, y, chartArea.Right, y);
                        }
                    }
                }
                
                // Draw the line chart
                int pointCount = data.Count;
                if (pointCount > 1)
                {
                    float pointSpacing = chartArea.Width / (pointCount - 1);
                    Point[] points = new Point[pointCount];
                    
                    // Prepare points array
                    int pointIndex = 0;
                    foreach (var entry in data)
                    {
                        int value = entry.Value;
                        int x = chartArea.Left + (int)(pointIndex * pointSpacing);
                        int y = chartArea.Bottom - (int)(((float)value / maxValue) * chartArea.Height);
                        
                        points[pointIndex] = new Point(x, y);
                        pointIndex++;
                    }
                    
                    // Draw the line
                    using (Pen linePen = new Pen(Color.Blue, 3))
                    {
                        g.DrawLines(linePen, points);
                    }
                    
                    // Draw the points
                    int markerSize = 8;
                    using (Brush markerBrush = Brushes.Red)
                    {
                        foreach (var point in points)
                        {
                            g.FillEllipse(markerBrush, point.X - markerSize/2, point.Y - markerSize/2, markerSize, markerSize);
                            g.DrawEllipse(Pens.Black, point.X - markerSize/2, point.Y - markerSize/2, markerSize, markerSize);
                        }
                    }
                    
                    // Draw the values above points
                    pointIndex = 0;
                    using (Font valueFont = new Font("Arial", 10, FontStyle.Bold))
                    {
                        foreach (var entry in data)
                        {
                            g.DrawString(entry.Value.ToString(), valueFont, Brushes.Black, 
                                points[pointIndex].X - 10, points[pointIndex].Y - 25);
                            pointIndex++;
                        }
                    }
                }
                
                // Draw X-axis labels
                int labelIndex = 0;
                float labelSpacing = chartArea.Width / (pointCount - 1);
                using (Font labelFont = new Font("Arial", 9))
                {
                    foreach (var entry in data)
                    {
                        string category = entry.Key;
                        float x = chartArea.Left + (labelIndex * labelSpacing);
                        
                        // Draw the label
                        using (StringFormat format = new StringFormat() { Alignment = StringAlignment.Center })
                        {
                            g.DrawString(category, labelFont, Brushes.Black, 
                                new RectangleF(x - 40, chartArea.Bottom + 5, 80, 20), format);
                        }
                        
                        labelIndex++;
                    }
                }
                
                // Save the chart as a PNG
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
        
        // Generate a bar chart showing data distribution
        public byte[] GenerateBarChart(Dictionary<string, int> data, string title)
        {
            // The implementation is actually the same as GenerateRatingDistributionChart but with a different method name
            return GenerateRatingDistributionChart(data, title);
        }
        
        // Helper method to get different brush colors for bar charts
        private Brush GetBarBrush(int index)
        {
            switch (index % 6)
            {
                case 0: return new SolidBrush(Color.FromArgb(65, 105, 225));  // Royal Blue
                case 1: return new SolidBrush(Color.FromArgb(50, 205, 50));   // Lime Green
                case 2: return new SolidBrush(Color.FromArgb(255, 99, 71));   // Tomato
                case 3: return new SolidBrush(Color.FromArgb(75, 0, 130));    // Indigo
                case 4: return new SolidBrush(Color.FromArgb(255, 165, 0));   // Orange
                case 5: return new SolidBrush(Color.FromArgb(106, 90, 205));  // Slate Blue
                default: return Brushes.Gray;
            }
        }
        
        // Helper method to get different brush colors for pie charts
        private Brush GetPieBrush(int index)
        {
            switch (index % 8)
            {
                case 0: return new SolidBrush(Color.FromArgb(65, 105, 225));  // Royal Blue
                case 1: return new SolidBrush(Color.FromArgb(50, 205, 50));   // Lime Green
                case 2: return new SolidBrush(Color.FromArgb(255, 99, 71));   // Tomato
                case 3: return new SolidBrush(Color.FromArgb(75, 0, 130));    // Indigo
                case 4: return new SolidBrush(Color.FromArgb(255, 165, 0));   // Orange
                case 5: return new SolidBrush(Color.FromArgb(106, 90, 205));  // Slate Blue
                case 6: return new SolidBrush(Color.FromArgb(220, 20, 60));   // Crimson
                case 7: return new SolidBrush(Color.FromArgb(34, 139, 34));   // Forest Green
                default: return Brushes.Gray;
            }
        }
    }
} 