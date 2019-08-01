using AzureFunctions.Extensions;
using AzureFunctions.Shared;
using AzureFunctions.Shared.StarRez.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SA.Azure.Storage;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AzureFunctions.StarRez
{
    public static class StarRez_S_GetReportByIdAndFormat
    {
       [FunctionName("StarRez_S_GetReportByIdAndFormat")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("----- StarRez Azure Function Started -----");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var reportNameOrId = (string)data?.reportNameOrId;
            var fileFormat = (string)data?.fileFormat;
            var blobName = (string)data?.blobName;
            var blobExtension = (string)data?.blobExtension;
            var blobDirectory = (string)data?.blobDirectory;
            var parameters = (object)data?.parameters;

            log.LogInformation($"Report Name: {reportNameOrId}");
            log.LogInformation($"File Format: {fileFormat}");
            log.LogInformation($"Blob Name: {blobName}");
            log.LogInformation($"Blob Extension: {blobExtension}");
            log.LogInformation($"Blob Path: {blobDirectory}");
            log.LogInformation($"Parameters: {parameters}");

            var url = $"{Environment.GetEnvironmentVariable("STARREZ_ENDPOINT_BASEPATH")}/getReport/{reportNameOrId}.{fileFormat}";

            if (parameters != null)
            {
                dynamic paramData = JsonConvert.DeserializeObject(parameters.ToString());
                url += "?";
                var counter = 0;
                foreach (JProperty property in paramData.Properties())
                {
                    if (counter == 0)
                    {
                        url += property.Name + "=" + property.Value;
                    }
                    else
                    {
                        url += "&" + property.Name + "=" + property.Value;
                    }

                    counter++;
                }
            }

            
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", Environment.GetEnvironmentVariable("STARREZ_AUTHORIZATION_KEY"));

            var response = await httpClient.GetAsync(url);
            var contentType = response.Content.Headers.ContentType.ToString();
            var content = await response.Content.ReadAsStringAsync();
            log.LogInformation($"StarRez content: {content.ToString()}");


            string accountName = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME");
            string accessKey = Environment.GetEnvironmentVariable("STORAGE_ACCESS_KEY");           
 
            var storage = new StorageFactory(accountName, accessKey);
            var pathNotFoundMessage = "";
            var container = storage.CreateBlobStorageInstance(AppConstants.Interfaces.ROOT_PATH);
            var containerExists = await container.ContainerExists();

            // Valid path
            if (containerExists && !String.IsNullOrEmpty(blobDirectory))
            {
                string blobId = blobName + "." + blobExtension;
                var blobRef = container.GetBlockBlobReferenceFromDirectory(blobId, blobDirectory);

                blobRef.Properties.ContentType = contentType;
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                {
                    await blobRef.UploadFromStreamAsync(stream);
                }

                pathNotFoundMessage = "Report was copied to blob successfully.";
            }
            else if (String.IsNullOrEmpty(blobDirectory)) //Empty path
            {
                pathNotFoundMessage = "Blob path was not specified.";
            }
            else //Wrong path
            {
                pathNotFoundMessage = "Container does not exist.";
            }
                       

            var output =  response.BuildCustomResponseAsync<GetReportByIdAndFormatOutput>(pathNotFoundMessage);
            return new ObjectResult(output);
        }


    }
}
