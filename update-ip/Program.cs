using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace update_ip
{
    public class update_ip
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine($"{DateTime.Now} Start app update ip Cloudflare...");

            Config config = new Config();
            GetConfig(config);

            List<string> getPublicIPs = new List<string>
            {
                "https://icanhazip.com",
                "https://ifconfig.me/ip",
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://ifconfig.me/ip",
                "https://api.ipify.org"
            };

            while (true)
            {
                string lastIP = await GetLastIP();
                string currentIP = await GetPublicIP(getPublicIPs, config);

                if (lastIP.Equals(currentIP))
                {
                    await Task.Delay(config.IntervalTime);
                    continue;
                }

                var zoneId = await GetZoneID(config);
                var records = await GetRecords(zoneId, config);

                foreach (var record in records)
                {
                    if (record.type.Equals("A") && (config.SubDomains.Count == 0 || config.SubDomains.Any(s => record.name.Equals(s.domainName))) && !currentIP.Equals(record.content))
                    {
                        await UpdateDNS(currentIP, record, zoneId, config);
                    }
                }
                await File.WriteAllTextAsync("ip.txt", currentIP);
                await Task.Delay(config.IntervalTime);
            }
        }

        private static void GetConfig(Config config)
        {
            Console.WriteLine($"{DateTime.Now} Start get config");
            string SubDomainString = "";
#if DEBUG

            string configPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "config.json");
            if (File.Exists(configPath))
            {
                string configJson = File.ReadAllText(configPath);
                var configString = JsonSerializer.Deserialize<ConfigString>(configJson);

                config.Domain = configString.Domain;
                SubDomainString = configString.SubDomains;
                config.LoginToken = configString.LoginToken;
                config.IntervalTime = configString.IntervalTime * 1000;
                config.Endpoint = configString.Endpoint;
            }
#else
             config.Domain = Environment.GetEnvironmentVariable("domain");
             SubDomainString = Environment.GetEnvironmentVariable("subDomains");
             config.LoginToken = Environment.GetEnvironmentVariable("loginToken");
             config.IntervalTime = int.Parse(Environment.GetEnvironmentVariable("intervalTime")) * 1000;
             config.Endpoint = "https://api.cloudflare.com/client/v4/";
#endif
            var getDomains = SubDomainString.Split(';').ToList();
            foreach (var item in getDomains)
            {
                var getDomain = item.Split(',').ToList();

                SubDomain subomain = new SubDomain();
                subomain.domainName = getDomain[0];
                if (getDomain.Count > 1)
                    subomain.Proxied = string.IsNullOrWhiteSpace(getDomain[1]) ? true : bool.Parse(getDomain[1]);
                config.SubDomains.Add(subomain);
            }
            Console.WriteLine($"{DateTime.Now} Get config --- \nDomain: {config.Domain}, SubDomain: {JsonSerializer.Serialize(config.SubDomains)}, intervalTime: {config.IntervalTime}");
        }

        private static async Task<string> GetLastIP()
        {
            if (File.Exists("ip.txt"))
                return await File.ReadAllTextAsync("ip.txt");
            return await Task.FromResult("");
        }

        private static async Task<string> GetPublicIP(List<string> getPublicIPs, Config config)
        {
            foreach (var url in getPublicIPs)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var response22 = await httpClient.GetAsync(url);
                    if (response22.IsSuccessStatusCode)
                    {
                        var ipAddressString = await response22.Content.ReadAsStringAsync();
                        return ipAddressString.Trim().ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now + " " + ex.Message);
                    await Task.Delay(config.IntervalTime * 2);
                }
            }
            return default;
        }

        private static async Task<string> GetZoneID(Config config)
        {
            var uri = new Uri($"{config.Endpoint}zones?name={config.Domain}");
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + config.LoginToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(uri);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CloudflareApiResponse>(responseContent);

            if (!result.success)
            {
                Console.WriteLine($"{DateTime.Now} No zone found for {config.Domain}");
                return default;
            }
            return result.result[0].id;
        }

        private static async Task<List<Result>> GetRecords(string zoneId, Config config)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + config.LoginToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var uri = new Uri($"{config.Endpoint}zones/{zoneId}/dns_records");

            var response = await client.GetAsync(uri);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Zone>(responseContent);

            if (!result.success)
            {
                Console.WriteLine($"{DateTime.Now} No zone found for {JsonSerializer.Serialize(result)}");
                return default;
            }
            return result.result;
        }

        private static async Task UpdateDNS(string currentIP, Result record, string zoneId, Config config)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + config.LoginToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var updateUri = new Uri($"{config.Endpoint}zones/{zoneId}/dns_records/{record.id}");
            var updateRequest = new HttpRequestMessage(HttpMethod.Put, updateUri);
            updateRequest.Content = new StringContent(JsonSerializer.Serialize(new
            {
                content = currentIP,
                type = "A",
                name = record.name,
                proxied = config.SubDomains.Where(x => x.domainName.Equals(record.name)).FirstOrDefault().Proxied
            }));
            updateRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.SendAsync(updateRequest);

            var responseContent = await response.Content.ReadAsStringAsync();
            var updateResult = JsonSerializer.Deserialize<UpdateIP>(responseContent);

            if (updateResult.success)
            {
                Console.WriteLine($"{DateTime.Now} DNS record updated for: {record.name}, Last IP: {record.content}, current IP: {currentIP}");
            }
            else
            {
                Console.WriteLine($"Failed to update DNS record for {JsonSerializer.Serialize(updateResult)}");
            }
        }
    }
}