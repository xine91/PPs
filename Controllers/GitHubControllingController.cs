using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace topfact.Pulse.Controllers
{
    public class GitHubControllingController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public GitHubControllingController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("/pulse/Organisation/GitHubControlling/GitHubControlling")]
        [HttpGet("/pulse/GitHubControlling/GitHubControlling")]
        public IActionResult GitHubControlling()
        {
            return View("~/Views/Organisation/GitHubControlling.cshtml");
        }

        [HttpGet("/pulse/Organisation/GitHubControlling/GetPullRequests")]
        [HttpGet("/pulse/GitHubControlling/GetPullRequests")]
        public async Task<IActionResult> GetPullRequests()
        {
            var token = _configuration["GitHub:PersonalAccessToken"];
            var owner = _configuration["GitHub:Owner"];
            var repo  = _configuration["GitHub:Repository"];

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            {
                return Json(new { error = "GitHub-Konfiguration fehlt. Bitte PersonalAccessToken, Owner und Repository in appsettings.json eintragen." });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                client.DefaultRequestHeaders.Add("User-Agent", "topfact.Pulse");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

                var allPrs = new List<JObject>();
                int page = 1;

                while (true)
                {
                    var url = $"https://api.github.com/repos/{owner}/{repo}/pulls?state=open&per_page=100&page={page}";
                    var response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        return Json(new { error = $"GitHub API Fehler {(int)response.StatusCode} beim Aufruf von: {url} — {errorBody}" });
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var page_prs = JsonConvert.DeserializeObject<List<JObject>>(json) ?? [];

                    if (page_prs.Count == 0) break;
                    allPrs.AddRange(page_prs);
                    if (page_prs.Count < 100) break;
                    page++;
                }

                var result = allPrs.Select(pr => new
                {
                    number    = pr["number"]?.Value<int>(),
                    title     = pr["title"]?.Value<string>(),
                    state     = pr["state"]?.Value<string>(),
                    user      = pr["user"]?["login"]?.Value<string>(),
                    createdAt = pr["created_at"]?.Value<DateTime?>(),
                    updatedAt = pr["updated_at"]?.Value<DateTime?>(),
                    url       = pr["html_url"]?.Value<string>(),
                    draft     = pr["draft"]?.Value<bool>() ?? false,
                    labels    = pr["labels"]?.Select(l => l["name"]?.Value<string>()).ToList(),
                    milestone = (pr["milestone"] as JObject)?["title"]?.Value<string>(),
                    assignees = pr["assignees"]?.Select(a => a["login"]?.Value<string>()).ToList(),
                    body      = pr["body"]?.Value<string>(),
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("/pulse/Organisation/GitHubControlling/GetIssues")]
        [HttpGet("/pulse/GitHubControlling/GetIssues")]
        public async Task<IActionResult> GetIssues()
        {
            var token = _configuration["GitHub:PersonalAccessToken"];
            var owner = _configuration["GitHub:Owner"];
            var repo  = _configuration["GitHub:Repository"];

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            {
                return Json(new { error = "GitHub-Konfiguration fehlt. Bitte PersonalAccessToken, Owner und Repository in appsettings.json eintragen." });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                client.DefaultRequestHeaders.Add("User-Agent", "topfact.Pulse");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

                var allIssues = new List<JObject>();
                int page = 1;

                while (true)
                {
                    var url = $"https://api.github.com/repos/{owner}/{repo}/issues?state=open&per_page=100&page={page}";
                    var response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        return Json(new { error = $"GitHub API Fehler {(int)response.StatusCode} beim Aufruf von: {url} — {errorBody}" });
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var pageIssues = JsonConvert.DeserializeObject<List<JObject>>(json) ?? [];

                    if (pageIssues.Count == 0) break;
                    // Issues-Endpoint gibt auch PRs zurück – nur echte Issues behalten
                    var onlyIssues = pageIssues.Where(i => i["pull_request"] == null).ToList();
                    allIssues.AddRange(onlyIssues);
                    if (pageIssues.Count < 100) break;
                    page++;
                }

                var result = allIssues.Select(issue => new
                {
                    number    = issue["number"]?.Value<int>(),
                    title     = issue["title"]?.Value<string>(),
                    user      = issue["user"]?["login"]?.Value<string>(),
                    createdAt = issue["created_at"]?.Value<DateTime?>(),
                    updatedAt = issue["updated_at"]?.Value<DateTime?>(),
                    url       = issue["html_url"]?.Value<string>(),
                    labels    = issue["labels"]?.Select(l => l["name"]?.Value<string>()).ToList(),
                    milestone = (issue["milestone"] as JObject)?["title"]?.Value<string>(),
                    assignees = issue["assignees"]?.Select(a => a["login"]?.Value<string>()).ToList(),
                    comments  = issue["comments"]?.Value<int>() ?? 0,
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>Listet alle Repos des konfigurierten Owners — hilft beim Prüfen des korrekten Repository-Namens.</summary>
        [HttpGet("/pulse/Organisation/GitHubControlling/Diagnose")]
        [HttpGet("/pulse/GitHubControlling/Diagnose")]
        public async Task<IActionResult> Diagnose()
        {
            var token = _configuration["GitHub:PersonalAccessToken"];
            var owner = _configuration["GitHub:Owner"];
            var repo  = _configuration["GitHub:Repository"];

            if (string.IsNullOrWhiteSpace(token))
                return Json(new { error = "Kein PersonalAccessToken konfiguriert." });

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                client.DefaultRequestHeaders.Add("User-Agent", "topfact.Pulse");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

                // Token-Besitzer prüfen
                var meRes  = await client.GetAsync("https://api.github.com/user");
                var meJson = await meRes.Content.ReadAsStringAsync();

                // Repo direkt prüfen
                var repoUrl = $"https://api.github.com/repos/{owner}/{repo}";
                var repoRes  = await client.GetAsync(repoUrl);
                var repoJson = await repoRes.Content.ReadAsStringAsync();

                // Org-Repos auflisten (erste Seite)
                var orgReposUrl = $"https://api.github.com/orgs/{owner}/repos?per_page=50";
                var orgRes  = await client.GetAsync(orgReposUrl);
                var orgJson = await orgRes.Content.ReadAsStringAsync();
                var orgRepos = orgRes.IsSuccessStatusCode
                    ? (JsonConvert.DeserializeObject<List<JObject>>(orgJson) ?? [])
                        .Select(r => r["full_name"]?.Value<string>()).ToList()
                    : new List<string?> { $"Org-Repos nicht abrufbar: {orgRes.StatusCode}" };

                return Json(new
                {
                    configuredOwner     = owner,
                    configuredRepo      = repo,
                    configuredRepoUrl   = repoUrl,
                    repoHttpStatus      = (int)repoRes.StatusCode,
                    repoFound           = repoRes.IsSuccessStatusCode,
                    tokenUser           = meRes.IsSuccessStatusCode
                        ? JsonConvert.DeserializeObject<JObject>(meJson)?["login"]?.Value<string>()
                        : $"Token-Check fehlgeschlagen: {meRes.StatusCode}",
                    availableOrgRepos   = orgRepos,
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
