using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Newtonsoft.Json;
using System;

[assembly:LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AwsDotnetCsharp
{
    public class Handler
    {
        IAmazonS3 S3Client { get; set; }

        public Handler()
        {
            S3Client = new AmazonS3Client();
        }

        public Handler(IAmazonS3 s3Client)
        {
            S3Client = s3Client;
        }

        public Response S3ThumbnailGeneratorAsync(S3Event evnt, ILambdaContext context)
        {
            Console.WriteLine("Event: " + JsonConvert.SerializeObject(evnt));
            Console.WriteLine("Context: " + JsonConvert.SerializeObject(context));

            return new Response("Ingrid");
        }
    }

    public class Response
    {
      public string Message {get; set;}
      public Request Request {get; set;}

      public Response(string message){
        Message = message;
      }
    }

    public class Request
    {
      public string Key1 {get; set;}
      public string Key2 {get; set;}
      public string Key3 {get; set;}

      public Request(string key1, string key2, string key3){
        Key1 = key1;
        Key2 = key2;
        Key3 = key3;
      }
    }
}
