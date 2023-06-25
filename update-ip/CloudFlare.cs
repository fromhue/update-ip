using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace update_ip
{
    public class Config
    {
        public int IntervalTime { get; set; }
        public string Endpoint { get; set; }
        public string Domain { get; set; }
        public string LoginToken { get; set; }
        public List<SubDomain> SubDomains { get; set; } = new List<SubDomain>();
    }
    public class ConfigString
    {
        public int IntervalTime { get; set; }
        public string Endpoint { get; set; }
        public string Domain { get; set; }
        public string LoginToken { get; set; }
        public string SubDomains { get; set; }
    }

    public class SubDomain
    {
        public string domainName { get; set; }
        public bool Proxied { get; set; } = true;
    }
    class Error
    {
        public int code { get; set; }
        public string message { get; set; }
    }
    class UpdateIP
    {
        public Result result { get; set; }
        public bool success { get; set; }
        public List<object> errors { get; set; }
        public List<object> messages { get; set; }
    }

    class Zone
    {
        public List<Result> result { get; set; }
        public bool success { get; set; }
        public List<object> errors { get; set; }
        public List<object> messages { get; set; }
    }
    class Result
    {
        public string id { get; set; }
        public string zone_id { get; set; }
        public string zone_name { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string content { get; set; }
        public int ttl { get; set; }
        public DateTime created_on { get; set; }
        public DateTime modified_on { get; set; }
    }

    class CloudflareApiResponse
    {
        public bool success { get; set; }
        public DnsRecord[] result { get; set; }
        public Error[] errors { get; set; }
        public string[] message { get; set; }
    }

    class DnsRecord
    {
        public string id { get; set; }
        public string name { get; set; }
        public string status { get; set; }
    }
}
