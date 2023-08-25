#load nuget:?package=Cake.Recipe&version=3.1.1
#addin nuget:?package=Cake.FileHelpers&version=6.1.3

var standardNotificationMessage = "Version {0} of {1} has just been released, it will be available here https://www.nuget.org/packages/{1}, once package indexing is complete.";

Environment.SetVariableNames();

BuildParameters.SetParameters(
    context: Context,
    buildSystem: BuildSystem,
    masterBranchName: "main",
    sourceDirectoryPath: "./src",
    title: "Cake.IntelliJ.Recipe",
    repositoryOwner: "cake-contrib",
    shouldRunInspectCode: false,
    shouldRunIntegrationTests: true,
    shouldRunCoveralls: false,
    shouldRunCodecov: false,
    shouldRunDotNetCorePack: true,
    gitterMessage: "@/all " + standardNotificationMessage,
    twitterMessage: standardNotificationMessage,
    preferredBuildProviderType: BuildProviderType.GitHubActions,
    preferredBuildAgentOperatingSystem: PlatformFamily.Linux);

BuildParameters.PrintParameters(Context);

ToolSettings.SetToolSettings(context: Context);

BuildParameters.Tasks.CleanTask
    .IsDependentOn("Generate-Version-File")
    .IsDependentOn("Copy-Cake-Recipe-Content");

Task("Copy-Cake-Recipe-Content")
.Does(() =>
{
    var src = Directory("./lib/Cake.Recipe/Source/Cake.Recipe/Content");
    var dst = Directory("./src/Cake.IntelliJ.Recipe/Content/Cake.Recipe");
    var here = Directory("./src/Cake.IntelliJ.Recipe/Content");
    DeleteFiles((dst + File("*.cake")).Path.FullPath);
    DeleteFiles((dst + File("*.cake_ex")).Path.FullPath);
    var files = GetFiles((src + File("*.cake")).Path.FullPath);
    foreach (var file in files)
    {
        var name = file.GetFilename().FullPath;
        var newName = name + "_ex";
        CopyFile(file, dst + File(newName));

        var fileHere = (here + File(name)).Path;
        if(!FileExists(fileHere))
        {
            Warning("Creating new file from Cake.Recipe: "+name);
            FileWriteText(fileHere, "#l ./Cake.Recipe/"+newName);
        }
    }
});

Task("Generate-Version-File")
    .Does<BuildVersion>((context, buildVersion) => {
        var gitTool = context.Tools.Resolve(new[]{"git", "git.exe"});
        var cakeRecipeVersion = "";
        if(gitTool == null) {
            throw new FileNotFoundException("git could not be found on your system.");
        }
        // git submodule update --init
        StartProcess(gitTool, new ProcessSettings {
            Arguments = new ProcessArgumentBuilder()
                .Append("submodule")
                .Append("update")
                .Append("--init")
        });
        StartProcess(gitTool, new ProcessSettings {
            Arguments = new ProcessArgumentBuilder()
                .Append("submodule")
                .Append("status")
                .AppendQuoted(context.MakeAbsolute(File("./lib/Cake.Recipe")).FullPath),
            RedirectStandardOutput = true,
            RedirectedStandardOutputHandler = x => cakeRecipeVersion += x,
        });
        var buildMetaDataCodeGen = TransformText(@"
        public class BuildMetaData
        {
            public static string Date { get; } = ""<%date%>"";
            public static string Version { get; } = ""<%cakeRecipeVersion%>"";
            public static string CakeVersion { get; } = ""<%cakeVersion%>"";
        }
        public class IntelliJBuildMetaData
        {
            public static string Version { get; } = ""<%version%>"";
        }",
        "<%",
        "%>"
        )
   .WithToken("date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
   .WithToken("version", buildVersion.SemVersion)
   .WithToken("cakeVersion", context.GetType().Assembly.GetName().Version)
   .WithToken("cakeRecipeVersion", cakeRecipeVersion.Trim().Split(" ")[0])
   .ToString();

    System.IO.File.WriteAllText(
        "./src/Cake.IntelliJ.Recipe/Content/version.cake",
        buildMetaDataCodeGen
    );
});

Build.RunDotNetCore();
