using System.Text;

namespace EditorBridge.Editor.Server
{
    internal static class JsonHelper
    {
        public static string Object(params (string key, string value)[] pairs)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            for (var i = 0; i < pairs.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                Escape(sb, pairs[i].key);
                sb.Append("\":");
                sb.Append('"');
                Escape(sb, pairs[i].value);
                sb.Append('"');
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string ObjectRaw(params (string key, string rawValue)[] pairs)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            for (var i = 0; i < pairs.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                Escape(sb, pairs[i].key);
                sb.Append("\":");
                sb.Append(pairs[i].rawValue);
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string Error(string message)
        {
            return Object(("error", message));
        }

        static void Escape(StringBuilder sb, string value)
        {
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
        }
    }
}
