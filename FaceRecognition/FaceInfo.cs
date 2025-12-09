using FaceRecognitionDotNet;
using System;
using System.Drawing;

namespace FaceRecognitionApp
{
    public class FaceInfo
    {
        public string ImagePath { get; private set; }
        public Location Location { get; private set; }  
        public FaceEncoding Encoding { get; private set; }

        public FaceInfo(string imagePath, Location location, FaceEncoding encoding)
        {
            ImagePath = imagePath;
            Location = location;
            Encoding = encoding;
        }
    }
}