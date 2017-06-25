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
    [LuisModel("subscription id", "subscription key")]
    [Serializable]
    public class LyncLuisDialog : LuisDialog<object>
    {


        private PresenceService _presenceService;

        public LyncLuisDialog(PresenceService presenceService)
        {
            _presenceService = presenceService;
        }

        private static string HelpString = "What I can do is find the ‘current build’, ‘last success build’, ’health report‘, and ’failed build‘ for you.";

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("[(bandit)robot reply]I'm sorry. I don't understand you.\n" + HelpString);
            context.Wait(MessageReceived);
        }

        static private string[] HelloString = new string[] { "Hi", "Hello", "How are you?", "How are you doing?", "How's everything?", "Hey", "What's up?" };
        [LuisIntent("hello")]
        public async Task SayHello(IDialogContext context, LuisResult result)
        {
            Random r = new Random();
            int i = r.Next()%HelloString.Length;
            await context.PostAsync("[(bandit)robot reply]" + HelloString[i] + "\n" + HelpString);
            context.Wait(MessageReceived);
        }

        [LuisIntent("current build")]
        public async Task CurrentBuild(IDialogContext context, LuisResult result)
        {

            dynamic jobj = Utilities.GetBuildInfo();
            var url = jobj.url.ToString();
            string message = "[(bandit)robot reply]You are asking for current build, here's link for the current build, show the url to you:\n" + url;
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("last success")]
        public async Task LastSuccess(IDialogContext context, LuisResult result)
        {
            dynamic jobj = Utilities.GetDBBuildInfo(Utilities.UFT_BUILDINFO_CMD.lstsuccessed, Utilities.UFT_BUILDINFO_TYPE.Nightly);
            if (jobj != null && jobj.data != null)
            {
                foreach(dynamic da in jobj.data)
                {
                    string url = da.url.ToString();
                    string dvd = da.artifactdir.ToString();
                    await context.PostAsync("[(bandit)robot reply]You are asking for last success build, url is here:\n" + url + "\nDVD is here:" + dvd);
                    context.Wait(MessageReceived);
                }
            }

        }

        [LuisIntent("health report")]
        public async Task HealthReport(IDialogContext context, LuisResult result)
        {
            dynamic jobj = Utilities.GetBuildInfo();
            string url = jobj.healthReport.ToString();
            await context.PostAsync("[(bandit)robot reply]You are asking for health report. Current status is: " + url);
            context.Wait(MessageReceived);
        }

        [LuisIntent("failed build")]
        public async Task FailedBuild(IDialogContext context, LuisResult result)
        {
            dynamic jobj = Utilities.GetBuildInfo();
            string url = "";
            string message = "";
            if (jobj.lastFailedBuild != null && jobj.lastFailedBuild.url != null)
            {
                url = jobj.lastFailedBuild.url.ToString();
                dynamic failedjob = Utilities.GetFailedJob(jobj.lastFailedBuild.subBuilds);
                message = "[(bandit)robot reply]You are asking for Failed build " + url;
                if (failedjob != null && failedjob.url != null)
                {
                    message += "\n Please check the failed job link for details: " + failedjob.url.ToString();
                }
            }
            else
            {
                message = "[(bandit)robot reply]You are asking for Failed build, we are lucky, no failed job recently";
            }
            await context.PostAsync(message);

            context.Wait(MessageReceived);
        }

        [LuisIntent("QAAutomationPortal")]
        public async Task QAAutomation(IDialogContext context, LuisResult result)
        {

            string message = "[(bandit)robot reply]You are asking for QA Automation ";

            await context.PostAsync(message);

            context.Wait(MessageReceived);
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
