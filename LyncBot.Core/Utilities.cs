using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LyncBot.Core
{
    class Utilities
    {
        public static dynamic GetBuildInfo()
        {
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                HttpResponseMessage response = client.GetAsync("Your URL 1").Result;
                response.EnsureSuccessStatusCode();
                string result = response.Content.ReadAsStringAsync().Result;
                dynamic jObj = JsonConvert.DeserializeObject(result);
                return jObj;
            }
        }

        public static string UFT_BUILDINFO_URL = "Your URL 2";
        public enum UFT_BUILDINFO_TYPE
        {
            CI,
            Nightly
        }

        public enum UFT_BUILDINFO_CMD
        {
            lstfailure, 
            lstsuccessed,
            lst, //last job
            all // last with count
        }


        public static dynamic GetDBBuildInfo(UFT_BUILDINFO_CMD cmd, UFT_BUILDINFO_TYPE type, int count = 1)
        {
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                HttpResponseMessage response = client.GetAsync(UFT_BUILDINFO_URL + cmd + "?type=" +  type + "&count=" + count).Result;
                response.EnsureSuccessStatusCode();
                string result = response.Content.ReadAsStringAsync().Result;
                dynamic jObj = JsonConvert.DeserializeObject(result);
                return jObj;
            }
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
                Console.WriteLine(e);
            }
            return null;
        }
    }
}
