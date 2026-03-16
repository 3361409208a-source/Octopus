using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CockroachPet;

public class AiService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string ApiKey = "sk-jgsuebgufkbpsmcsofdckpnzubycmqjxeugysosocimukxiz";
    private const string BaseUrl = "https://api.siliconflow.cn/v1/chat/completions";
    private const string Model = "Qwen/Qwen3-Omni-30B-A3B-Instruct";

    public static async Task<string> GetThoughtAsync(string robotName, string status, string lastAction, string personality)
    {
        try
        {
            var prompt = $"你是像素宠物机器人 {robotName}，性格：{personality}。状态：{status}。动作：{lastAction}。" +
                         "请输出一句极简的中文心里话（10字内）。不要解释。";

            var requestBody = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 32,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return "";

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var result = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return result?.Trim() ?? "";
        }
        catch { return ""; }
    }

    public class ChatResponse
    {
        public string Thought { get; set; } = "";
        public string Answer { get; set; } = "";
    }

    public static async Task<ChatResponse> GetChatResponseAsync(string robotName, string personality, string userMessage, List<(string role, string content)> history)
    {
        try
        {
            var messages = new List<object>
            {
                new { role = "system", content = $"你是{robotName}，性格{personality}。说话要极简，控制在30字内。直接回复，不要带任何自我分析。" }
            };

            foreach (var h in history)
            {
                messages.Add(new { role = h.role, content = h.content });
            }

            messages.Add(new { role = "user", content = userMessage });

            var requestBody = new
            {
                model = Model,
                messages = messages.ToArray(),
                max_tokens = 128,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new ChatResponse { Answer = "（还没睡醒...）" };

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var result = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            
            return SplitAiResponse(result);
        }
        catch { return new ChatResponse { Answer = "（脑回路堵塞...）" }; }
    }

    private static ChatResponse SplitAiResponse(string? input)
    {
        if (string.IsNullOrEmpty(input)) return new ChatResponse();

        // 兼容处理：如果是带 <think> 的模型（虽然现在换成了 Qwen3）
        int thinkEnd = input.IndexOf("</think>");
        if (thinkEnd != -1)
        {
            int thinkStart = input.IndexOf("<think>");
            int actualStart = thinkStart != -1 ? thinkStart + 7 : 0;
            return new ChatResponse { 
                Thought = input.Substring(actualStart, thinkEnd - actualStart).Trim(),
                Answer = input.Substring(thinkEnd + 8).Trim() 
            };
        }

        // Qwen3-Omni 通常直接输出结果，没有思考链
        return new ChatResponse { Answer = input.Trim(' ', '\n', '\r', '\"') };
    }

    private static string CleanAiResponse(string? input)
    {
        var res = SplitAiResponse(input);
        return res.Answer;
    }
}
