using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static ConsoleApp1.Program.SolutionBuilder;

namespace ConsoleApp1
{
    internal static class Program
    {
        private static string GetTranslateFileldName(bool translateField, string origName, string type)
        {
            if (translateField)
            {
                string sourceName = origName.Remove(origName.Length - 3, 3);
                return sourceName + origName.Substring(origName.Length - 3, 3);
            }
            else
            {
                string value = origName;
                return value;
            }
        }

        static void Main2()
        {

            Console.WriteLine("Введите путь до папки");

            string rootPath = Console.ReadLine();// @"C:\Users\GTR\source\repos\test";
             
            string solutionName = "MySolution";
            SolutionBuilder solutionBuilder = new SolutionBuilder(rootPath, solutionName);

            var projectFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);

            foreach (var projectFile in projectFiles)
            {
                solutionBuilder.AddProject(projectFile);
            }

            string solutionPath = Path.Combine(rootPath, solutionName + ".sln");
            string result = solutionBuilder.Build();

            File.WriteAllText(solutionPath, result);
        }

        public class SolutionBuilder
        {
            private readonly string _solutionName;
            private readonly string _root;

            public SolutionBuilder(string root, string solutionName)
            {
                _root = root;
                _solutionName = solutionName;
            }

            private List<ProjectFile> _files = new List<ProjectFile>();
            private List<ProjectFolder> _folders = new List<ProjectFolder>();

            public ProjectFolder Solution { get; set; } = new ProjectFolder();

            public class ProjectFolder
            {
                public string Guid { get; set; }

                public string Name { get; set; }

                public List<ProjectFile> Projects { get; set; } = new List<ProjectFile>();

                public List<ProjectFolder> Children { get; set; } = new List<ProjectFolder>();
                public ProjectFolder Parent { get; internal set; }
            }

            public class ProjectFile
            {
                public string ProjectName { get; set; }

                public string FullPath { get; set; }

                public string RelativePath { get; set; }

                public string Guid { get; set; }
            }

            public void AddProject(string projectFile)
            {
                ProjectFile result = new ProjectFile()
                {
                    FullPath = projectFile,
                    RelativePath = projectFile.Substring(_root.Length).TrimStart('\\'),
                    Guid = GetGuidProject(projectFile),
                    ProjectName = Path.GetFileNameWithoutExtension(projectFile),
                };

                if (result.Guid == null)
                    return;

                ProjectFolder folder = EnshureProjectFlder(result);
                folder.Projects.Add(result);

                _files.Add(result);
            }

            private ProjectFolder EnshureProjectFlder(ProjectFile project)
            {
                string[] parts = project.RelativePath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                ProjectFolder currentFolder = Solution;

                for (int i = 0; i < parts.Length - 2; i++)
                {
                    string currentPart = parts[i];
                    ProjectFolder parentFolder = currentFolder;
                    currentFolder = currentFolder.Children.FirstOrDefault(x => x.Name == currentPart);

                    if (currentFolder == null)
                    {
                        currentFolder = new ProjectFolder()
                        {
                            Name = currentPart,
                            Guid = Guid.NewGuid().ToString(),
                            Parent = parentFolder,
                        };
                        _folders.Add(currentFolder);
                        parentFolder.Children.Add(currentFolder);
                    }
                }

                return currentFolder;
            }

            private string GetGuidProject(string projectFile)
            {
                var xmlns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
                string name = Path.GetFileNameWithoutExtension(projectFile);
                string relativePath = projectFile.Substring(_root.Length).TrimStart('\\');
                var doc = XDocument.Load(projectFile);
                var guidElement = doc.Root.Elements(xmlns + "PropertyGroup")
                                          .Elements(xmlns + "ProjectGuid")
                                          .FirstOrDefault();
                if (guidElement == null)
                    return null;

                string guid = guidElement.Value;

                return guid;
            }


            public string Build()
            {
                string globalSections = @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2013
VisualStudioVersion = 12.0.21005.1
MinimumVisualStudioVersion = 10.0.40219.1
#Projects#
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        HideSolutionNode = FALSE
    EndGlobalSection 
	GlobalSection(NestedProjects) = preSolution
#NestedProjects#
    EndGlobalSection
EndGlobal";

                string csharpProjectTemplate = @"Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{0}"", ""{1}"", ""{2}""
EndProject";
                string csharpFolderTemplate = @"Project(""{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}"") = ""{0}"", ""{0}"", ""{1}""
EndProject";

                List<string> linesProjects = new List<string>();
                List<string> linesFolders = new List<string>();

                foreach (var projectFile in _files)
                {
                    string entry = string.Format(csharpProjectTemplate, projectFile.ProjectName, projectFile.RelativePath, projectFile.Guid);
                    linesProjects.Add(entry);
                }

                foreach (var folder in _folders)
                {
                    string entry = string.Format(csharpFolderTemplate, folder.Name, folder.Guid);
                    linesProjects.Add(entry);

                    if (folder.Parent?.Guid != null)
                        linesFolders.Add($"\t\t{folder.Guid} = {folder.Parent.Guid}");

                    foreach (var projectFile in folder.Projects)
                        linesFolders.Add($"\t\t{projectFile.Guid} = {folder.Guid}");
                }


                globalSections = globalSections.Replace("#Projects#", string.Join(Environment.NewLine, linesProjects.ToArray()));
                globalSections = globalSections.Replace("#NestedProjects#", string.Join(Environment.NewLine, linesFolders.ToArray()));

                return globalSections;
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("start");
            try
            {
                Main2();

                //ManualResetEvent _event = new ManualResetEvent(true);
                //_event.Reset();

                //_event.WaitOne();

                //                string res = GetTranslateFileldName(true, "asd en", "asd");

                //                string value = @"

                //<webs>
                //  <web url=""/dms/contracts"">
                //    <lists> 
                //      <list name='test' name2=""test2"">test</list>
                //      <list>[test]</list>
                //      <list>{test}</list>
                //      <list>&lt;Choice&gt;test&lt;/Choice&gt;</list> 
                //    </lists>
                //  </web>
                //</webs> 
                //";
                //                string resultValue = Replase(value, "test", "OK",
                //                    new SymbolPara("'"),
                //                    new SymbolPara("\""),
                //                    new SymbolPara("[", "]"),
                //                    new SymbolPara("{", "}"),
                //                    new SymbolPara(">", "<"),
                //                    new SymbolPara("&gt;", "&lt;")
                //                    );

                //                Console.WriteLine(resultValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("end");
            Console.ReadLine();
        }

        public static string Replase(this string value, string currentValue, string newValue, params SymbolPara[] pars)
        {
            string result = value;
            foreach (SymbolPara para in pars)
                result = Replase(result, currentValue, newValue, para.StartSymbol, para.EndSymbol);

            return result;
        }

        public static string Replase(this string value, string currentValue, string newValue, string startSymbol, string endSymbol)
        {
            int startIndex = value.IndexOf(startSymbol);

            string resultValue = value;

            while (startIndex > -1)
            {
                int endIndex = resultValue.IndexOf(endSymbol, startIndex + startSymbol.Length);
                if (endIndex == -1)
                    break;

                string oldValue = resultValue.Substring(startIndex + startSymbol.Length, endIndex - startIndex - startSymbol.Length);

                if (oldValue == currentValue)
                {
                    resultValue = resultValue
                        .Remove(startIndex + startSymbol.Length, endIndex - startIndex - startSymbol.Length)
                        .Insert(startIndex + startSymbol.Length, newValue);
                }

                startIndex = resultValue.IndexOf(startSymbol, startIndex + startSymbol.Length);
            }

            return resultValue;
        }

        public readonly struct SymbolPara
        {
            public readonly string StartSymbol;
            public readonly string EndSymbol;

            public SymbolPara(string startSymbol, string endSymbol)
            {
                StartSymbol = startSymbol;
                EndSymbol = endSymbol;
            }

            public SymbolPara(string symbol)
            {
                StartSymbol = symbol;
                EndSymbol = symbol;
            }
        }
    }
}
