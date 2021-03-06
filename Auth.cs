using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MDACS.API
{
    public class Auth
    {
        class AuthCompletePacketInner
        {
            public string challenge;
            public string chash;
            public bool payload;
        }

        class AuthCompletePacket
        {
            public AuthCompletePacketInner auth;
            public string payload;
        }

        private static string ByteArrayToHexString(byte[] input)
        {
            StringBuilder sb;

            sb = new StringBuilder();

            for (uint x = 0; x < input.Length; ++x)
            {
                sb.AppendFormat("{0:x2}", input[x]);
            }

            return sb.ToString();
        }

        public static async Task<string> AuthTransactionAsync(string auth_url, string msg)
        {
            WebRequest req;
            WebResponse resp;
            Stream data;
            StreamReader reader;

            req = WebRequest.Create(auth_url);

            req.Method = "POST";
            req.ContentType = "text/json";

            data = await req.GetRequestStreamAsync();
            var buf = Encoding.UTF8.GetBytes(msg);
            await data.WriteAsync(buf, 0, buf.Length);
            data.Close();

            resp = await req.GetResponseAsync();

            data = resp.GetResponseStream();

            reader = new StreamReader(data);

            return await reader.ReadToEndAsync();
        }
        public static async Task<string> GetAuthChallengeAsync(string auth_url)
        {
            WebRequest req;
            WebResponse resp;
            Stream data;
            StreamReader reader;
            string json;
            Responses.AuthResponse auth_resp;

            req = WebRequest.Create(string.Format("{0}/challenge", auth_url));

            req.Method = "POST";
            req.ContentType = "text/json";
            req.ContentLength = 0;

            data = await req.GetRequestStreamAsync();
            data.Close();

            resp = await req.GetResponseAsync();

            data = resp.GetResponseStream();

            reader = new StreamReader(data);

            json = reader.ReadToEnd();

            auth_resp = JsonConvert.DeserializeObject<Responses.AuthResponse>(json);

            return auth_resp.challenge;
        }
        public class MsgAuth
        {
            public string chash;
            public string challenge;
            public string hash;
        }
        public class Msg
        {
            public MsgAuth auth;
            public string payload;
        }
        /// <summary>
        /// This structure is used across all services. It is returned for users
        /// who are successfully authenticated. It holds information about the user
        /// including security privileges.
        /// </summary>
        public class User
        {
            public string name;
            public string user;
            public string hash;
            public bool admin;
            public string userfilter;
            public bool can_delete;
            public string phone;
            public string email;
            public bool privExecuteCommand;
            public bool privRegisterCommandService;
        }
        public class AuthCheckPayload
        {
            public string chash;
            public string phash;
            public string challenge;
        }
        public class AuthCheckNoPayload
        {
            public string hash;
            public string challenge;
        }
        public static async Task<Responses.AuthCheckResponse> AuthenticateMessageAsync(string auth_url, string msg)
        {
            var msg_decoded = JsonConvert.DeserializeObject<Msg>(msg);

            if (msg_decoded == null)
            {
                var tmp = new Responses.AuthCheckResponse();
                tmp.success = false;
                return tmp;
            }

            return await AuthenticateMessageAsync(
                auth_url,
                msg_decoded
            );
        }
        public static async Task<Responses.AuthCheckResponse> AuthenticateMessageAsync(string auth_url, Msg msg)
        {
            if (msg.payload == null) {
                var checknp = new AuthCheckNoPayload();

                checknp.hash = msg.auth.hash;
                checknp.challenge = msg.auth.challenge;

                var resp_string = await AuthTransactionAsync(
                    string.Format("{0}/verify", auth_url),
                    JsonConvert.SerializeObject(checknp)
                );

                return JsonConvert.DeserializeObject<Responses.AuthCheckResponse>(resp_string);
            }

            var check = new AuthCheckPayload();

            var hasher = new SHA512Managed();

            var binary_hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(msg.payload));

            var phash = ByteArrayToHexString(binary_hash);

            check.chash = msg.auth.chash;
            check.phash = phash;
            check.challenge = msg.auth.challenge;

            var resp_string2 = await AuthTransactionAsync(
                string.Format("{0}/verify-payload", auth_url),
                JsonConvert.SerializeObject(check)
            );

            var resp = JsonConvert.DeserializeObject<Responses.AuthCheckResponse>(resp_string2);

            resp.payload = msg.payload;

            return resp;
        }
        public static async Task<string> BuildAuthWithPayloadAsync(string auth_url, string username, string password, string payload)
        {
            var challenge = await GetAuthChallengeAsync(auth_url);
            var hasher = new SHA512Managed();

            var payload_hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(payload));
            var password_hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(password));

            var complete_hash = hasher.ComputeHash(
                Encoding.ASCII.GetBytes(string.Format(
                    "{0}{1}{2}{3}",
                    ByteArrayToHexString(payload_hash),
                    challenge,
                    username,
                    ByteArrayToHexString(password_hash)
                ))
            );

            var packet = new AuthCompletePacket();
            packet.auth = new AuthCompletePacketInner();

            packet.auth.challenge = challenge;
            packet.auth.chash = ByteArrayToHexString(complete_hash);
            packet.auth.payload = true;
            packet.payload = payload;

            return JsonConvert.SerializeObject(packet);
        }
    }
}
