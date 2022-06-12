using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace GitlabCloner
{
    public class GitlabREST
    {
        public class Group
        {
            public int ID = 0;
            public Dictionary<string, Group> SubGroups = new Dictionary<string, Group>();
            public Dictionary<string, GitlabProject> Projects = new Dictionary<string, GitlabProject>();
        }
        public class GitlabGroup
        {
            public int id { get; set; }
            public string name { get; set; }
            public string path { get; set; }
            public string description { get; set; }
            public string visibility { get; set; }
            public bool lfs_enabled { get; set; }
            public object avatar_url { get; set; }
            public string web_url { get; set; }
            public bool request_access_enabled { get; set; }
            public string full_name { get; set; }
            public string full_path { get; set; }
            public int? parent_id { get; set; }
        }
        public class GroupAccess
        {
            public int access_level { get; set; }
            public int notification_level { get; set; }
        }

        public class Namespace
        {
            public int id { get; set; }
            public string name { get; set; }
            public string path { get; set; }
            public string kind { get; set; }
            public string full_path { get; set; }
        }

        public class Permissions
        {
            public object project_access { get; set; }
            public GroupAccess group_access { get; set; }
        }

        public class GitlabProject
        {
            public int id { get; set; }
            public string description { get; set; }
            public string default_branch { get; set; }
            public List<object> tag_list { get; set; }
            public bool archived { get; set; }
            public string visibility { get; set; }
            public string ssh_url_to_repo { get; set; }
            public string http_url_to_repo { get; set; }
            public string web_url { get; set; }
            public string name { get; set; }
            public string name_with_namespace { get; set; }
            public string path { get; set; }
            public string path_with_namespace { get; set; }
            public bool container_registry_enabled { get; set; }
            public bool issues_enabled { get; set; }
            public bool merge_requests_enabled { get; set; }
            public bool wiki_enabled { get; set; }
            public bool jobs_enabled { get; set; }
            public bool snippets_enabled { get; set; }
            public DateTime created_at { get; set; }
            public DateTime last_activity_at { get; set; }
            public bool shared_runners_enabled { get; set; }
            public bool lfs_enabled { get; set; }
            public int creator_id { get; set; }
            public Namespace @namespace { get; set; }
            public string import_status { get; set; }
            public object avatar_url { get; set; }
            public int star_count { get; set; }
            public int forks_count { get; set; }
            public int open_issues_count { get; set; }
            public bool public_jobs { get; set; }
            public List<object> shared_with_groups { get; set; }
            public bool only_allow_merge_if_pipeline_succeeds { get; set; }
            public bool request_access_enabled { get; set; }
            public bool only_allow_merge_if_all_discussions_are_resolved { get; set; }
            public Permissions permissions { get; set; }
        }

        HttpClient HttpCli = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
        });

        string DoGET(string Path)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, URL + "api/v4/" + Path);
            req.Headers.TryAddWithoutValidation("PRIVATE-TOKEN", Token);
            var rsp = HttpCli.SendAsync(req);
            rsp.Wait();
            if (!rsp.IsCompletedSuccessfully)
                return "";
            if (rsp.Result.StatusCode != System.Net.HttpStatusCode.OK)
                return "";
            var cntnt = rsp.Result.Content.ReadAsStringAsync();
            cntnt.Wait();
            return cntnt.Result;
        }

        string DoPOST(string Path, string Content)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, URL + "api/v4/" + Path);
            req.Headers.TryAddWithoutValidation("PRIVATE-TOKEN", Token);
            req.Content = new StringContent(Content, Encoding.UTF8, "application/json");

            var rsp = HttpCli.SendAsync(req);
            rsp.Wait();
            if (!rsp.IsCompletedSuccessfully)
                return "";
            if ((rsp.Result.StatusCode == System.Net.HttpStatusCode.OK) ||
                (rsp.Result.StatusCode == System.Net.HttpStatusCode.Created))
            {
                var cntnt = rsp.Result.Content.ReadAsStringAsync();
                cntnt.Wait();
                return cntnt.Result;
            }
            else
            {
                return "";
            }
        }

        bool AddGroup(string Path, int ID)
        {
            string[] sp = Path.Split('/');

            Group g = null;
            for (int i = 0; i < sp.Length; i++)
            {
                if (i == 0)
                {
                    if (!Groups.ContainsKey(sp[i]))
                    {
                        Groups.Add(sp[i], new Group());
                    }
                    g = Groups[sp[i]];
                }
                else
                {
                    if (!g.SubGroups.ContainsKey(sp[i]))
                    {
                        g.SubGroups.Add(sp[i], new Group());
                    }
                    g = g.SubGroups[sp[i]];
                }

                if (i == (sp.Length - 1))
                {
                    g.ID = ID;
                }
            }
            return true;
        }
        bool AddProjectToGroups(GitlabProject Project)
        {
            string[] sp = Project.path_with_namespace.Split('/');
            Group g = null;
            for (int i=0;i<sp.Length;i++)
            {
                if (i==sp.Length-1)
                {
                    if (g.Projects.ContainsKey(sp[i]))
                        return false;
                    g.Projects.Add(sp[i], Project);
                }
                else
                if (i==0)
                {
                    if (!Groups.ContainsKey(sp[i]))
                        return false;
                    g = Groups[sp[0]];
                }
                else
                {
                    if (!g.SubGroups.ContainsKey(sp[i]))
                        return false;
                    g = g.SubGroups[sp[i]];
                }
            }
            return true;
        }

        public bool Init(string inURL, string inPrivateToken)
        {
            URL = new Uri(inURL).AbsoluteUri;
            Token = inPrivateToken;

            int page = 1;
            while(true)
            {
                List<GitlabGroup> p = JsonSerializer.Deserialize<List<GitlabGroup>>(DoGET("groups/?page=" + page));
                if (p.Count == 0)
                    break;
                foreach(var g in p)
                {
                    AddGroup(g.full_path, g.id);
                }
                page++;
            }

            page = 1;
            while (true)
            {
                List<GitlabProject> p = JsonSerializer.Deserialize<List<GitlabProject>>(DoGET("projects/?page=" + page));
                if (p.Count == 0)
                    break;
                foreach(var t in p)
                {
                    if (AddProjectToGroups(t))
                        Projects.Add(t);
                }
                page++;
            }
            return true;
        }

        bool intCreateGroup(string Name)
        {
            Console.Write("(add:" + Name + ")");
            GitlabGroup newG = JsonSerializer.Deserialize<GitlabGroup>(DoPOST("groups", "{\"path\": \"" + Name + "\", \"name\": \"" + Name + "\", \"parent_id\": \"\" }"));
            Groups.Add(Name, new Group());
            Groups[Name].ID = newG.id;

            return true;
        }
        bool intCreateSubGroup(Group parent, string Name)
        {
            Console.Write("(add:" + Name + ")");
            GitlabGroup newG = JsonSerializer.Deserialize<GitlabGroup>(DoPOST("groups", "{\"path\": \"" + Name + "\", \"name\": \"" + Name + "\", \"parent_id\": \"" + parent.ID + "\" }"));
            parent.SubGroups.Add(Name, new Group());
            parent.SubGroups[Name].ID = newG.id;

            return true;
        }
        bool intCreateProject(Group parent, string Name)
        {
            if (parent.Projects.ContainsKey(Name))
            {
                Console.Write("(present)");
                return true;
            }
            Console.Write("(add prj:" + Name + ")");
            string cntnt = "{\"name\": \"" + Name + "\", \"description\": \"\", \"path\": \"" + Name + "\", \"namespace_id\": \"" + (parent != null ? parent.ID : "") + "\", \"initialize_with_readme\": \"false\"}";
            GitlabProject p = JsonSerializer.Deserialize<GitlabProject>(DoPOST("projects", cntnt));
            Projects.Add(p);
            AddProjectToGroups(p);
            return true;
        }

        public bool CreateProject(string Path)
        {
            Group g = null;
            string[] sp = Path.Split('/');
            for (int i = 0; i < sp.Length; i++)
            {
                if (i==sp.Length-1)
                {
                    if (!intCreateProject(g, sp[i]))
                        return false;
                }
                else
                if (i==0)
                {
                    if (!Groups.ContainsKey(sp[i]))
                    {
                        if (!intCreateGroup(sp[i]))
                            return false;
                    }
                    g = Groups[sp[i]];
                }
                else
                {
                    if (g == null)
                        return false;
                    if (!g.SubGroups.ContainsKey(sp[i]))
                    {
                        if (!intCreateSubGroup(g, sp[i]))
                            return false;
                    }
                    g = g.SubGroups[sp[i]];
                }
            }
            return true;
        }
        public string GetProjectHTTPURI(string ProjectPath)
        {
            foreach(var p in Projects)
            {
                if (p.path_with_namespace == ProjectPath)
                    return p.http_url_to_repo;
            }
            return "";
        }

        public List<GitlabProject> Projects = new List<GitlabProject>();
        public Dictionary<string, Group> Groups = new Dictionary<string, Group>();

        string URL = "";
        string Token = "";
    }
}
