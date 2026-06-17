using System;

namespace Chess.AI
{
    /// <summary>
    /// Claude API configuration sourced from environment variables.
    /// Hardcoded defaults match the project's coding gateway (Volces Ark).
    /// </summary>
    public static class ClaudeConfig
    {
        // Hardcoded fallback — matches ANTHROPIC_BASE_URL/ANTHROPIC_MODEL in
        // the project's Claude Code session. Override at runtime by setting
        // the matching env vars before launching the Unity player.
        public const string DefaultBaseUrl = "https://ark.cn-beijing.volces.com/api/coding";
        public const string DefaultModel  = "ark-code-latest";
        public const string DefaultApiKey = "ark-1381126b-0993-4507-9cb9-abe644b94be6-dedca";
        public const string AnthropicVersion = "2023-06-01";

        public const string EnvApiKey  = "ANTHROPIC_AUTH_TOKEN";
        public const string EnvBaseUrl = "ANTHROPIC_BASE_URL";
        public const string EnvModel   = "ANTHROPIC_MODEL";

        public static string ApiKey
        {
            get
            {
                var v = Environment.GetEnvironmentVariable(EnvApiKey);
                return string.IsNullOrWhiteSpace(v) ? DefaultApiKey : v.Trim();
            }
        }

        public static string BaseUrl
        {
            get
            {
                var v = Environment.GetEnvironmentVariable(EnvBaseUrl);
                return string.IsNullOrWhiteSpace(v) ? DefaultBaseUrl : v.Trim().TrimEnd('/');
            }
        }

        public static string Model
        {
            get
            {
                var v = Environment.GetEnvironmentVariable(EnvModel);
                return string.IsNullOrWhiteSpace(v) ? DefaultModel : v.Trim();
            }
        }

        public static string MessagesUrl => BaseUrl + "/v1/messages";
    }
}
