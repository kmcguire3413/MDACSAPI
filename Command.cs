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
    public class Command {
        public static async Task<bool> ExecuteCommandAsync(
            string authUrl,
            string cmdUrl, 
            string serviceId,
            string command, 
            string username, 
            string password) {

            var payload = JsonConvert.SerializeObject(new Requests.CommandExecuteRequest() {
                command = command,
                serviceId = serviceId,
            });

            var response = await Generic.ReadTypeTransactionAsync<Responses.SuccessBooleanResponse>(authUrl, cmdUrl, username, password, payload);

            return response.success;
        }

        public static async Task<Responses.CommandWaitResponse> FetchCommandsAsync(
            string authUrl,
            string cmdUrl,
            string serviceId,
            string serviceGuid,
            int timeout,
            string username,
            string password) 
        {
            var payload = JsonConvert.SerializeObject(new Requests.CommandWaitRequest() {
                serviceId = serviceId,
                serviceGuid = serviceGuid,
                timeout = timeout,
            });

            return await Generic.ReadTypeTransactionAsync<Responses.CommandWaitResponse>(authUrl, cmdUrl, username, password, payload);
        }

        public static async Task<bool> WriteResponsesAsync(
            string authUrl,
            string cmdUrl, 
            string serviceId, 
            string serviceGuid, 
            string username, 
            string password,
            Dictionary<string, string> responses) 
        {
            var payload = JsonConvert.SerializeObject(new Requests.CommandResponseWriteRequest() {
                serviceId = serviceId,
                serviceGuid = serviceGuid,
                responses = responses,
            });

            var response = await Generic.ReadTypeTransactionAsync<Responses.SuccessBooleanResponse>(authUrl, cmdUrl, username, password, payload);

            return response.success;
        }

        public static async Task<Responses.CommandResponseReadResponse> ReadResponsesAsync(
            string authUrl,
            string cmdUrl,
            string serviceId,
            string username,
            string password,
            string[] commandIds) 
        {
            var payload = JsonConvert.SerializeObject(new Requests.CommandResponseReadRequest() {
                commandIds = commandIds,
            });

            return await Generic.ReadTypeTransactionAsync<Responses.CommandResponseReadResponse>(authUrl, cmdUrl, username, password, payload);
        }
    }
}