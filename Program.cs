using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace GitlabCloner
{
    class Program
    {
        public static void RecursiveDelete(string targetDir)
        {
            File.SetAttributes(targetDir, FileAttributes.Normal);

            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                RecursiveDelete(dir);
            }

            Directory.Delete(targetDir, false);
        }
        static void RunCmd(string Folder, string CMD, string Arguments)
        {
            // Start the child process.
            using Process p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = CMD;
            p.StartInfo.Arguments = Arguments;
            p.StartInfo.WorkingDirectory = Folder;
            p.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            // p.WaitForExit();
            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Create a clone of a Gitlab server.");
            if (args.Length != 4)
            {
                Console.WriteLine("SourceURI SourceApiToken DestinationURI DestinationApiToken");
                return;
            }

            using HttpClient HttpCli = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
            });

            GitlabREST src = new GitlabREST();
            if (!src.Init(args[0], args[1]))
            {
                return;
            }
            GitlabREST dst = new GitlabREST();
            if (!dst.Init(args[2], args[3]))
            {
                return;
            }

            string cdir = System.IO.Directory.GetCurrentDirectory();
            string tmpDirRoo = cdir + "\\clonetmp\\";
            if (System.IO.Directory.Exists(tmpDirRoo))
            {
                Console.Write("Clean up local files..");
                RecursiveDelete(tmpDirRoo);
            }
            System.IO.Directory.CreateDirectory(tmpDirRoo);
            int idx = 1;
            foreach(var p in src.Projects)
            {
                Console.Write("Cloning (" + idx + "): " + p.path_with_namespace);
                if (!dst.CreateProject(p.path_with_namespace))
                    return;
                Console.Write("(git clone)");

                string destURI = dst.GetProjectHTTPURI(p.path_with_namespace);
                if (destURI.Length > 5)
                {
                    string tmpdir = tmpDirRoo + idx;
                    System.IO.Directory.CreateDirectory(tmpdir);
                    System.IO.Directory.SetCurrentDirectory(tmpdir);

                    RunCmd(tmpdir, "git", "clone " + p.http_url_to_repo);

                    tmpdir = tmpdir + "\\" + p.name;
                    System.IO.Directory.SetCurrentDirectory(tmpdir);

                    RunCmd(tmpdir, "git", "remote set-url origin " + destURI);
                    RunCmd(tmpdir, "git", "push --all --progress \"origin\"");
                }
                else
                {
                    Console.Write("...project missing on dest...");
                }

                Console.WriteLine();


                idx++;
            }
            Console.Write("Clean up local files..");
            System.IO.Directory.SetCurrentDirectory(cdir);
            RecursiveDelete(tmpDirRoo);
        }
    }
}
