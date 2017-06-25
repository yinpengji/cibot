using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;

namespace LyncBot.Core
{
    public class MessageSender
    {
        Microsoft.Lync.Model.Extensibility.Automation automation;
        public MessageSender()
        {
            try
            {
                automation = LyncClient.GetAutomation();
            }
            catch (LyncClientException lyncClientException)
            {
                Console.Out.WriteLine(lyncClientException);
            }
            catch (SystemException systemException)
            {
                if (IsLyncException(systemException))
                {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("Error: " + systemException);
                }
                else
                {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }

        }

        private bool IsLyncException(SystemException ex)
        {
            return
                ex is NotImplementedException ||
                ex is ArgumentException ||
                ex is NullReferenceException ||
                ex is NotSupportedException ||
                ex is ArgumentOutOfRangeException ||
                ex is IndexOutOfRangeException ||
                ex is InvalidOperationException ||
                ex is TypeLoadException ||
                ex is TypeInitializationException ||
                ex is InvalidComObjectException ||
                ex is InvalidCastException;
        }

        public void Start()
        {
            Thread oThread = new Thread(new ThreadStart(SendBreakBuildMessage));

            // Start the thread
            oThread.Start();
        }

        private static readonly HttpClient client = new HttpClient();
        public void SendBreakBuildMessage()
        {
            do
            {
                FileInfo fi = new FileInfo(@"c:\builderror.txt");

                //if (fi.Exists)
                {
                    try
                    {
                        dynamic jobj = Utilities.GetDBBuildInfo(Utilities.UFT_BUILDINFO_CMD.lstfailure, Utilities.UFT_BUILDINFO_TYPE.CI);
                        string failedJobUrl = "";
                        dynamic data = jobj.data;

                        List<string> committers = new List<string>();
                        string buildVersion = "";
                        foreach (var obj in jobj.data)
                        {
                            bool notified = obj.notified;
                            if (!notified)
                            { 
                                failedJobUrl = obj.url.ToString();
                                buildVersion = obj.buildversion;
                                if (obj.committers != null)
                                {
                                    //Get the conversation modalities and settings
                                    AutomationModalities conversationModes = 0;
                                    Dictionary<AutomationModalitySettings, object> conversationSettings =
                                        new Dictionary<AutomationModalitySettings, object>();

                                    conversationModes |= AutomationModalities.InstantMessage;
                                    conversationSettings.Add(AutomationModalitySettings.SendFirstInstantMessageImmediately, true);
                                    conversationSettings.Add(AutomationModalitySettings.FirstInstantMessage, "[(bandit)robot Message]Your commit may break the build, please check the failure. Check the link below for details\n" + failedJobUrl);

                                    string[] committersArray = obj.committers.ToString().Split(',');

                                    foreach (string committer in committersArray)
                                    {
                                        try
                                        {

                                            //committers.Add("sip:" + "yguo@hpe.com");
                                            if (committer == "xiwen.zhao@hpe.com")
                                            {
                                                committers.Add("sip:" + "xw@hpe.com");
                                            }
                                            else
                                            {
                                                committers.Add("sip:" + committer);
                                            }
                                            automation.BeginStartConversation(conversationModes, committers, conversationSettings,
                                                                                StartConversationCallback, null);

                                            committers.Clear();
                                        }
                                        catch (LyncClientException lyncClientException)
                                        {
                                            Console.WriteLine(lyncClientException);
                                        }
                                        catch (SystemException systemException)
                                        {
                                            if (IsLyncException(systemException))
                                            {
                                                // Log the exception thrown by the Lync Model API.
                                                Console.WriteLine("Error: " + systemException);
                                            }
                                            else
                                            {
                                                // Rethrow the SystemException which did not come from the Lync Model API.
                                                throw;
                                            }
                                        }

                                    }
                                    if (committersArray.Length > 0)
                                    {
                                        HttpContent content = new StringContent("{  \"notified\":\"true\" }", Encoding.UTF8, "application/json");
                                         
                                        client.PatchAsync("your url" + buildVersion, content);
                                    }
                                }
                            }
                            break;
                        }

                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    //fi.Delete();
                }
                Thread.Sleep(10000);
            } while (true);
        }

        
        private void StartConversationCallback(IAsyncResult result)
        {
            try
            {
                automation.EndStartConversation(result);
            }
            catch (LyncClientException lyncClientException)
            {
                Console.WriteLine(lyncClientException);
            }
            catch (SystemException systemException)
            {
                if (IsLyncException(systemException))
                {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("Error: " + systemException);
                }
                else
                {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }
        }
    }
}


public static class HttpClientExtensions
{
    /// <summary>
    /// Send a PATCH request to the specified Uri as an asynchronous operation.
    /// </summary>
    /// 
    /// <returns>
    /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
    /// </returns>
    /// <param name="client">The instantiated Http Client <see cref="HttpClient"/></param>
    /// <param name="requestUri">The Uri the request is sent to.</param>
    /// <param name="content">The HTTP request content sent to the server.</param>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="client"/> was null.</exception>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
    {
        return client.PatchAsync(CreateUri(requestUri), content);
    }

    /// <summary>
    /// Send a PATCH request to the specified Uri as an asynchronous operation.
    /// </summary>
    /// 
    /// <returns>
    /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
    /// </returns>
    /// <param name="client">The instantiated Http Client <see cref="HttpClient"/></param>
    /// <param name="requestUri">The Uri the request is sent to.</param>
    /// <param name="content">The HTTP request content sent to the server.</param>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="client"/> was null.</exception>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent content)
    {
        return client.PatchAsync(requestUri, content, CancellationToken.None);
    }
    /// <summary>
    /// Send a PATCH request with a cancellation token as an asynchronous operation.
    /// </summary>
    /// 
    /// <returns>
    /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
    /// </returns>
    /// <param name="client">The instantiated Http Client <see cref="HttpClient"/></param>
    /// <param name="requestUri">The Uri the request is sent to.</param>
    /// <param name="content">The HTTP request content sent to the server.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="client"/> was null.</exception>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content, CancellationToken cancellationToken)
    {
        return client.PatchAsync(CreateUri(requestUri), content, cancellationToken);
    }

    /// <summary>
    /// Send a PATCH request with a cancellation token as an asynchronous operation.
    /// </summary>
    /// 
    /// <returns>
    /// Returns <see cref="T:System.Threading.Tasks.Task`1"/>.The task object representing the asynchronous operation.
    /// </returns>
    /// <param name="client">The instantiated Http Client <see cref="HttpClient"/></param>
    /// <param name="requestUri">The Uri the request is sent to.</param>
    /// <param name="content">The HTTP request content sent to the server.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="client"/> was null.</exception>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent content, CancellationToken cancellationToken)
    {
        return client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), requestUri)
        {
            Content = content
        }, cancellationToken);
    }

    //public static Task<HttpResponseMessage> PatchJsonAsync(this HttpClient client, Uri requestUri, HttpContent content, CancellationToken cancellationToken)
    //{
    //    var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri)
    //    {
    //        Content = content
    //    };
    //    request.Headers.Add("Content-Type", "application/jason");
    //    return client.SendAsync(request, cancellationToken);
    //}
    private static Uri CreateUri(string uri)
    {
        return string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
    }
}