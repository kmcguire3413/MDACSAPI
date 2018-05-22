using MDACS.API;
using MDACS.API.Requests;
using MDACS.API.Responses;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MDACS.API
{
    public class Session
    {
        public string authUrl { get; }
        public string dbUrl { get; }
        public string cmdUrl { get; }
        public string username { get; }
        public string password { get; }

        public Session(
            string authUrl,
            string dbUrl,
            string cmdUrl,
            string username,
            string password
            )
        {
            this.authUrl = authUrl;
            this.dbUrl = dbUrl;
            this.cmdUrl = cmdUrl;
            this.username = username;
            this.password = password;
        }

        public async Task<bool> ExecuteCommandAsync(
            string serviceId,
            string command) 
        {
            return await Command.ExecuteCommandAsync(authUrl, cmdUrl, serviceId, command, username, password);
        }

        public async Task<Responses.CommandWaitResponse> FetchCommandsAsync(
            string serviceId,
            string serviceGuid,
            int timeout)
        {
            return await Command.FetchCommandsAsync(authUrl, cmdUrl, serviceId, serviceGuid, timeout, username, password);
        }

        public async Task<bool> WriteResponsesAsync(
            string serviceId, 
            string serviceGuid, 
            Dictionary<string, string> responses)
        {
            return await Command.WriteResponsesAsync(authUrl, cmdUrl, serviceId, serviceGuid, username, password, responses);
        }

        public async Task<Responses.CommandResponseReadResponse> ReadResponsesAsync(
            string serviceId,
            string[] commandIds) 
        {
            return await Command.ReadResponsesAsync(authUrl, cmdUrl, serviceId, username, password, commandIds);
        }        

        public async Task<CommitSetResponse> CommitSetAsync(string sid, JObject meta)
        {
            return await Database.CommitSetAsync(
                authUrl,
                dbUrl,
                username,
                password,
                sid,
                meta
            );
        }

        public async Task<HandleBatchSingleOpsResponse> BatchSingleOps(BatchSingleOp[] ops)
        {
            return await Database.BatchSingleOps(
                authUrl,
                dbUrl,
                username,
                password,
                ops
            );
        }

        public async Task<EnumerateConfigurationsResponse> EnumerateConfigurations()
        {
            return await Database.EnumerateConfigurations(
                authUrl,
                dbUrl,
                username,
                password
            );
        }

        public async Task<bool> Delete(string sid)
        {
            return await Database.DeleteAsync(
                authUrl,
                dbUrl,
                username,
                password,
                sid
            );
        }

        public async Task<DeviceConfigResponse> DeviceConfig(string deviceid, string current_config_data)
        {
            return await Database.DeviceConfig(
                authUrl,
                dbUrl,
                username,
                password,
                deviceid,
                current_config_data
            );
        }

        public async Task<DataResponse> Data()
        {
            return await Database.GetDataAsync(
                authUrl,
                dbUrl,
                username,
                password,
                null
            );
        }

        public async Task<CommitConfigurationResponse> CommitConfigurationAsync(
            string deviceid,
            string userid,
            string config_data
        )
        {
            return await Database.CommitConfiguration(
                authUrl,
                dbUrl,
                username,
                password,
                deviceid,
                userid,
                config_data
            );
        }

        public async Task<UploadResponse> UploadAsync(
            long datasize,
            string datatype,
            string datestr,
            string devicestr,
            string timestr,
            string userstr,
            Stream data
        )
        {
            return await Database.UploadAsync(
                authUrl,
                dbUrl,
                username,
                password,
                datasize,
                datatype,
                datestr,
                devicestr,
                timestr,
                userstr,
                data
            );
        }
    }
}
