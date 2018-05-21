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

namespace MDACS.API
{
    public class Database
    {
        public class Alert
        {

        }

        public enum ItemSourceType
        {
            AmazonGlacier = 0,
            CIFSPrivateNetwork = 1,
        }

        public class Item
        {
            public string security_id;
            public string node;
            public double duration;
            public double metatime;
            public string fqpath;
            public string userstr;
            public string timestr;
            public string datestr;
            public string devicestr;
            public string datatype;
            public ulong datasize;
            public string note;
            public string state;
            public string uploaded_by_user;
            public string data_hash_sha512;
            public string manager_uuid;
            public List<string[]> sources;

            public static string Serialize(Item item)
            {
                return JsonConvert.SerializeObject(item);
            }

            public static Item Deserialize(string input)
            {
                return JsonConvert.DeserializeObject<Item>(input);
            }
        }

        public static async Task<Stream> DownloadDataAsync(string security_id, string auth_url, string db_url, string username, string password)
        {
            return await Generic.ReadStreamTransactionAsync(
                auth_url,
                string.Format("{0}/download?{1}", db_url, security_id),
                username,
                password,
                "{}"
            );
        }

        public static async Task<Responses.HandleBatchSingleOpsResponse> BatchSingleOps(
            string auth_url,
            string db_url,
            string username,
            string password,
            Requests.BatchSingleOp[] ops)
        {
            var req = new Requests.HandleBatchSingleOpsRequest()
            {
                ops = ops
            };

            var stream = await Generic.ReadStreamTransactionAsync(
                auth_url,
                $"{db_url}/commit_batch_single_ops",
                username,
                password,
                JsonConvert.SerializeObject(req)
            );

            var bs = new StreamReader(stream, Encoding.UTF8, false);

            return JsonConvert.DeserializeObject<Responses.HandleBatchSingleOpsResponse>(await bs.ReadToEndAsync());
        }

        public static async Task<Responses.CommitConfigurationResponse> CommitConfiguration(
            string auth_url,
            string db_url,
            string username,
            string password,
            string deviceid,
            string userid,
            string config_data)
        {
            var req = new Requests.CommitConfigurationRequest()
            {
                deviceid = deviceid,
                config_data = config_data,
                userid = userid,
            };

            var stream = await Generic.ReadStreamTransactionAsync(
                auth_url,
                $"{db_url}/commit-configuration",
                username,
                password,
                JsonConvert.SerializeObject(req)
            );

            var bs = new StreamReader(stream, Encoding.UTF8, false);

            return JsonConvert.DeserializeObject<Responses.CommitConfigurationResponse>(await bs.ReadToEndAsync());
        }

        public static async Task<Responses.DeviceConfigResponse> DeviceConfig(
            string auth_url,
            string db_url,
            string username,
            string password,
            string deviceid,
            string current_config_data)
        {
            var stream = await Generic.ReadStreamTransactionAsync(
                null,
                $"{db_url}/device-config",
                username,
                password,
                JsonConvert.SerializeObject(new DeviceConfigRequest()
                {
                    deviceid = deviceid,
                    current_config_data = current_config_data,
                })
            );

            var bs = new StreamReader(stream, Encoding.UTF8, false);

            return JsonConvert.DeserializeObject<Responses.DeviceConfigResponse>(await bs.ReadToEndAsync());
        }

        public static async Task<Responses.EnumerateConfigurationsResponse> EnumerateConfigurations(
            string auth_url,
            string db_url,
            string username,
            string password)
        {
            var stream = await Generic.ReadStreamTransactionAsync(
                auth_url,
                $"{db_url}/enumerate-configurations",
                username,
                password,
                ""
            );

            var bs = new StreamReader(stream, Encoding.UTF8, false);

            return JsonConvert.DeserializeObject<Responses.EnumerateConfigurationsResponse>(await bs.ReadToEndAsync());
        }

        public static async Task<Responses.CommitSetResponse> CommitSetAsync(
            string auth_url,
            string db_url,
            string username,
            string password,
            string sid,
            JObject meta)
        {
            var csreq = new Requests.CommitSetRequest();

            csreq.security_id = sid;
            csreq.meta = meta;

            var stream = await Generic.ReadStreamTransactionAsync(
                auth_url,
                $"{db_url}/commitset",
                username,
                password,
                JsonConvert.SerializeObject(csreq)
            );

            var bs = new StreamReader(stream, Encoding.UTF8, false);

            return JsonConvert.DeserializeObject<Responses.CommitSetResponse>(await bs.ReadToEndAsync());
        }

        public static async Task<Responses.UploadResponse> UploadAsync(
            string auth_url,
            string db_url,
            string username,
            string password,
            long datasize,
            string datatype,
            string datestr,
            string devicestr,
            string timestr,
            string userstr,
            Stream data
        )
        {
            var header = new Requests.UploadHeader();

            header.datasize = (ulong)datasize;
            header.datatype = datatype;
            header.datestr = datestr;
            header.devicestr = devicestr;
            header.timestr = timestr;
            header.userstr = userstr;

            var payload = JsonConvert.SerializeObject(header);

            Console.WriteLine("Waiting for auth...");
            var packet = await API.Auth.BuildAuthWithPayloadAsync(
                auth_url,
                username,
                password,
                payload
            );
            Console.WriteLine("Got auth...");

            var packet_bytes = Encoding.UTF8.GetBytes($"{packet}\n");

            var wr = WebRequest.Create($"{db_url}/upload");

            wr.ContentType = "text/json";
            wr.Method = "POST";

            // Workaround, TODO, BUG: The server is not likely correctly doing something which causes
            //                        the connection to hang since the subsystem appears to be reusing
            //                        the sockets (which is appropriate and good) yet without forcing
            //                        a new connection this will cause hangs/failures in the code below.
            wr.ConnectionGroupName = DateTime.Now.ToString();

            ((HttpWebRequest)wr).AllowWriteStreamBuffering = false;
            ((HttpWebRequest)wr).SendChunked = true;
            // When sending a very long post this might need to be turned off
            // since it can cause an abrupt canceling of the request.
            ((HttpWebRequest)wr).KeepAlive = false;

            wr.Timeout = 1000 * 30;

            var reqstreamTask = wr.GetRequestStreamAsync();

            if (await Task.WhenAny(reqstreamTask, Task.Delay(1000 * 12)) != reqstreamTask)
            {
                return new Responses.UploadResponse()
                {
                    success = false,
                    fqpath = null,
                    security_id = null,
                };
            }

            var reqstream = reqstreamTask.Result;

            var firstWriteTask = reqstream.WriteAsync(packet_bytes, 0, packet_bytes.Length);

            if (await Task.WhenAny(firstWriteTask, Task.Delay(1000 * 12)) != firstWriteTask)
            {
                return new Responses.UploadResponse()
                {
                    success = false,
                    fqpath = null,
                    security_id = null,
                };
            }

            Console.WriteLine("First write done.");

            int sent = 0;

            byte[] buf = new byte[1024 * 32];

            while (sent < data.Length)
            {
                //Console.WriteLine("Read");
                var amtTask = data.ReadAsync(buf, 0, buf.Length);

                if (await Task.WhenAny(amtTask, Task.Delay(1000 * 12)) != amtTask)
                {
                    //Console.WriteLine("ReadTimeout");
                    return new Responses.UploadResponse()
                    {
                        success = false,
                        fqpath = null,
                        security_id = null,
                    };
                }

                //Console.WriteLine("ReadDone");

                var amt = amtTask.Result;

                // Is it possible for amt to be zero and that create an infinite loop here?
                // This checks for that condition since the documentation says zero means that
                // the end of the stream has been reached.

                if (amt < 1)
                {
                    break;
                }

                //Console.WriteLine("Write");

                var writeTask = reqstream.WriteAsync(buf, 0, amt);

                if (await Task.WhenAny(writeTask, Task.Delay(1000 * 12)) != writeTask)
                {
                    //Console.WriteLine("WriteTimeout");
                    return new Responses.UploadResponse()
                    {
                        success = false,
                        fqpath = null,
                        security_id = null,
                    };
                }

                //Console.WriteLine("WriteDone");

                sent += amt;
            }

            Console.WriteLine("All writes done.");

            reqstream.Close();

            var chunk = new byte[1024];

            Console.WriteLine("Reading the response.");

            var respTask = wr.GetResponseAsync();

            if (await Task.WhenAny(respTask, Task.Delay(1000 * 60 * 5)) != respTask)
            {
                return new Responses.UploadResponse()
                {
                    success = false,
                    fqpath = null,
                    security_id = null,
                };
            }

            var rstream = respTask.Result.GetResponseStream();
            var respBytesTask = rstream.ReadAsync(chunk, 0, chunk.Length);

            if (await Task.WhenAny(respBytesTask, Task.Delay(1000 * 60 * 5)) != respBytesTask)
            {
                return new Responses.UploadResponse()
                {
                    success = false,
                    fqpath = null,
                    security_id = null,
                };
            }

            var respLength = respBytesTask.Result;

            var respText = Encoding.UTF8.GetString(chunk, 0, respLength);

            return JsonConvert.DeserializeObject<Responses.UploadResponse>(respText);
        }

        public static async Task<Responses.UploadResponse> UploadAsync(
            string auth_url,
            string db_url,
            string username,
            string password,
            long datasize,
            string datatype,
            string datestr,
            string devicestr,
            string timestr,
            string userstr,
            IEnumerable<byte[]> callback
        )
        {
            var header = new Requests.UploadHeader();

            header.datasize = (ulong)datasize;
            header.datatype = datatype;
            header.datestr = datestr;
            header.devicestr = devicestr;
            header.timestr = timestr;
            header.userstr = userstr;

            var payload = JsonConvert.SerializeObject(header);

            var packet = await API.Auth.BuildAuthWithPayloadAsync(
                auth_url,
                username,
                password,
                payload
            );
            var packet_bytes = Encoding.UTF8.GetBytes($"{packet}\n");

            var wr = WebRequest.Create($"{db_url}/upload");

            wr.ContentType = "text/json";
            wr.Method = "POST";

            ((HttpWebRequest)wr).AllowWriteStreamBuffering = false;
            ((HttpWebRequest)wr).SendChunked = true;
            // When sending a very long post this might need to be turned off
            // since it can cause an abrupt canceling of the request.
            ((HttpWebRequest)wr).KeepAlive = false;

            var reqstream = await wr.GetRequestStreamAsync();

            await reqstream.WriteAsync(packet_bytes, 0, packet_bytes.Length);

            foreach (var stream_chunk in callback)
            {
                await reqstream.WriteAsync(stream_chunk, 0, stream_chunk.Length);
            }

            reqstream.Close();

            var chunk = new byte[1024];

            var resp = await wr.GetResponseAsync();
            var rstream = resp.GetResponseStream();
            var resp_length = await rstream.ReadAsync(chunk, 0, chunk.Length);
            var resp_text = Encoding.UTF8.GetString(chunk, 0, resp_length);

            return JsonConvert.DeserializeObject<Responses.UploadResponse>(resp_text);
        }

        public delegate void GetDataProgress(ulong bytes_read);

        public static async Task<DataResponse> GetDataAsync(string auth_url, string db_url, string username, string password, GetDataProgress progress_event)
        {
            string payload = "{}";

            var stream = await Generic.ReadStreamTransactionAsync(
                auth_url,
                string.Format("{0}/data", db_url),
                username,
                password,
                payload
            );

            MemoryStream ms = new MemoryStream(1024 * 1024 * 5);

            int count = 0;
            byte[] buf = new byte[1024 * 32];
            ulong amount_read = 0;

            while ((count = await stream.ReadAsync(buf, 0, buf.Length)) > 0)
            {
                ms.Write(buf, 0, count);
                amount_read += (ulong)count;
                if (progress_event != null)
                {
                    progress_event?.Invoke(amount_read);
                }
            }

            string resp_json = Encoding.UTF8.GetString(ms.ToArray());

            //string resp_json = File.ReadAllText("dump.txt");

            return JsonConvert.DeserializeObject<Responses.DataResponse>(resp_json);
        }

        public static async Task<bool> DeleteAsync(string auth_url, string db_url, string username, string password, string sid)
        {
            string payload = JsonConvert.SerializeObject(new DeleteRequest()
            {
                sid = sid,
            });

            var stream = await Generic.ReadStreamTransactionAsync(
                auth_url,
                string.Format("{0}/delete", db_url),
                username,
                password,
                payload
            );

            return JsonConvert.DeserializeObject<DeleteResponse>(new StreamReader(stream).ReadToEnd()).success;
        }
    }
}