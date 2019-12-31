using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Amazon.S3.Util;
using System.Collections.Generic;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AwsDotnetCsharp
{
    public class Handler
    {
        private IAmazonS3 S3Client { get; set; }
        private readonly string[] _supportedImageTypes = new string[] { ".png", ".jpg", ".jpeg" };

        public Handler()
        {
            S3Client = new AmazonS3Client();
        }

        public Handler(IAmazonS3 s3Client)
        {
            S3Client = s3Client;
        }

        public async Task S3ThumbnailGeneratorAsync(S3Event evnt, ILambdaContext context)
        {
            try
            {
                foreach (var record in evnt.Records)
                {
                    // Validate extention
                    if (!_supportedImageTypes.Contains(Path.GetExtension(record.S3.Object.Key).ToLower()))
                    {
                        Console.WriteLine("File extention is not supported - " + record.S3.Object.Key);
                        continue;
                    }

                    // Validating if image has already been compressed
                    if (await CheckCompressedTagAsync(record))
                    {
                        Console.WriteLine($"Image {record.S3.Bucket.Name}:{record.S3.Object.Key} has already been compressed");
                        continue;
                    }

                    // Resize the image according to the size set in the environment variables
                    var imageStream = await ResizeImageAsync(record);

                    // Put in the same S3 bucket the image resized ending in "_thumbnail"
                    await PutResizedObjectAsync(record, imageStream);
                }

                Console.WriteLine("Thumbnail has been created");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        private async Task<bool> CheckCompressedTagAsync(S3EventNotification.S3EventNotificationRecord record) =>
            (await S3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
            {
                BucketName = record.S3.Bucket.Name,
                Key = record.S3.Object.Key
            })).Tagging.Any(tag => tag.Key == "Compressed" && tag.Value == "true");

        private async Task PutResizedObjectAsync(S3EventNotification.S3EventNotificationRecord record, Stream imageStream) =>
            await S3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = record.S3.Bucket.Name,
                Key = $"{Path.GetFileNameWithoutExtension(record.S3.Object.Key)}_thumbnail{Path.GetExtension(record.S3.Object.Key)}",
                InputStream = imageStream,
                TagSet = new List<Tag>
                {
                    new Tag
                    {
                        Key = "Compressed",
                        Value = "true"
                    }
                }
            });

        private async Task<Stream> ResizeImageAsync(S3EventNotification.S3EventNotificationRecord record)
        {
            var imageSize = int.Parse(Environment.GetEnvironmentVariable("THUMBNAIL_SIZE"));

            Stream imageStream = new MemoryStream();
            using (var objectResponse = await S3Client.GetObjectAsync(record.S3.Bucket.Name, record.S3.Object.Key))
            using (Stream responseStream = objectResponse.ResponseStream)
            {
                using (Image<Rgba32> image = Image.Load(responseStream))
                {
                    image.Mutate(ctx => ctx.Resize(imageSize, imageSize));
                    image.Save(imageStream, new JpegEncoder());
                    imageStream.Seek(0, SeekOrigin.Begin);
                }
            }

            return imageStream;
        }
    }
}
