using MDACS.API.Requests;
using MDACS.API.Responses;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MDACS.API {
    public class Generic 
    {
        public static async Task<T> ReadTypeTransactionAsync<T>(
            string authUrl,
            string serviceUrl,
            string username,
            string password,
            string payload) 
        {
            var resp = await ReadStringTransactionAsync(authUrl, serviceUrl, username, password, payload);

            return JsonConvert.DeserializeObject<T>(resp);
        }

        public static async Task<string> ReadStringTransactionAsync(
            string authUrl,
            string serviceUrl,
            string username,
            string password,
            string payload) 
        {
            var stream = await ReadStreamTransactionAsync(authUrl, serviceUrl, username, password, payload);
            var reader = new StreamReader(stream);
            return reader.ReadToEnd();            
        }

        public static async Task<Stream> ReadStreamTransactionAsync(
            string authUrl,
            string serviceUrl,
            string username,
            string password,
            string payload) {
            WebRequest req;

            byte[] payloadBytes;

            if (authUrl != null)
            {
                payloadBytes = Encoding.UTF8.GetBytes(
                    await Auth.BuildAuthWithPayloadAsync(authUrl, username, password, payload)
                );
            }
            else
            {
                payloadBytes = Encoding.UTF8.GetBytes(payload);
            }

            req = WebRequest.Create(serviceUrl);
            req.Method = "POST";
            req.ContentType = "text/json";

            var data = await req.GetRequestStreamAsync();
            await data.WriteAsync(payloadBytes, 0, payloadBytes.Length);
            data.Close();

            var resp = await req.GetResponseAsync();

            var data_out = resp.GetResponseStream();

            return data_out;
        }

        public static Stream ReadStreamTransaction(
            string authUrl,
            string serviceUrl,
            string username,
            string password,
            string payload)
        {
            WebRequest req;
            WebResponse resp;
            Stream data;

            byte[] payloadBytes;

            if (authUrl != null)
            {
                var tsk = Auth.BuildAuthWithPayloadAsync(authUrl, username, password, payload);

                tsk.Wait();

                payloadBytes = Encoding.UTF8.GetBytes(
                    tsk.Result
                );
            } else
            {
                payloadBytes = Encoding.UTF8.GetBytes(payload);
            }

            req = WebRequest.Create(serviceUrl);

            req.Method = "POST";
            req.ContentType = "text/json";

            data = req.GetRequestStream();
            data.Write(payloadBytes, 0, payloadBytes.Length);
            data.Close();

            resp = req.GetResponse();

            data = resp.GetResponseStream();

            return data;
        }
    }
}