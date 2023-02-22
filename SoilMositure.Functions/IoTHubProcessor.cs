using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using SoilMoisture.Models;
using Newtonsoft.Json;
using System;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Cosmos.Core;

namespace SoilMositure.Functions
{
    public static class IoTHubProcessor
    {
        private static HttpClient client = new HttpClient();

        [FunctionName("IoTHubProcessor")]
        public static void Run([IoTHubTrigger("soilmoisture-iot", Connection = "iothubConn")]EventData message,
            [CosmosDB(
                databaseName:"my-iot-cdb",
                containerName:"dev-data-store",
                Connection ="dbConn"
            )] IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
           // log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.ToArray())}");
            
            // SoilMoistureModel data = JsonConvert.DeserializeObject<SoilMoistureModel>(Encoding.UTF8.GetString(message.Body.Array));
          SoilMoistureModel data = JsonConvert.DeserializeObject<SoilMoistureModel>(Encoding.UTF8.GetString(message.Body.ToArray()));

            log.LogInformation($"Device Name - {data.deviceId} \t Moisture Level - {data.moistureLevel} \t Recorded at - {data.recordedAt.ToLongTimeString()}");


            if (data.moistureLevel > 750)
            {
                SendEmailAsync().Wait();
            }


            documentsOut.AddAsync(
                new
                {
                    id = Guid.NewGuid().ToString(),
                    deviceId = data.deviceId,
                    moistureLevel = data.moistureLevel,
                    recordedAt = data.recordedAt
                }
                    ).Wait();
        }

        public static async Task SendEmailAsync()
        {

            var apiKey = Environment.GetEnvironmentVariable("sendGridApiKey");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("kamalapun22@outlook.com", "Admin");
            var subject = Environment.GetEnvironmentVariable("emailSubject");
            var to = new EmailAddress(Environment.GetEnvironmentVariable("recipientEmail"), "Kamala Pun");
            var plainTextContent = Environment.GetEnvironmentVariable("emailMessage");
            var htmlContent = "";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

        }

    }
}