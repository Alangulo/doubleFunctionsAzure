using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctions.Shared
{
    public static class S_VerifyDeployment
    {
        [FunctionName("S_VerifyDeployment")]
        public static async Task<IActionResult> Run(
             [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            log.LogInformation("S_VerifyDeployment Started.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult(new { status = "successful" })
                : new BadRequestObjectResult("Please pass the endpoint invoker name on the query string or in the request body");
        }


    }


}
