using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
const string TRES_DIR = @"C:\PostInstall\TimerResolution";

string? GetArgValue(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    if (index >= 0 && index < args.Length - 1)
        return args[index + 1];
    return null;
}
void BMres(int res, int samples)
{
    //first kill settimerres and measuresleep
    KillProcess("SetTimerResolution.exe");
    KillProcess("MeasureSleep.exe");
    Thread.Sleep(150);
    try //try to bench res, settimerres in
    {
        using (Process setTimerRes = new Process())
        {
            setTimerRes.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/c {Path.Combine(TRES_DIR, "SetTimerResolution.exe")} --no-console --resolution {res}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            setTimerRes.Start();
        }

        using (Process MeasureSleep = new Process())
        {
            MeasureSleep.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/c {Path.Combine(TRES_DIR, "MeasureSleep.exe")} --samples {samples}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            MeasureSleep.Start();
            string output = MeasureSleep.StandardOutput.ReadToEnd();
            MeasureSleep.WaitForExit();

            handleResults(output, res);
        }
    } catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
}



//functions to use in bmres (literally copy pasted from https://github.com/HickerDicker/microadjust/blob/main/Form1.cs, thank you hickensa)
void KillProcess(string processName)
{
    foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName)))
    {
        process.Kill();
    }
}

int bestRes = 5000;
double bestMax = 1;
void handleResults(string output, int resolution)
{
    //we only really care about the maximum
    var maxRegex = Regex.Match(output, @"Max: (.*)");
    double max = double.Parse(maxRegex.Groups[1].Value);
    if (max < bestMax)
    {
        Console.WriteLine($"{max} < {bestMax}");
        bestRes = resolution;
        bestMax = max;
    }
}

int minRes = int.Parse(GetArgValue(args, "--minRes") ?? "5000");
int maxRes = int.Parse(GetArgValue(args, "--maxRes") ?? "5100");
int interval = int.Parse(GetArgValue(args, "--interval") ?? "5");
int samples = int.Parse(GetArgValue(args, "--samples") ?? "50");

Console.WriteLine($"minRes: {minRes}, maxRes: {maxRes}, interval: {interval}, samples: {samples}");

for (int currentRes = minRes; currentRes <= maxRes; currentRes += interval)
{
    Console.WriteLine($"Benchmarking: {currentRes}");
    BMres(currentRes, samples);
}

Console.WriteLine(bestRes);
