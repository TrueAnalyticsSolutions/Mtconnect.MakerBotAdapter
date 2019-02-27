using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using System.Web;

namespace MakerBotAgentAdapterCore.MakerBotAPI {
  public static class FCGI {
    public static string Send(string host, string path, object parameters) {
      Type t = parameters.GetType();
      string query = "";
      System.Reflection.PropertyInfo[] props = t.GetProperties();
      List<string> properties = new List<string>();
      foreach (var prop in props) {
        properties.Add(prop.Name + "=" + prop.GetValue(parameters).ToString());
      }
      query = string.Join("&", properties.ToArray());
      return FCGI.Send(host, path, query);
    }
    public static string Send(string host, string path, string queryArgs) {
      string url = string.Format("http://{0}/{1}?{2}", host, path, queryArgs);
      HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
      webRequest.Method = "GET";

      //write("Sending FCGI: \r\n\t" + url, ConsoleColor.Gray);

      WebResponse webResponse = null;
      try {
        using (webResponse = webRequest.GetResponse()) {
          using (Stream str = webResponse.GetResponseStream()) {
            using (StreamReader sr = new StreamReader(str)) {
              string strData = sr.ReadToEnd();
              if (!string.IsNullOrEmpty(strData)) {
                return strData;
                //JObject accessData = JsonConvert.DeserializeObject<JObject>(strData);
                //return accessData;
              }else{
                throw new Exception("Access Token Data is empty");
              }
            }
          }
        }
      } catch (Exception ex) {
        return null;
      }
    }
  }
}