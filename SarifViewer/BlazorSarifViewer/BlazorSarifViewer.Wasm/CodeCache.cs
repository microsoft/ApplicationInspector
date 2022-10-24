using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlazorSarifViewer.Wasm
{
    public static class CodeCache
    {
        private static HttpClient client = new HttpClient();
        private static ConcurrentDictionary<string, CachedCode> Cache = new ConcurrentDictionary<string, CachedCode>();
        public static CachedCode Get(string owner, string repo, string revision, string path)
        {
            var query = $"https://raw.githubusercontent.com/{owner}/{repo}/{revision}/{path}";
            if (Cache.ContainsKey(query))
            {
                return Cache[query];
            }
            var code = new CachedCode();
            Cache[query] = code;
            Task.Run(async () =>
            {
                var res = await client.GetAsync(query);
                if (res.IsSuccessStatusCode)
                {
                    code.Content = await res.Content.ReadAsStringAsync();
                    code.Status = CacheStatus.Fetched;
                }
                else
                {
                    code.Content = "Failed to fetch code from GitHub.";
                    code.Status = CacheStatus.Failed;
                }
            });
            return code;
        }
    }

    public class CachedCode
    {
        public string Content { get; set; }

        private CacheStatus _status = CacheStatus.Pending;
        public CacheStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                foreach (var callback in Callbacks) { callback(); }
            }
        }
        public List<Action> Callbacks { get; set; } = new List<Action>();
    }

    public enum CacheStatus
    {
        Pending,
        Fetched,
        Failed
    }
}
