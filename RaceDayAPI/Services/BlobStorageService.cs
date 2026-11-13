using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayAPI.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _bannerContainer;
        private readonly BlobContainerClient _profileContainer;

        public BlobStorageService(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("AzureBlobStorage")
                ?? throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing.");

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            _bannerContainer = blobServiceClient.GetBlobContainerClient("event-banners");
            _profileContainer = blobServiceClient.GetBlobContainerClient("profile-pictures");
        }

        public async Task<bool> TestConnectionAsync()
        {
            await _bannerContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);

            return await _bannerContainer.ExistsAsync();
        }

        public Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType)
            => UploadAsync(_bannerContainer, imageStream, fileName, contentType);

        public Task<string> UploadProfilePictureAsync(Stream imageStream, string fileName, string contentType)
            => UploadAsync(_profileContainer, imageStream, fileName, contentType);

        private static async Task<string> UploadAsync(
            BlobContainerClient container,
            Stream imageStream,
            string fileName,
            string contentType)
        {
            // Make sure the container exists (and is readable by the browser) before the first upload.
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            string uniqueFileName = $"{Guid.NewGuid()}-{Path.GetFileName(fileName)}";

            BlobClient blobClient = container.GetBlobClient(uniqueFileName);

            await blobClient.UploadAsync(
                imageStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                });

            return blobClient.Uri.ToString();
        }
    }
}
