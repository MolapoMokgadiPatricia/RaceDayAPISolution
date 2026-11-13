namespace RaceDayAPI.Services
{
    /// <summary>
    /// Shared checks for uploaded images: size, content type, extension and the real file signature.
    /// </summary>
    public static class ImageValidator
    {
        public const long MaxFileSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        /// <summary>Returns an error message, or null when the file is acceptable.</summary>
        public static async Task<string?> ValidateAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return "Please select an image to upload.";
            }

            if (file.Length > MaxFileSize)
            {
                return "The image must not exceed 5 MB.";
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                return "Only JPEG, PNG and WebP images are allowed.";
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            string expectedExtension = file.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => string.Empty
            };

            if (extension != expectedExtension &&
                !(file.ContentType == "image/jpeg" && extension == ".jpeg"))
            {
                return "The image file extension does not match its content type.";
            }

            // Check the real file signature, not only the name and type the client sent.
            byte[] header = new byte[12];
            int read;
            await using (var stream = file.OpenReadStream())
            {
                read = await stream.ReadAsync(header, 0, header.Length);
            }

            if (read < 12)
            {
                return "The file is not a valid image.";
            }

            bool isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
            bool isWebp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                          header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

            return (isJpeg || isPng || isWebp) ? null : "The file is not a valid image.";
        }
    }
}
