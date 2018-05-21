using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MDACS.API.Requests
{
    class CommandExecuteRequest {
        public string command;
        public string serviceId;
    }

    class CommandWaitRequest {
        /// <summary>
        /// The service identifier represents the service waiting using a human readable
        /// descriptive string. The string should have no whitespace so that it is not
        /// ambigious with the command string that it may be used within.
        /// </summary>
        public string serviceId;
        /// <summary>
        /// An identifier that should be globally unique. It helps to create consistent
        /// suffixes for conflicting service identifiers.
        /// </summary>
        public string serviceGuid;
        /// <summary>
        /// The maximum time to wait in milliseconds for the request before returning a
        /// non-success code.
        /// </summary>
        public int timeout;
    }

    class CommandResponseReadRequest {
        /// <summary>
        /// The list of identifiers representing each command to receive the response for.
        /// </summary>
        public string[] commandIds;
    }

    /// <summary>
    /// Request to provide results for executed commands.
    /// </summary>
    class CommandResponseWriteRequest {
        public string serviceId;
        public string serviceGuid;
        /// <summary>
        /// Provide results using command GUID as the key and the result data as the value.
        /// </summary>
        public Dictionary<string, string> responses;
    }    

    public class DeleteRequest
    {
        public String sid;
    }

    public class AuthUserSetRequest
    {
        public Auth.User user;
    }

    public class AuthUserDeleteRequest
    {
        public string username;
    }

    public class AuthVerifyPayloadRequest
    {
        /// <summary>
        /// The payload hash.
        /// </summary>
        public string phash;
        /// <summary>
        /// The client hash or user's hash.
        /// </summary>
        public string chash;
        /// <summary>
        /// The challenge.
        /// </summary>
        public string challenge;
    }

    public class AuthVerifyRequest
    {
        /// <summary>
        /// The challenge.
        /// </summary>
        public string challenge;
        /// <summary>
        /// The client hash or user's hash.
        /// </summary>
        public string hash;
    }

    public class DeviceConfigRequest
    {
        public String deviceid;
        public String current_config_data;
    }

    public class CommitConfigurationRequest
    {
        public string deviceid;
        public string userid;
        public string config_data;
    }

    public class BatchSingleOp
    {
        public string sid;
        public string field_name;
        public JToken value;
    }

    public class HandleBatchSingleOpsRequest
    {
        public BatchSingleOp[] ops;
    }

    public class UploadHeader
    {
        public string datestr;
        public string timestr;
        public string devicestr;
        public string userstr;
        public string datatype;
        public ulong datasize;
    }

    public class CommitSetRequest
    {
        public string security_id;
        public JObject meta;
    }

    public class UniversalPullInfoRequest
    {
        public string data_hash_sha512;
    }
}
