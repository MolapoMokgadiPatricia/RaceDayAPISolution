namespace RaceDayAPI.Services
{
    /// <summary>
    /// Stores uploaded images in Azure Blob Storage (or the Azurite emulator) and returns their URL.
    /// An interface is used so the controllers can be unit tested without a real storage account.
    /// </summary>
    public interface IBlobStorageService
    {
        Task<bool> TestConnectionAsync();

        Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType);

        Task<string> UploadProfilePictureAsync(Stream imageStream, string fileName, string contentType);
    }
}
