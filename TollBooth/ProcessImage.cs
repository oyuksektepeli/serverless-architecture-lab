using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace TollBooth
{
    public static class ProcessImage
    {
        static string storageAccountConnectionString = System.Environment.GetEnvironmentVariable("myBlobStorage_STORAGE");

        [FunctionName("ProcessImage")]
        public static async Task Run(EventGridEvent uploadEvent, Stream incomingPlate, TraceWriter log)
        {
            string licensePlateText = string.Empty;
            log.Info(uploadEvent.ToString());

            // Get the name of the uploaded image from the event's data.
            var imageName = GetBlobNameFromUrl((string) uploadEvent.Data["url"]);
            // Convert the incoming image stream to a byte array.
            using (var br = new BinaryReader(incomingPlate))
            {
                var licensePlateImage = br.ReadBytes((int)incomingPlate.Length);
                licensePlateText = await new FindLicensePlateText().GetLicensePlate(licensePlateImage);
            }

            // Send the details to Event Grid.
        }

        private static string GetBlobNameFromUrl(string bloblUrl)
        {
            var myUri = new Uri(bloblUrl);
            var myCloudBlob = new CloudBlob(myUri);
            return myCloudBlob.Name;
        }
    }
}
