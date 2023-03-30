using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using OpenAI_API.Chat;

namespace SeaTheEssay.AI
{
    public static class OpenAICaller
    {
        [FunctionName("CallOpenAI")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string apiKey = Environment.GetEnvironmentVariable("apiKey");

            log.LogInformation("Going to call OpenAI.");

            //get post data
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            var essay = (string)data?.essay;
            var guide = (string)data?.guide;
            var system = (string)data?.system;

            log.LogInformation("Essay: " + essay);
            log.LogInformation("Guide: " + guide);
            log.LogInformation("System: " + system);


            //call open ai
            var response = await DoPromptAsync(essay, guide, system, apiKey);
            
            return new OkObjectResult(response);
        }

        public static async Task<GptResponse> DoPromptAsync(string essayText, string guide, string system, 
            string apiKey)
        {

            var openai = new OpenAI_API.OpenAIAPI(apiKey);

            
            string text = StripNonAlphanumeric(essayText);
            string prompt = guide + text + " |||";
            //string model = apiVersion == "3" ? "gpt-3.5-turbo" : "gpt-4";
            string model =  "gpt-4";

            var chatMessages = new List<ChatMessage>
            {
                new ChatMessage { Role = ChatMessageRole.System, Content = system },
                new ChatMessage { Role = ChatMessageRole.User, Content = prompt }
            };

            var chatRequest = new ChatRequest{
                 Messages = chatMessages, 
                 Model = model, 
                 MaxTokens = 2067, 
                 Temperature = 0, TopP = 1, 
                 FrequencyPenalty = 0.5, 
                 PresencePenalty = 0, 
                 StopSequence = "|||" };
            
            var chatResponse = await openai.Chat.CreateChatCompletionAsync(chatRequest);

            string result = chatResponse.Choices[0].Message.Content;

            var gptResponse = new GptResponse { Result = result };

            try
            {
                dynamic json = JsonConvert.DeserializeObject(result);

                gptResponse.Grade = json.Grade;
                gptResponse.Comments = json.Comments;
                gptResponse.Improvements = json.Improvements;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return gptResponse;
        }

        //Strip non alphanumeric 
        private static string StripNonAlphanumeric(string text)
        {
            // Replace this with the actual implementation of the StripNonAlphanumeric method
            return text;
        }
    }


    public class GptResponse
    {
        public string Grade { get; set; }
        public string Comments { get; set; }
        public string Improvements { get; set; }
        public string Result { get; set; }
    }

}
