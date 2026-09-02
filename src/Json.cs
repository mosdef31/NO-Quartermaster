using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Quartermaster
{

    internal sealed class JsonValue
    {
        internal enum Kind { Object, Array, String, Number, Bool, Null }

        internal Kind Type;
        internal Dictionary<string, JsonValue>? Members;
        internal List<JsonValue>? Items;
        internal string? Text;
        internal double Number;
        internal bool Bool;

        internal int Line;

        internal bool IsObject => Type == Kind.Object;
        internal bool IsArray => Type == Kind.Array;

        internal JsonValue? Member(string name)
        {
            if (Members == null) return null;
            return Members.TryGetValue(name, out JsonValue? v) ? v : null;
        }

        internal string AsString(string fallback = "") =>
            Type == Kind.String ? Text ?? fallback : fallback;

        internal double AsNumber(double fallback = 0d) =>
            Type == Kind.Number ? Number : fallback;
    }

    internal sealed class JsonError : Exception
    {
        internal JsonError(string message, int line)
            : base($"line {line}: {message}") { }
    }

    internal static class Json
    {
        internal static JsonValue Parse(string text)
        {
            int i = 0;
            int line = 1;
            JsonValue v = ParseValue(text, ref i, ref line);
            SkipWhitespace(text, ref i, ref line);
            if (i < text.Length)
                throw new JsonError($"unexpected '{text[i]}' after the end of the document", line);
            return v;
        }

        private static JsonValue ParseValue(string s, ref int i, ref int line)
        {
            SkipWhitespace(s, ref i, ref line);
            if (i >= s.Length) throw new JsonError("the document ended early", line);

            int startLine = line;
            char c = s[i];

            JsonValue v = c switch
            {
                '{' => ParseObject(s, ref i, ref line),
                '[' => ParseArray(s, ref i, ref line),
                '"' => new JsonValue { Type = JsonValue.Kind.String, Text = ParseString(s, ref i, ref line) },
                't' or 'f' => ParseBool(s, ref i, line),
                'n' => ParseNull(s, ref i, line),
                _ => ParseNumber(s, ref i, line),
            };

            v.Line = startLine;
            return v;
        }

        private static JsonValue ParseObject(string s, ref int i, ref int line)
        {
            var members = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            i++;

            SkipWhitespace(s, ref i, ref line);
            if (i < s.Length && s[i] == '}')
            {
                i++;
                return new JsonValue { Type = JsonValue.Kind.Object, Members = members };
            }

            while (true)
            {
                SkipWhitespace(s, ref i, ref line);
                if (i >= s.Length) throw new JsonError("the document ended inside an object", line);
                if (s[i] != '"')
                    throw new JsonError($"expected a quoted field name, found '{s[i]}'", line);

                string name = ParseString(s, ref i, ref line);

                SkipWhitespace(s, ref i, ref line);
                if (i >= s.Length || s[i] != ':')
                    throw new JsonError($"expected ':' after \"{name}\"", line);
                i++;

                if (members.ContainsKey(name))
                    QuartermasterPlugin.Log.LogWarning(
                        $"quartermaster.json line {line}: \"{name}\" appears twice in the same "
                        + "block, so the second one is the one that counts.");

                members[name] = ParseValue(s, ref i, ref line);

                SkipWhitespace(s, ref i, ref line);
                if (i >= s.Length) throw new JsonError("the document ended inside an object", line);

                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; break; }

                throw new JsonError($"expected ',' or '}}' after a field, found '{s[i]}'", line);
            }

            return new JsonValue { Type = JsonValue.Kind.Object, Members = members };
        }

        private static JsonValue ParseArray(string s, ref int i, ref int line)
        {
            var items = new List<JsonValue>();
            i++;

            SkipWhitespace(s, ref i, ref line);
            if (i < s.Length && s[i] == ']')
            {
                i++;
                return new JsonValue { Type = JsonValue.Kind.Array, Items = items };
            }

            while (true)
            {
                items.Add(ParseValue(s, ref i, ref line));

                SkipWhitespace(s, ref i, ref line);
                if (i >= s.Length) throw new JsonError("the document ended inside a list", line);

                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; break; }

                throw new JsonError($"expected ',' or ']' in a list, found '{s[i]}'", line);
            }

            return new JsonValue { Type = JsonValue.Kind.Array, Items = items };
        }

        private static string ParseString(string s, ref int i, ref int line)
        {
            i++;
            var sb = new StringBuilder();

            while (true)
            {
                if (i >= s.Length) throw new JsonError("a quoted value was never closed", line);

                char c = s[i++];

                if (c == '"') break;

                if (c == '\n')
                    throw new JsonError("a quoted value ran to the end of the line", line);

                if (c != '\\') { sb.Append(c); continue; }

                if (i >= s.Length) throw new JsonError("the document ended after a backslash", line);

                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length)
                            throw new JsonError(@"\u needs four hex digits after it", line);
                        if (!int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber,
                                          CultureInfo.InvariantCulture, out int code))
                            throw new JsonError($@"\u{s.Substring(i, 4)} is not four hex digits", line);
                        sb.Append((char)code);
                        i += 4;
                        break;
                    default:

                        throw new JsonError(
                            $"\\{e} is not an escape. A Windows path needs its backslashes "
                            + @"doubled - ""icons\\battery.png"" - or write it with forward "
                            + @"slashes - ""icons/battery.png""", line);
                }
            }

            return sb.ToString();
        }

        private static JsonValue ParseBool(string s, ref int i, int line)
        {
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0)
            {
                i += 4;
                return new JsonValue { Type = JsonValue.Kind.Bool, Bool = true };
            }

            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0)
            {
                i += 5;
                return new JsonValue { Type = JsonValue.Kind.Bool, Bool = false };
            }

            throw new JsonError("expected true or false", line);
        }

        private static JsonValue ParseNull(string s, ref int i, int line)
        {
            if (string.CompareOrdinal(s, i, "null", 0, 4) != 0)
                throw new JsonError("expected null", line);

            i += 4;
            return new JsonValue { Type = JsonValue.Kind.Null };
        }

        private static JsonValue ParseNumber(string s, ref int i, int line)
        {
            int start = i;

            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.'
                                    || s[i] == 'e' || s[i] == 'E'
                                    || s[i] == '-' || s[i] == '+')) i++;

            string token = s.Substring(start, i - start);

            if (token.Length == 0 || !double.TryParse(token, NumberStyles.Float,
                                                      CultureInfo.InvariantCulture, out double n))
                throw new JsonError(
                    token.Length == 0
                        ? $"expected a value, found '{(start < s.Length ? s[start] : ' ')}'"
                        : $"'{token}' is not a number", line);

            return new JsonValue { Type = JsonValue.Kind.Number, Number = n };
        }

        private static void SkipWhitespace(string s, ref int i, ref int line)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\n') { line++; i++; continue; }
                if (c == ' ' || c == '\t' || c == '\r') { i++; continue; }

                if (c == '/')
                    throw new JsonError(
                        "comments are not allowed in this file. Delete the line, or turn it "
                        + "into a field the mod ignores", line);

                return;
            }
        }
    }
}
