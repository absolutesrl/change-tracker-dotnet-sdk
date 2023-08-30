using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ChangeTracker.SDK.Interfaces;
using ChangeTracker.SDK.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ChangeTracker.SDK.Core
{
    public class RemoteChangeStorage : IChangeStorage
    {
        private HttpClient _client = new HttpClient();

        private JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public RemoteChangeStorage(string storageUrl)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            _client.BaseAddress = new Uri(storageUrl);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<StoreChangesResult> StoreChangesAsync(string token, Table table)
        {
            if (table == null) return null;

            var json = JsonConvert.SerializeObject(table, Formatting.None, jsonSerializerSettings);

            var httpContent = new StringContent(json);

            var tableName = Uri.EscapeDataString(table.Name);
            var url = "/?tableName=" + tableName;

            url += "&token=" + Uri.EscapeDataString(token);

            var result = new HttpResponseMessage();
            try
            {
                result = await _client.PostAsync(url, httpContent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var isSuccessResponse = result.StatusCode == HttpStatusCode.OK;

            var ret = new StoreChangesResult
            {
                Ok = isSuccessResponse,
                ErrorText = !isSuccessResponse ? await result.Content.ReadAsStringAsync() : null
            };

            return ret;
        }

        public async Task<GetChangesResult> GetChangesAsync(string token, string tableName, string rowKey = "",
            string paginationToken = "", bool backwardSearch = true)
        {
            var url = "?token=" + Uri.EscapeDataString(token) +
                      (!string.IsNullOrEmpty(tableName) ? "&tableName=" + tableName : "") +
                      (!string.IsNullOrEmpty(rowKey) ? "&rowKey=" + rowKey : "") +
                      (!string.IsNullOrEmpty(paginationToken) ? "&paginationToken=" + paginationToken : "") +
                      (!backwardSearch ? "&backwardSearch=false" : "");

            var results = await _client.GetAsync(url);
            var resultString = await results.Content.ReadAsStringAsync();

            var ret = JsonConvert.DeserializeObject<GetChangesResult>(resultString);

            return ret;
        }
    }
}