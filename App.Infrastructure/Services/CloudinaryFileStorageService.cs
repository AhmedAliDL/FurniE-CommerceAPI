using App.Application.Common.Interfaces.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Services
{
    public class CloudinaryFileStorageService(Cloudinary cloudinary, IConfiguration configuration, ILogger<CloudinaryFileStorageService> logger) : IFileStorageService
    {
        private const string DefaultFolder = "product-images";
        private const string UploadMarker = "/upload/";

        private string Folder =>
            configuration["Cloudinary:Folder"] ?? DefaultFolder;

        public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, content),
                Folder = Folder,
                Overwrite = false,
                UniqueFilename = true
            };

            var uploadResult = await cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");

            var url = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("Cloudinary upload did not return a URL.");

            logger.LogInformation("Uploaded image to Cloudinary. PublicId: {PublicId}", uploadResult.PublicId);
            return url;
        }

        public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var publicId = ExtractPublicId(fileUrl);
                if (publicId is null)
                {
                    logger.LogWarning("Could not extract a Cloudinary public id from {FileUrl}; skipping delete.", fileUrl);
                    return Task.CompletedTask;
                }

                var destroyResult = cloudinary.Destroy(new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                });

                if (destroyResult.Error != null)
                    logger.LogWarning("Cloudinary delete failed for {PublicId}: {Message}", publicId, destroyResult.Error.Message);
            }
            catch (Exception ex)
            {
                // Orphaned images are removed manually; a delete failure must not fail the request.
                logger.LogWarning(ex, "Failed to delete Cloudinary image {FileUrl}.", fileUrl);
            }

            return Task.CompletedTask;
        }

        private static string? ExtractPublicId(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return null;

            var url = fileUrl.Split('?', '#')[0];
            var markerIndex = url.IndexOf(UploadMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                return null;

            var tail = url[(markerIndex + UploadMarker.Length)..];
            var segments = tail.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var skip = segments.Length > 0 && segments[0].StartsWith('v') ? 1 : 0;
            if (segments.Length <= skip)
                return null;

            var publicId = string.Join("/", segments.Skip(skip));

            // Strip the delivery format extension from the last segment.
            var lastSlash = publicId.LastIndexOf('/');
            var file = publicId[(lastSlash + 1)..];
            var dot = file.LastIndexOf('.');
            if (dot > 0)
                publicId = publicId[..(lastSlash + 1 + dot)];

            return publicId;
        }
    }
}
