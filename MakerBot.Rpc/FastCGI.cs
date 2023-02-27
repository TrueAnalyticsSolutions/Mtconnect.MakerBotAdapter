using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MakerBot
{
    /// <summary>
    /// Sends HTTP requests to the FastCGI implementation
    /// </summary>
    public static class FastCGI
    {
        public enum AccessTokenContexts
        {
            jsonrpc,
            put,
            camera
        }

        public static async Task<string> GetAccessToken(IPAddress address, string authCode, string clientId, string clientSecret,  AccessTokenContexts context = AccessTokenContexts.jsonrpc)
        {
            object options = new
            {
                response_type = "token",
                client_id = clientId,
                client_secret = clientSecret,
                auth_code = authCode,
                context = context.ToString()
            };
            var response = await FastCGI.Send(address, "auth", options);
            if (string.IsNullOrEmpty(response)) throw new Exception("Failed to get Access Token");

            JObject access = JsonConvert.DeserializeObject<JObject>(response);
            if (access == null) throw new Exception("Failed to parse Access Token response");

            string accessResponse = access["status"].ToString();
            if (!accessResponse.Equals("success", StringComparison.OrdinalIgnoreCase)) throw new Exception("Access Token rejected");

            return access["access_token"].ToString();
        }
        public static async Task<string> GetAccessToken(IPAddress address, string clientId, string clientSecret, AccessTokenContexts context = AccessTokenContexts.jsonrpc)
        {
            var authCode = await FastCGI.GetAuthCode(address, clientId, clientSecret);
            return await FastCGI.GetAccessToken(address, authCode, clientId, clientSecret, context);
        }

        public static async Task<string> GetAuthCode(IPAddress address, string clientId, string clientSecret)
        {
            var accessCode = await FastCGI.GetAccessCode(address, clientId, clientSecret);
            return await FastCGI.GetAuthCode(address, accessCode, clientId, clientSecret);
        }
        public static async Task<string> GetAuthCode(IPAddress address, string accessCode, string clientId, string clientSecret)
        {
            object options = new
            {
                response_type = "answer",
                client_id = clientId,
                client_secret = clientSecret,
                answer_code = accessCode
            };

            string authCode = string.Empty;

            int waitCycles = 0;
            int cycleTime = 1000;
            using (var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(120)))
            {
                while (!cancellationSource.IsCancellationRequested && string.IsNullOrEmpty(authCode))
                {
                    Thread.Sleep(cycleTime);
                    waitCycles++;

                    var response = await FastCGI.Send(address, "auth", options);
                    if (string.IsNullOrEmpty(response)) throw new Exception("Failed to get Auth Code");

                    JObject answer = JsonConvert.DeserializeObject<JObject>(response);
                    if (answer == null) throw new Exception("Failed to parse Auth Code response");

                    string answerResponse = answer["answer"].ToString();
                    if (string.IsNullOrEmpty(answerResponse)) throw new Exception("Invalid Auth Code response");

                    if (answerResponse.Equals("accepted"))
                    {
                        authCode = answer["code"].ToString();
                        cancellationSource.Cancel();
                        break;
                    } else if (answerResponse.Equals("rejected"))
                    {
                        cancellationSource.Cancel();
                        break;
                    } else if (answerResponse.Equals("pending"))
                    {
                        // Pairing mode is already active for this printer. Press a button on printer to disable pairing mode.
                    }
                    else
                    {
                        throw new InvalidOperationException("Unrecognized answer response");
                    }
                }
            }

            if (string.IsNullOrEmpty(authCode)) throw new UnauthorizedAccessException();

            return authCode;
        }
        
        public static async Task<string> GetAccessCode(IPAddress address, string clientId, string clientSecret)
        {
            object options = new
            {
                response_type = "code",
                client_id = clientId,
                client_secret = clientSecret
            };
            var response = await FastCGI.Send(address, "auth", options);

            if (string.IsNullOrEmpty(response)) throw new Exception("Failed to get Access Code");

            JObject accessData = JsonConvert.DeserializeObject<JObject>(response);
            if (accessData == null) throw new Exception("Failed to parse Access Code response");

            return accessData["answer_code"].ToString();
        }

        public static async Task<string> Send(IPAddress address, string path, object parameters)
        {
            Type t = parameters.GetType();
            string query = "";
            System.Reflection.PropertyInfo[] props = t.GetProperties();
            List<string> properties = new List<string>();
            foreach (var prop in props)
            {
                properties.Add(prop.Name + "=" + prop.GetValue(parameters).ToString());
            }
            query = string.Join("&", properties.ToArray());
            return await FastCGI.Send(address, path, query);
        }
        public static async Task<string> Send(IPAddress address, string path, string queryArgs)
        {
            string url = string.Format("http://{0}/{1}?{2}", address.ToString(), path, queryArgs);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";

            WebResponse webResponse = null;
            try
            {
                using (webResponse = await webRequest.GetResponseAsync())
                using (Stream str = webResponse.GetResponseStream())
                using (StreamReader sr = new StreamReader(str))
                {
                    string strData = await sr.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(strData))
                    {
                        return strData;
                    }
                    else
                    {
                        throw new Exception("FastCGI Response is empty");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
