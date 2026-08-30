namespace WAMS.Infrastructure.Extensions;

using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Files;
using WAMS.Domain.Constants;
using WAMS.Infrastructure.Services;
using WAMS.Infrastructure.Services.Files;

public static class ObjectStorageServiceCollectionExtensions
{
    public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ObjectStorageOptions.SectionName);
        var endpoint = section["Endpoint"];

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            var opts = section.Get<ObjectStorageOptions>() ?? new ObjectStorageOptions();

            if (string.IsNullOrWhiteSpace(opts.BucketName))
                throw new InvalidOperationException(ErrorMessages.ObjectStorage.BucketNameRequired);
            if (string.IsNullOrWhiteSpace(opts.AccessKey))
                throw new InvalidOperationException(ErrorMessages.ObjectStorage.AccessKeyRequired);
            if (string.IsNullOrWhiteSpace(opts.SecretKey))
                throw new InvalidOperationException(ErrorMessages.ObjectStorage.SecretKeyRequired);

            services.Configure<ObjectStorageOptions>(section);
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var config = new AmazonS3Config
                {
                    ServiceURL = opts.Endpoint,
                    ForcePathStyle = opts.ForcePathStyle,
                    AuthenticationRegion = opts.Region
                };
                return new AmazonS3Client(opts.AccessKey, opts.SecretKey, config);
            });
            services.AddSingleton<IFileAttachmentStorage, S3FileAttachmentStorage>();
        }
        else
        {
            services.AddSingleton<IFileAttachmentStorage, LocalFileAttachmentStorage>();
        }

        return services;
    }
}
