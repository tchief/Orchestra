#r @"tools\FAKE\tools\FakeLib.dll"

open System
open System.IO
open System.Linq
open System.Text
open System.Text.RegularExpressions
open Fake
open Fake.MSTest

// --------------------------------------------------------------------------------------
// Definitions

let srcDir  = @".\src\"
let version = "1.0.0.0"

let outputDir = @".\output\"
let outputDebugDir = outputDir + "debug"
let outputReleaseDir = outputDir + "release"
let testDir = outputDir + "tests"

let outputDebugFiles = !! (outputDebugDir + @"\**\*.*")
                            -- "*.vshost.exe"
let outputReleaseFiles = !! (outputReleaseDir + @"\**\*.*")
                            -- "*.vshost.exe"

let sdkNET40 = srcDir + @"\**\*.NET40.csproj" 
let sdkNET45 = srcDir + @"\**\*.NET45.csproj" 
let sdkSL = srcDir + @"\**\*.SL.csproj" 
let tests = srcDir + @"\**\*.Test.csproj" 
let templates = srcDir + @"\**\*.ProjectTemplates*.csproj" 
let allProjects = srcDir + @"\**\*.csproj" 


let sdkProjects   = !! sdkNET40
                        ++ sdkNET45
                        ++ sdkSL

let testProjects  = !! tests

let otherProjects = !! allProjects
                        -- sdkNET40 
                        -- sdkNET45
                        -- sdkSL
                        -- tests
                        -- templates
                        
// --------------------------------------------------------------------------------------
// Clean build results

Target "CleanDirectories" (fun _ ->
    CleanDirs [outputDebugDir; outputReleaseDir; testDir]
)

Target "DeleteOutputFiles" (fun _ ->
    !! (outputDebugDir + @"\**\*.*")
        ++ (outputReleaseDir + @"\**\*.*")
        ++ (testDir + @"\**\*.*")
        -- "\**\*.vshost.exe"
    |> DeleteFiles
)

Target "DeleteOutputDirectories" (fun _ ->
    CreateDir outputDir
    DirectoryInfo(outputDir).GetDirectories("*", SearchOption.AllDirectories)
    |> Array.filter (fun d -> not (d.GetFiles("*.vshost.exe", SearchOption.AllDirectories).Any()))
    |> Array.map (fun d -> d.FullName)
    |> DeleteDirs
)

// --------------------------------------------------------------------------------------
// Build projects

Target "UpdateAssemblyVersion" (fun _ ->
      let solutionAssemblyInfo = srcDir + "SolutionAssemblyInfo.cs"
      let pattern = Regex("Assembly(|File)Version(\w*)\(.*\)")
      let result = "Assembly$1Version$2(\"" + version + "\")"
      let content = File.ReadAllLines(solutionAssemblyInfo, Encoding.Unicode)
                    |> Array.map(fun line -> pattern.Replace(line, result, 1))
      File.WriteAllLines(solutionAssemblyInfo, content, Encoding.Unicode)
)

Target "BuildSDK" (fun _ ->    
    sdkProjects 
      |> MSBuildRelease "" "Rebuild" 
      |> Log "Build SDK: "
)

Target "BuildOtherProjects" (fun _ ->    
    otherProjects
      |> MSBuildRelease "" "Rebuild" 
      |> Log "Build Other Projects: "
)

Target "BuildTests" (fun _ ->    
    testProjects
      |> MSBuildDebug "" "Rebuild" 
      |> Log "Build Tests: "
)

// --------------------------------------------------------------------------------------
// Run tests

Target "RunTests" (fun _ ->
    ActivateFinalTarget "CloseMSTestRunner"
    CleanDir testDir
    CreateDir testDir

    !! (outputDir + @"\**\*.Test.dll") 
      |> MSTest (fun p ->
                  { p with
                     TimeOut = TimeSpan.FromMinutes 20.
                     ResultsDir = testDir})
)

FinalTarget "CloseMSTestRunner" (fun _ ->  
    ProcessHelper.killProcess "mstest.exe"
)

Target "RunNUnitTests" (fun _ ->
    let nUnitVersion = GetPackageVersion "lib" "NUnit.Runners"
    let nUnitPath = sprintf "lib/NUnit.Runners.%s/Tools" nUnitVersion
    ActivateFinalTarget "CloseNUnitTestRunner"

    !! (outputDir + @"\**\*.Test.dll") 
      |> NUnit (fun p -> 
                 {p with 
                   ToolPath = nUnitPath
                   DisableShadowCopy = true
                   TimeOut = TimeSpan.FromMinutes 20.
                   OutputFile = testDir + @"TestResults.xml"})
)

FinalTarget "CloseNUnitTestRunner" (fun _ ->  
    ProcessHelper.killProcess "nunit-agent.exe"
)

// --------------------------------------------------------------------------------------
// Combined targets

Target "Clean" DoNothing
"DeleteOutputFiles" ==> "DeleteOutputDirectories" ==> "Clean"

Target "Build" DoNothing
"UpdateAssemblyVersion" ==> "Build"
"BuildSDK" ==> "Build"
"BuildOtherProjects" ==> "Build"

Target "Tests" DoNothing
"BuildTests" ==> "RunTests" ==> "RunNUnitTests" ==> "Tests"

Target "All" DoNothing
"Clean" ==> "All"
"Build" ==> "All"
"Tests" ==> "All"
 
RunTargetOrDefault "All"