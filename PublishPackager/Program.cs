using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;

namespace PublishPackager
{
    public class Program
    {
        public static string s_solutionPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())));
        public static string s_publishPath = Path.Combine(s_solutionPath, "Publish");

        public static string s_gamePath = @"D:\Program Files (x86)\Steam\steamapps\common\AmazingCultivationSimulator";
        public static string s_gameModsRootFolder = "CSharpMods";
        public static string s_gameModsRootPath = Path.Combine(s_gamePath, s_gameModsRootFolder);
        public static string s_modPublishPath = Path.Combine(s_publishPath, "mods");

        public static string[] s_modsList =
         {
             nameof(Mod_AutoUtils)
            ,nameof(Mod_CrystalPowerUp)
            ,nameof(Mod_PaintFu)

         };

        public static string s_managerAppVersionName = "13_0_0";
        public static string s_managerAppName = nameof(UnityModManagerNet.Installer.UnityModManagerApp);
        public static string s_managerAppPath = Path.Combine(s_solutionPath, Path.Combine(s_managerAppName, @"bin\Release"));
        public static string s_managerZipFilePath = Path.Combine(s_publishPath, "Manager_" + s_managerAppVersionName + ".zip");


        static void Main(string[] args)
        {
            {
                if (Directory.Exists(s_publishPath))
                    Directory.Delete(s_publishPath, true);

                Directory.CreateDirectory(s_publishPath);
            }


            {
                var mangerAppfiles = Directory.GetFiles(s_managerAppPath, "*.*")
                            .Where(s => s.EndsWith(".dll") || s.EndsWith(".exe") || s.EndsWith(".xml"));

                var zip = new ZipFile();
                zip.AddFiles(mangerAppfiles, false, "./");
                zip.Save(s_managerZipFilePath);
                Console.WriteLine(string.Format("App: '{0}' packed successfully", s_managerZipFilePath));

            }

            bool result = true;
            {

                Directory.CreateDirectory(s_modPublishPath);

                var modCount = s_modsList.Length;
                for (int i = 0; i < modCount; i++)
                {
                    var modName = s_modsList[i];
                    var modFolderPath = Path.Combine(s_gameModsRootPath, modName);

                    var modDllFilePath = Path.Combine(modFolderPath, modName + ".dll");
                    var modInfoFilePath = Path.Combine(modFolderPath, "info.json");

                    if (File.Exists(modDllFilePath) && File.Exists(modInfoFilePath))
                    {

                        var modPublishPath = Path.Combine(s_publishPath, Path.Combine(s_modPublishPath, modName + ".zip"));
                        using (var zip = new ZipFile())
                        {
                            zip.AddFile(modDllFilePath,modName+"/");
                            zip.AddFile(modInfoFilePath, modName + "/");
                            zip.Save(modPublishPath);
                        }


                        Console.WriteLine(string.Format("Mod: '{0}' packed successfully to {1}", modName, modPublishPath));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: mod '{0}' is not packed", modName));
                        result = false;
                    }
                }

            }

            if (!result) Console.ReadKey();

        }
        private static string[] GetFiles(string sourceFolder, string filters, System.IO.SearchOption searchOption)
        {
            return filters.Split('|').SelectMany(filter => Directory.GetFiles(sourceFolder, filter, searchOption)).ToArray();
        }
    }
}
