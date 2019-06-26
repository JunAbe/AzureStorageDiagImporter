// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StorageDiagImporter
{
    public class StorageDiagImporter
    {
        private static readonly string BLOB_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static readonly string CUSTOMLOG_NAME = Environment.GetEnvironmentVariable("CUSTOMLOG_NAME");
        private static readonly string LOG_ANALYTICS_CUSTOMER_ID = Environment.GetEnvironmentVariable("LOG_ANALYTICS_CUSTOMER_ID");
        private static readonly string LOG_ANALYTICS_SHARE_KEY = Environment.GetEnvironmentVariable("LOG_ANALYTICS_SHARE_KEY");

        public async Task<List<IListBlobItem>> ReferenceBlobOfContainerAsync(CloudBlobClient blobClient, string prefix)
        {
            var cloudBlobContainer = blobClient.GetContainerReference("$logs");

            // List the blobs in the container.
            Console.WriteLine("List blobs in container.");
            BlobContinuationToken blobContinuationToken = null;

            var blobItems = new List<IListBlobItem>();
            do
            {
                var results = await cloudBlobContainer.ListBlobsSegmentedAsync(prefix, true, BlobListingDetails.All, null, null, null, null);
                // Get the value of the continuation token returned by the listing call.
                blobContinuationToken = results.ContinuationToken;
                foreach (IListBlobItem item in results.Results)
                {
                    blobItems.Add(item);
                    Console.WriteLine(item.Uri);
                }
            } while (blobContinuationToken != null); // Loop while the continuation token is not null.

            return blobItems;
        }

        private DiagFormatV1 makeDiagFormatV1(string[] datas)
        {
            var diagFormat = new DiagFormatV1();
            diagFormat.versionNumber = datas[0];
            diagFormat.requestStartTime = datas[1];
            diagFormat.operationType = datas[2];
            diagFormat.requestStatus = datas[3];
            diagFormat.httpStatusCode = datas[4];

            if (long.TryParse(datas[5], out long endToEndLatencyInMs))
            {
                diagFormat.endToEndLatencyInMs = endToEndLatencyInMs;
            }

            if (long.TryParse(datas[6], out long serverLatencyInMs))
            {
                diagFormat.serverLatencyInMs = serverLatencyInMs;
            }

            diagFormat.authenticationType = datas[7];
            diagFormat.requesterAccountName = datas[8];
            diagFormat.ownerAccountName = datas[9];
            diagFormat.serviceType = datas[10];
            diagFormat.requestUrl = datas[11];
            diagFormat.requestedObjectKey = datas[12];
            diagFormat.requestIdHeader = datas[13];

            if (int.TryParse(datas[14], out int operationCount))
            {
                diagFormat.operationCount = operationCount;
            }

            diagFormat.requesterIpAddress = datas[15];
            diagFormat.requestVersionHeader = datas[16];

            if (long.TryParse(datas[17], out long requestHeaderSize))
            {
                diagFormat.requestHeaderSize = requestHeaderSize;
            }

            if (long.TryParse(datas[18], out long requestPacketSize))
            {
                diagFormat.requestPacketSize = requestPacketSize;
            }

            if (long.TryParse(datas[19], out long responseHeaderSize))
            {
                diagFormat.responseHeaderSize = responseHeaderSize;
            }

            if (long.TryParse(datas[20], out long responsePacketSize))
            {
                diagFormat.responsePacketSize = responsePacketSize;
            }

            if (long.TryParse(datas[21], out long requestContentLength))
            {
                diagFormat.requestContentLength = requestContentLength;
            }

            diagFormat.requestMd5 = datas[22];
            diagFormat.serverMd5 = datas[23];
            diagFormat.etagIdentifier = datas[24];
            diagFormat.lastModifiedTime = datas[25];
            diagFormat.conditionsUsed = datas[26];
            diagFormat.userAgentHeader = datas[27];
            diagFormat.referrerHeader = datas[28];
            diagFormat.clientRequestId = datas[29];

            return diagFormat;
        }

        private DiagFormatV2 makeDiagFormatV2(string[] datas)
        {
            var diagFormat = new DiagFormatV2();
            diagFormat.versionNumber = datas[0];
            diagFormat.requestStartTime = datas[1];
            diagFormat.operationType = datas[2];
            diagFormat.requestStatus = datas[3];
            diagFormat.httpStatusCode = datas[4];

            if (long.TryParse(datas[5], out long endToEndLatencyInMs))
            {
                diagFormat.endToEndLatencyInMs = endToEndLatencyInMs;
            }

            if (long.TryParse(datas[6], out long serverLatencyInMs))
            {
                diagFormat.serverLatencyInMs = serverLatencyInMs;
            }

            diagFormat.authenticationType = datas[7];
            diagFormat.requesterAccountName = datas[8];
            diagFormat.ownerAccountName = datas[9];
            diagFormat.serviceType = datas[10];
            diagFormat.requestUrl = datas[11];
            diagFormat.requestedObjectKey = datas[12];
            diagFormat.requestIdHeader = datas[13];

            if (int.TryParse(datas[14], out int operationCount))
            {
                diagFormat.operationCount = operationCount;
            }

            diagFormat.requesterIpAddress = datas[15];
            diagFormat.requestVersionHeader = datas[16];

            if (long.TryParse(datas[17], out long requestHeaderSize))
            {
                diagFormat.requestHeaderSize = requestHeaderSize;
            }

            if (long.TryParse(datas[18], out long requestPacketSize))
            {
                diagFormat.requestPacketSize = requestPacketSize;
            }

            if (long.TryParse(datas[19], out long responseHeaderSize))
            {
                diagFormat.responseHeaderSize = responseHeaderSize;
            }

            if (long.TryParse(datas[20], out long responsePacketSize))
            {
                diagFormat.responsePacketSize = responsePacketSize;
            }

            if (long.TryParse(datas[21], out long requestContentLength))
            {
                diagFormat.requestContentLength = requestContentLength;
            }

            diagFormat.requestMd5 = datas[22];
            diagFormat.serverMd5 = datas[23];
            diagFormat.etagIdentifier = datas[24];
            diagFormat.lastModifiedTime = datas[25];
            diagFormat.conditionsUsed = datas[26];
            diagFormat.userAgentHeader = datas[27];
            diagFormat.referrerHeader = datas[28];
            diagFormat.clientRequestId = datas[29];
            diagFormat.UserObjectId = datas[30];
            diagFormat.TenantId = datas[31];
            diagFormat.ApplicationId = datas[32];
            diagFormat.ResourceId = datas[33];
            diagFormat.Issuer = datas[34];
            diagFormat.UserPrincipalName = datas[35];
            diagFormat.Reserved = datas[36];
            diagFormat.AuthorizationDetail = datas[37];

            return diagFormat;
        }

        private void PostBlobDiagLog(CloudBlobClient client, string targetFolderPrefix)
        {
            var blobprefix = "blob/" + targetFolderPrefix;
            var logItems = ReferenceBlobOfContainerAsync(client, blobprefix);
            foreach (var logItem in logItems.Result)
            {
                var blockblob = (CloudBlockBlob)logItem;
                using (var stream = blockblob.OpenReadAsync())
                using (StreamReader reader = new StreamReader(stream.Result))
                {
                    // blob は V2 フォーマット対応
                    var diagFormatV2List = new List<DiagFormatV2>();

                    while (!reader.EndOfStream)
                    {
                        var rowdata = reader.ReadLine();
                        var datas = rowdata.Split(';');
                        diagFormatV2List.Add(makeDiagFormatV2(datas));
                    }

                    string bodyStr = JsonConvert.SerializeObject(diagFormatV2List);

                    // LogAnalytics に Post する
                    SendCustomLog(bodyStr);
                }
            }
        }

        private void PostTableDiagLog(CloudBlobClient client, string targetFolderPrefix)
        {
            var blobprefix = "table/" + targetFolderPrefix;
            var logItems = ReferenceBlobOfContainerAsync(client, blobprefix);
            foreach (var logItem in logItems.Result)
            {
                var blockblob = (CloudBlockBlob)logItem;
                using (var stream = blockblob.OpenReadAsync())
                using (StreamReader reader = new StreamReader(stream.Result))
                {
                    // table は V1 フォーマット対応
                    var diagFormatV1List = new List<DiagFormatV1>();

                    while (!reader.EndOfStream)
                    {
                        var rowdata = reader.ReadLine();
                        var datas = rowdata.Split(';');
                        diagFormatV1List.Add(makeDiagFormatV1(datas));
                    }

                    string bodyStr = JsonConvert.SerializeObject(diagFormatV1List);

                    // LogAnalytics に Post する
                    SendCustomLog(bodyStr);
                }
            }
        }

        [FunctionName("StorageDiagImporter")]
        public void Run([TimerTrigger("5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                // 1時間前のフォルダ名が対象
                var targetFolderPrefix = DateTime.UtcNow.AddHours(-1).ToString("yyyy/MM/dd/hh00");

                var storageAccount = CloudStorageAccount.Parse(BLOB_STORAGE_CONNECTION_STRING);
                var blobClient = storageAccount.CreateCloudBlobClient();

                // blob の Diag Log(V2) を送信
                PostBlobDiagLog(blobClient, targetFolderPrefix);

                // table の Diag Log(V1) を送信
                PostTableDiagLog(blobClient, targetFolderPrefix);
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                throw;
            }
        }

        // Update customerId to your Log Analytics workspace ID
        static string customerId = LOG_ANALYTICS_CUSTOMER_ID;

        // For sharedKey, use either the primary or the secondary Connected Sources client authentication key   
        static string sharedKey = LOG_ANALYTICS_SHARE_KEY;

        // LogName is name of the event type that is being submitted to Azure Monitor
        static string LogName = CUSTOMLOG_NAME;

        // You can use an optional field to specify the timestamp from the data. If the time field is not specified, Azure Monitor assumes the time is the message ingestion time
        static string TimeStampField = "";

        static void SendCustomLog(string sendData)
        {
            // Create a hash for the API signature
            var datestring = DateTime.UtcNow.ToString("r");
            var jsonBytes = Encoding.UTF8.GetBytes(sendData);
            string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
            string hashedString = BuildSignature(stringToHash, sharedKey);
            string signature = "SharedKey " + customerId + ":" + hashedString;

            PostData(signature, datestring, sendData);
        }

        // Build the API signature
        public static string BuildSignature(string message, string secret)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Send a request to the POST API endpoint
        public static void PostData(string signature, string date, string json)
        {
            try
            {
                string url = "https://" + customerId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", LogName);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", date);
                client.DefaultRequestHeaders.Add("time-generated-field", TimeStampField);

                HttpContent httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Task<HttpResponseMessage> response = client.PostAsync(new Uri(url), httpContent);

                HttpContent responseContent = response.Result.Content;
                string result = responseContent.ReadAsStringAsync().Result;
                Console.WriteLine("Return Result: " + result);
            }
            catch (Exception excep)
            {
                Console.WriteLine("API Post Exception: " + excep.Message);
            }
        }

        [JsonObject]
        public class DiagFormatV1Body
        {
            [JsonProperty("DiagFormatV1")]
            public List<DiagFormatV1> DiagFormatV1 { get; set; }
        }

        [JsonObject]
        public class DiagFormatV2Body
        {
            [JsonProperty("DiagFormatV2")]
            public List<DiagFormatV2> DiagFormatV2 { get; set; }
        }

        public class DiagFormatV1
        {
            [JsonProperty("versionNumber")]
            public string versionNumber { get; set; }

            [JsonProperty("requestStartTime")]
            public string requestStartTime { get; set; }

            [JsonProperty("operationType")]
            public string operationType { get; set; }

            [JsonProperty("requestStatus")]
            public string requestStatus { get; set; }

            [JsonProperty("httpStatusCode")]
            public string httpStatusCode { get; set; }

            [JsonProperty("endToEndLatencyInMs")]
            public long endToEndLatencyInMs { get; set; }

            [JsonProperty("serverLatencyInMs")]
            public long serverLatencyInMs { get; set; }

            [JsonProperty("authenticationType")]
            public string authenticationType { get; set; }

            [JsonProperty("requesterAccountName")]
            public string requesterAccountName { get; set; }

            [JsonProperty("ownerAccountName")]
            public string ownerAccountName { get; set; }

            [JsonProperty("serviceType")]
            public string serviceType { get; set; }

            [JsonProperty("requestUrl")]
            public string requestUrl { get; set; }

            [JsonProperty("requestedObjectKey")]
            public string requestedObjectKey { get; set; }

            [JsonProperty("requestIdHeader")]
            public string requestIdHeader { get; set; }

            [JsonProperty("operationCount")]
            public int operationCount { get; set; }

            [JsonProperty("requesterIpAddress")]
            public string requesterIpAddress { get; set; }

            [JsonProperty("requestVersionHeader")]
            public string requestVersionHeader { get; set; }

            [JsonProperty("requestHeaderSize")]
            public long requestHeaderSize { get; set; }

            [JsonProperty("requestPacketSize")]
            public long requestPacketSize { get; set; }

            [JsonProperty("responseHeaderSize")]
            public long responseHeaderSize { get; set; }

            [JsonProperty("responsePacketSize")]
            public long responsePacketSize { get; set; }

            [JsonProperty("requestContentLength")]
            public long requestContentLength { get; set; }

            [JsonProperty("requestMd5")]
            public string requestMd5 { get; set; }

            [JsonProperty("serverMd5")]
            public string serverMd5 { get; set; }

            [JsonProperty("etagIdentifier")]
            public string etagIdentifier { get; set; }

            [JsonProperty("lastModifiedTime")]
            public string lastModifiedTime { get; set; }

            [JsonProperty("conditionsUsed")]
            public string conditionsUsed { get; set; }

            [JsonProperty("userAgentHeader")]
            public string userAgentHeader { get; set; }

            [JsonProperty("referrerHeader")]
            public string referrerHeader { get; set; }

            [JsonProperty("clientRequestId")]
            public string clientRequestId { get; set; }
        }

        [JsonObject]
        public class DiagFormatV2 : DiagFormatV1
        {
            [JsonProperty("UserObjectId")]
            public string UserObjectId { get; set; }

            [JsonProperty("TenantId")]
            public string TenantId { get; set; }

            [JsonProperty("ApplicationId")]
            public string ApplicationId { get; set; }

            [JsonProperty("ResourceId")]
            public string ResourceId { get; set; }

            [JsonProperty("Issuer")]
            public string Issuer { get; set; }

            [JsonProperty("UserPrincipalName")]
            public string UserPrincipalName { get; set; }

            [JsonProperty("Reserved")]
            public string Reserved { get; set; }

            [JsonProperty("AuthorizationDetail")]
            public string AuthorizationDetail { get; set; }
        }
    }
}
