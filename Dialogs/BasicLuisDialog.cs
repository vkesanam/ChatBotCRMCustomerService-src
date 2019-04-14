using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

namespace Microsoft.Bot.Sample.LuisBot
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        public string customerName;
        public string email;
        public string phone;
        public string complaint;
        static string host = "https://api.microsofttranslator.com";
        static string path = "/V2/Http.svc/Translate";

        // NOTE: Replace this example key with a valid subscription key.
        static string key = "830fda84bdce4810a78cc508745a2f9e";

        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"], 
            ConfigurationManager.AppSettings["LuisAPIKey"], 
            domain: ConfigurationManager.AppSettings["LuisAPIHostName"])))
        {
        }
        private async Task<string> Translation(string text)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
            string uri = host + path + "?from=ar-ae&to=en-us&text=" + System.Net.WebUtility.UrlEncode(text);

            HttpResponseMessage response = await client.GetAsync(uri);

            string result = await response.Content.ReadAsStringAsync();
            var content = XElement.Parse(result).Value;
            return content;
        }
        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            //await this.ShowLuisResult(context, result);
            string message = "I'm sorry, i am not in a condition to answer your question currently.";
            await context.PostAsync(message);
        }

        // Go to https://luis.ai and create a new intent, then train/publish your luis app.
        // Finally replace "Greeting" with the name of your newly created intent in the following handler
        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, LuisResult result)
        {
            //await this.ShowLuisResult(context, result);
            string Welcomemessage = "Glad to talk to you. Welcome to iBot - your Virtual Customer Service.";
            await context.PostAsync(Welcomemessage);

            var feedback = ((Activity)context.Activity).CreateReply("Let's start by choosing your preferred language?");

            feedback.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                     new CardAction(){ Title = "English", Type=ActionTypes.PostBack, Value=$"English" },
                    new CardAction(){ Title = "Arabic", Type=ActionTypes.PostBack, Value=$"Arabic" }
                }
            };

            await context.PostAsync(feedback);

            context.Wait(MessageReceivedAsync);
        }
        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var userFeedback = await result;

            if (userFeedback.Text.Contains("English"))
            {
                var feedback = ((Activity)context.Activity).CreateReply("Let's start by choosing your preferred service?");

                feedback.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                {
                     new CardAction(){ Title = "Complaint Registration", Type=ActionTypes.PostBack, Value=$"Complaint Registration" },
                    new CardAction(){ Title = "Complaint Status", Type=ActionTypes.PostBack, Value=$"Complaint Status" }
                }
                };

                await context.PostAsync(feedback);

                context.Wait(MessageReceivedAsyncService);


            }
            else
            {
                PromptDialog.Text(
         context: context,
         resume: ServiceMessageArabic,
         prompt: "دعوانا نبدا باختيار الخدمة المفضلة لديك ؟ الشكوى أو الحالة",
         retry: "Sorry, I don't understand that.");

            }
        }
        public async Task ServiceMessageArabic(IDialogContext context, IAwaitable<string> result)
        {
            string transText = await Translation(result.ToString());

            if (transText.Contains("Complaint") || transText.Contains("service") || transText.Contains("issue"))
            {
                string Welcomemessage = "ما هي شكواك ؟";
                await context.PostAsync(Welcomemessage);
            }
            else if (transText.Contains("status"))
            {
                string Welcomemessage = "ما هو رقمك المرجعي للشكاوى ؟";
                await context.PostAsync(Welcomemessage);
            }
        }
        public async Task MessageReceivedAsyncService(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var userFeedback = await result;

            if (userFeedback.Text.Contains("Complaint Registration"))
            {
                PromptDialog.Text(
              context: context,
              resume: CustomerName,
              prompt: "What is your complaint/suggestion?",
              retry: "Sorry, I don't understand that.");
            }
            else
            {
                PromptDialog.Text(
             context: context,
             resume: ComplaintStatus,
             prompt: "What is your case reference number?",
             retry: "Sorry, I don't understand that.");
            }
        }
        public async Task ComplaintStatus(IDialogContext context, IAwaitable<string> result)
        {
            string message = "Your Complaint Status: In progress";
            await context.PostAsync(message);

            PromptDialog.Text(
        context: context,
        resume: AnythingElseHandler,
        prompt: "Is there anything else that I could help?",
        retry: "Sorry, I don't understand that.");
        }

        public async Task CustomerName(IDialogContext context, IAwaitable<string> result)
        {
            string response = await result;
            complaint = response;

            PromptDialog.Text(
              context: context,
              resume: Customer,
              prompt: "May I know your Name?",
              retry: "Sorry, I don't understand that.");
        }
        public async Task Customer(IDialogContext context, IAwaitable<string> result)
        {
            string response = await result;
            customerName = response;

            PromptDialog.Text(
                context: context,
                resume: CustomerMob,
                prompt: "May I have your Mobile Number?",
                retry: "Sorry, I don't understand that.");
        }
        public async Task CustomerMob(IDialogContext context, IAwaitable<string> result)
        {
            string response = await result;
            phone = response;

            PromptDialog.Text(
                context: context,
                resume: Final,
                prompt: "May I have your Email ID?",
                retry: "Sorry, I don't understand that.");
        }
        public async Task Final(IDialogContext context, IAwaitable<string> result)
        {
            string response = await result;
            email = response;


            await context.PostAsync($@"Your request has been logged. Our customer service team will get back to you shortly.
                                    {Environment.NewLine}Your service request  summary:
                                    {Environment.NewLine}Reference Number: CAS-1456,
                                    {Environment.NewLine}Complaint Title: {complaint},
                                    {Environment.NewLine}Customer Name: {customerName},
                                    {Environment.NewLine}Phone Number: {phone},
                                    {Environment.NewLine}Email: {email}");

            PromptDialog.Text(
          context: context,
          resume: AnythingElseHandler,
          prompt: "Is there anything else that I could help?",
          retry: "Sorry, I don't understand that.");
        }
        public async Task AnythingElseHandler(IDialogContext context, IAwaitable<string> argument)
        {


            var answer = await argument;
            if (answer.Contains("Yes") || answer.StartsWith("y") || answer.StartsWith("Y") || answer.StartsWith("yes"))
            {
                await GeneralGreeting(context, null);
            }
            else
            {
                string message = $"Thanks for using I Bot. Hope you have a great day!";
                await context.PostAsync(message);

                var survey = context.MakeMessage();

                var attachment = GetSurveyCard();
                survey.Attachments.Add(attachment);

                await context.PostAsync(survey);

                context.Done<string>("conversation ended.");
            }
        }

        public virtual async Task GeneralGreeting(IDialogContext context, IAwaitable<string> argument)
        {
            string message = $"Great! What else that can I help you?";
            await context.PostAsync(message);
            context.Wait(MessageReceivedAsync);
        }
        private static Microsoft.Bot.Connector.Attachment GetSurveyCard()
        {
            var heroCard = new HeroCard
            {
                Title = "",
                Subtitle = "",
                Text = "Kindly complete the Survey.",
                //Images = new List<CardImage> { new CardImage("http://idhabot.azurewebsites.net/DMankhool.png") },
                Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "Click to complete the Survey", value: "https://crmemeavoc1runtime.crm4.dynamics.com/471525fd-21d8-4134-a550-9d15c33c3bec/dubai-police-survey") }
            };

            return heroCard.ToAttachment();
        }
        [LuisIntent("Cancel")]
        public async Task CancelIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        private async Task ShowLuisResult(IDialogContext context, LuisResult result) 
        {
            await context.PostAsync($"You have reached {result.Intents[0].Intent}. You said: {result.Query}");
            context.Wait(MessageReceived);
        }
    }
}