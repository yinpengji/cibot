using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;

namespace LyncBot.Core.Dialogs
{
    [LuisModel("51a3752a-5915-47cc-bc37-44a63d360608", "dcc369a50b844a0bb887f621e8d2b90d")]
    [Serializable]
    public class LyncLuisDialog : LuisDialog<object>
    {

        public static dynamic GetBuildInfo()
        {
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                //client.BaseAddress = new Uri("http://mydtbld0120.hpeswlab.net:8080/job/UFT.14.1.Build/api/json");
                HttpResponseMessage response = client.GetAsync("http://mydtbld0120.hpeswlab.net:8080/job/UFT.14.1.Build/api/json").Result;
                response.EnsureSuccessStatusCode();
                string result = response.Content.ReadAsStringAsync().Result;
                dynamic jObj = JsonConvert.DeserializeObject(result);
                return jObj;
            }
        }

        public static dynamic GetDBBuildInfo()
        {
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                //client.BaseAddress = new Uri("http://mydtbld0120.hpeswlab.net:8080/job/UFT.14.1.Build/api/json");
                HttpResponseMessage response = client.GetAsync("http://myd-vm00130.hpeswlab.net:3000/api/v1/uft/lstsuccessednightly").Result;
                response.EnsureSuccessStatusCode();
                string result = response.Content.ReadAsStringAsync().Result;
                dynamic jObj = JsonConvert.DeserializeObject(result);
                return jObj;
            }
        }
        private PresenceService _presenceService;

        public LyncLuisDialog(PresenceService presenceService)
        {
            _presenceService = presenceService;
        }
        //[LuisIntent("")]
        //public async Task None(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
        //{
        //    //await context.PostAsync("I'm sorry. I didn't understand you.");
        //    // Dont do anything. Pretend I am busy.
        //    context.Wait(MessageReceived);
        //}

        //[LuisIntent("HiGreetings")]
        //public async Task HiGreetings(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
        //{
        //    var activity = await message;
        //    string name = GetName(activity.From);
        //    await context.PostOnlyOnceAsync(Responses.HiGreetingsResponse(name), nameof(HiGreetings));
        //    context.Wait(MessageReceived);
        //}

        //[LuisIntent("GoodMorningGreetings")]
        //public async Task GoodMorningGreetings(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
        //{
        //    string name = string.Empty;
        //    if (!context.PrivateConversationData.ContainsKey(nameof(HiGreetings)))
        //    {
        //        var activity = await message;
        //        name = GetName(activity.From);
        //    }
        //    await context.PostOnlyOnceAsync(Responses.GoodMorningGreetingsResponse(name), nameof(GoodMorningGreetings));
        //    context.Wait(MessageReceived);
        //}

        //[LuisIntent("Call")]
        //public async Task Call(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
        //{
        //    await context.PostOnlyOnceAsync(Responses.CallResponse(), nameof(Call));
        //    _presenceService.SetPresenceBusy();
        //    context.Wait(MessageReceived);
        //}


        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("[robot:]I'm sorry. I don't understand you.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("hello")]
        public async Task SayHello(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("[robot:]Hello");
            context.Wait(MessageReceived);
        }

        [LuisIntent("current build")]
        public async Task CurrentBuild(IDialogContext context, LuisResult result)
        {

            dynamic jobj = GetDBBuildInfo();
            var url = jobj.url.ToString();
            string message = "[robot:]You are asking for current build, here's link for the current build, show the url to you:\n" + url;
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("last success")]
        public async Task LastSuccess(IDialogContext context, LuisResult result)
        {
            dynamic jobj = GetBuildInfo();
            string url = jobj.lastSuccessfulBuild.url.ToString();
            await context.PostAsync("[robot:]You are asking for last success build, url is here:\n" + url);
            context.Wait(MessageReceived);
        }

        [LuisIntent("health report")]
        public async Task HealthReport(IDialogContext context, LuisResult result)
        {
            dynamic jobj = GetBuildInfo();
            string url = jobj.healthReport.ToString();
            await context.PostAsync("[robot:]You are asking for health report. Current status is: " + url);
            context.Wait(MessageReceived);
        }

        [LuisIntent("failed build")]
        public async Task FailedBuild(IDialogContext context, LuisResult result)
        {
            dynamic jobj = GetBuildInfo();
            string url = jobj.lastFailedBuild.url.ToString();
            dynamic failedjob = GetFailedJob(jobj.lastFailedBuild.subBuilds);
            string message = "[robot:]You are asking for Failed build, url is shown below\n" + url;
            if (failedjob != null)
            {
                message += "\n Please check the failed job link for details: " + "http://mydtbld0120.hpeswlab.net:8080/" + failedjob.url.ToString();
            }
            else
            {
                message = "[robot:]You are asking for Failed build, we are lucky, no failed job recently";
            }
            await context.PostAsync(message);

            context.Wait(MessageReceived);
        }

        public static dynamic GetFailedJob(dynamic subBuilds)
        {
            try
            {
                foreach (var job in subBuilds)
                {
                    if (job.result.ToString() != "SUCCESS")
                    {
                        if (job.build.subBuilds != null)
                        {
                            return GetFailedJob(job.build.subBuilds);
                        }
                        else
                        {
                            return job;
                        }
                    }

                }
            }
            catch (Exception e)
            {

            }
            return null;
        }
        private static string GetName(ChannelAccount from)
        {
            string name = string.Empty;
            if (string.IsNullOrEmpty(from.Name))
                return name;

            var res = from.Name.Split(' ');
            foreach (var item in res)
            {
                if (item.Length > 1)
                {
                    name = item;
                    break;
                }
            }
            return name;
        }
    }
}
