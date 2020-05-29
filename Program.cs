using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.IO;
using System.Reflection;

namespace ADOCLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Program p = new Program();
            Console.WriteLine("ADOBlame - built on " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToShortDateString() + " - Send feedback to asklar@microsoft.com");
            LoggerCallbackHandler.UseDefaultLogging = (Environment.GetEnvironmentVariable("BUILD_REPOSITORY_LOCALPATH") != null);
            string tag = "DEP-AppCompat-Triaged";
            int id = -1;

            if (args.Length == 1 && (args[0] == "/h" || args[0] == "/?"))
            {
                Console.WriteLine("Usage: ");
                Console.WriteLine("\tBlameADOTag [/tag <tagname>] [/id <bugid>]");
                Console.WriteLine("\tBlameADOTag [/tag \"<field=value>\"] [/id <bugid>]");
                Console.WriteLine();
                return;
            }
            if (args.Length >= 2)
            {
                for (int i = 0; i < args.Length; i += 2)
                {
                    switch (args[i])
                    {
                        case "/tag":
                            tag = args[i + 1];
                            break;
                        case "/id":
                            id = int.Parse(args[i + 1]);
                            break;
                        case "/debug":
                            AdoConfiguration.Instance.Debug = true;
                            i--; // don't advance 2, only 1.
                            break;
                    }
                }
            }
            if (tag.StartsWith('"') && tag.EndsWith('"'))
            {
                tag = tag.Substring(1, tag.Length - 2);
            }
            ChangeBlameController controller = new ChangeBlameController(tag);
            if (id != -1)
            {
                controller.Id = id;
            }
            controller.Process();
        }
    }
}
