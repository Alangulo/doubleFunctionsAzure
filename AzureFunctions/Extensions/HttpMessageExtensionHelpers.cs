using AzureFunctions.Shared.Dto;
using System;
using System.Net.Http;

namespace AzureFunctions.Extensions
{
    public static class HttpMessageHelperExtensions
    {
        public static T BuildCustomResponseAsync<T>(this HttpResponseMessage httpResponseMessage, string errorMessage = "") where T : DtoOutputBase, new()
        {
            var msg = "Success";

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                if (String.IsNullOrEmpty(errorMessage))
                {
                    msg = $"Failed: {httpResponseMessage.ReasonPhrase}";
                }
                else
                {
                    msg = $"Failed: {errorMessage}. {httpResponseMessage.ReasonPhrase}";
                }
            }
            else
            {
                msg = $"{msg}: {errorMessage}";
            }

            var data =  httpResponseMessage.Content.ReadAsStringAsync().Result;

            var obj = new T
            {
                Data = data,
                Message = msg,
                IsSuccessStatusCode = httpResponseMessage.IsSuccessStatusCode,
                StatusCode = (int)httpResponseMessage.StatusCode
            };
            return obj;
        }
    }
}
