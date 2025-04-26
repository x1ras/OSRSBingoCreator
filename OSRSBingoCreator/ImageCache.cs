using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace OSRSBingoCreator
{
    // Provides image caching capabilities to improve performance.
    public class ImageCache
    {
        private readonly Dictionary<string, Image> _cache = new Dictionary<string, Image>();

        // Retrieve an image from the cache.
        public Image RetrieveImage(string path)
        {
            if (_cache.TryGetValue(path, out var image))
            {
                if (ValidateImage(image))
                {
                    return image;
                }
                else
                {
                    // Remove corrupted image from cache
                    _cache.Remove(path);
                    Debug.WriteLine($"Corrupted image removed from cache: {path}");
                }
            }
            return null;
        }

        // Add an image to the cache.
        public void CacheImage(string path, Image image)
        {
            if (ValidateImage(image))
            {
                _cache[path] = image;
            }
            else
            {
                throw new ArgumentException("The provided image is invalid or corrupted.");
            }
        }

        // Validate that an image is not corrupted.
        private bool ValidateImage(Image image)
        {
            try
            {
                var width = image.Width;
                var height = image.Height;
                var format = image.RawFormat;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
