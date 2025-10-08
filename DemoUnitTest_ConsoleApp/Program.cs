using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

public class Program
{
    static async Task Main()
    {
        try
        {
            string? calculatorPath = FindUpwardFile(AppContext.BaseDirectory, "Calculator.cs");
            if (calculatorPath == null)
            {
                Console.WriteLine("❌ Calculator.cs not found.");
                return;
            }

            string methodCode = await File.ReadAllTextAsync(calculatorPath, Encoding.UTF8);

            // 🧠 Prompt đặc biệt cho DeepSeek: ép chỉ trả code thuần
            string prompt = $"""
You are a code generator. Respond ONLY with valid C# source code — no <think>, no reasoning, no markdown, no JSON, no explanations.
Generate an xUnit test class for the following C# code.
Requirements:
- Must compile in .NET 8.
- Include 'using Xunit;' and namespace.
- Use [Fact] or [Theory].
- Do not include ``` fences, comments, or natural language.

Code:
{methodCode}
""";

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "lm-studio");

            var body = new
            {
                model = "deepseek/deepseek-r1-0528-qwen3-8b",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 600,
                temperature = 0.2,
                stream = false
            };

            var json = System.Text.Json.JsonSerializer.Serialize(body);
            var resp = await client.PostAsync("http://localhost:1234/v1/chat/completions",
                                              new StringContent(json, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();

            var text = await resp.Content.ReadAsStringAsync();
            string raw = JObject.Parse(text)["choices"]![0]!["message"]!["content"]!.ToString();

            // 🧹 Lọc sạch reasoning và markdown
            string unitTestCode = CleanGeneratedCode(raw);

            // 💾 Ghi file ra thư mục UnitTest
            var unitTestDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(calculatorPath)!, "..", "UnitTest"));
            Directory.CreateDirectory(unitTestDir);
            string outFile = Path.Combine(unitTestDir, "UnitTest_Generated.cs");

            Console.WriteLine("────────────────────────────────────");
            Console.WriteLine("Preview of generated code:");
            Console.WriteLine("────────────────────────────────────");
            Console.WriteLine(unitTestCode);
            Console.WriteLine("────────────────────────────────────");

            await File.WriteAllTextAsync(outFile, unitTestCode, Encoding.UTF8);
            Console.WriteLine($"✅ Saved generated test: {outFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error: {ex.Message}");
        }
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

    // 🧽 Loại bỏ toàn bộ <think>, JSON, markdown, HTML...
    static string CleanGeneratedCode(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;

        s = s.Replace("\r", "").Trim();

        // Xóa reasoning log DeepSeek (<think>...</think>)
        while (s.Contains("<think>") && s.Contains("</think>"))
        {
            int start = s.IndexOf("<think>");
            int end = s.IndexOf("</think>", start + 7);
            if (end > start)
                s = s.Remove(start, end - start + 8);
            else break;
        }

        // Cắt nếu model vẫn in ```csharp
        if (s.Contains("```"))
        {
            int a = s.IndexOf("```");
            int b = s.LastIndexOf("```");
            if (b > a) s = s.Substring(a + 3, b - a - 3);
        }

        // Xóa tag HTML/XML rác
        s = s.Replace("<code>", "").Replace("</code>", "")
             .Replace("<pre>", "").Replace("</pre>", "")
             .Replace("```csharp", "").Replace("```cs", "")
             .Replace("```", "")
             .Trim();

        // Bỏ phần JSON wrapper nếu DeepSeek trả {"text":"..."}
        if (s.StartsWith("{") && s.Contains("\"text\""))
        {
            try
            {
                var j = JObject.Parse(s);
                if (j["text"] != null)
                    s = j["text"]!.ToString();
            }
            catch { /* ignore */ }
        }

        // Lọc ký tự kỳ lạ
        s = s.Replace(">", "").Replace("<", "").Trim();

        return s;
    }
}
