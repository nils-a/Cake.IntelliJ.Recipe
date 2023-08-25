#addin nuget:?package=Cake.FileHelpers&version=6.1.3

var target = Argument("target", "Default");

Task("clean")
.Does(()=>
{
     var dst = File("./test/Cake.IntelliJ.Recipe.cake");
     FileWriteText(dst, "// Created at " + DateTime.Now + Environment.NewLine);
     var sources = GetFiles("./src/Cake.IntelliJ.Recipe/Content/*.cake");
     FileAppendLines(dst, sources.Select(s => "#l "+s.FullPath).ToArray());

     // clean all tools folders in test.
     CleanDirectories("./test/**/tools");
     CleanDirectories("./test/**/.cake");
});

Task("simple-tests")
.IsDependentOn("clean")
.Does(()=>
{
    CakeExecuteScript("./test/simpleTests/recipe.cake", new CakeSettings
    {
        Verbosity = Context.Log.Verbosity
    });
});

Task("Default")
 .IsDependentOn("clean")
 .IsDependentOn("simple-tests");

RunTarget(target);
