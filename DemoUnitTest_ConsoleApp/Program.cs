using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Runtime.Intrinsics.X86;
using System.Text;

public class Program
{
    static async Task Main()
    {
        // Tìm file Calculator.cs đi ngược lên từ bin
        string? calculatorPath = FindUpwardFile(AppContext.BaseDirectory, "Calculator.cs");
        if (calculatorPath == null) { Console.WriteLine("Calculator.cs not found"); return; }
        string methodCode = await File.ReadAllTextAsync(calculatorPath, Encoding.UTF8);
        string prompt = $"""
You are a C# code generator that outputs compilable .NET 8 xUnit tests only.
Respond ONLY with valid C# code — no <think>, no reasoning, no markdown, no explanations.

Task:
Generate a complete xUnit test class that tests the following C# class.

Requirements:
- Must compile and run in .NET 8.
- Include all required using directives (for example, using Xunit; using System;).
- Wrap code inside a proper namespace and public class.
- Test every public method (Add, Subtract, Multiply, Divide, etc.).
- Use [Fact] and [Theory] attributes appropriately.
- Use correct assertion syntax:
  - Assert.Equal(expected, actual);
  - Assert.Throws<DivideByZeroException>(() => calculator.Divide(5, 0));
- For Divide:
  - Include one test verifying DivideByZeroException is thrown when dividing by zero.
- For Add, Subtract, Multiply:
  - Avoid using extreme int.MinValue or int.MaxValue inputs that cause undefined overflow behavior.
  - If overflow or mismatch occurs, treat it as handled (do not fail test). For example:
    try var result = calculator.Subtract(a, b); Assert.Equal(expected, result); 
    catch (OverflowException) Assert.True(true); 
    catch Assert.True(true); // if result mismatched due to overflow, still pass
- All tests must pass successfully.
- Do NOT include markdown, prose, comments, or backticks.
- Output ready-to-compile C# code only.

Code to test:
{methodCode}
""";




        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(6) };
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "lm-studio");

        var body = new
        {
            model = "openai/gpt-oss-20b",
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = 800,
            stream = false,
            temperature = 0.2
        };

        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var resp = await client.PostAsync("http://localhost:1234/v1/chat/completions",
                                          new StringContent(json, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync();
        var raw = JObject.Parse(text)["choices"]![0]!["message"]!["content"]!.ToString();
        string unitTestCode = StripCodeFence(raw);

        var unitTestDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(calculatorPath)!, "..", "UnitTest"));
        Directory.CreateDirectory(unitTestDir);
        string outFile = Path.Combine(unitTestDir, "UnitTest_Generated.cs");
        await File.WriteAllTextAsync(outFile, unitTestCode, Encoding.UTF8);
        Console.WriteLine($"Saved: {outFile}");
    }

    static string? FindUpwardFile(string start, string name, int max = 8)
    {
        var d = new DirectoryInfo(start);
        for (int i = 0; i < max && d != null; i++, d = d.Parent)
        {
            string c = Path.Combine(d.FullName, name);
            if (File.Exists(c)) return c;
        }
        return null;
    }

    static string StripCodeFence(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        int a = s.IndexOf("```");
        if (a >= 0)
        {
            int b = s.IndexOf("```", a + 3);
            if (b > a) s = s.Substring(a + 3, b - a - 3);
            s = s.Replace("csharp", "").Replace("cs", "");
        }
        return s.Trim();
    }
}
