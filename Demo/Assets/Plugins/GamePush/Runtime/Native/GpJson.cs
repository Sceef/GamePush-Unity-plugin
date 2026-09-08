using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace GamePush.Native
{
    public static class GpJson
    {
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        public static string Quote(string value) => "\"" + Escape(value ?? "") + "\"";

        public static bool TryGetString(string json, string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return false;
            var needle = "\"" + key + "\"";
            var index = IndexOfKey(json, needle);
            if (index < 0)
                return false;
            index = json.IndexOf(':', index + needle.Length);
            if (index < 0)
                return false;
            index++;
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            if (index >= json.Length)
                return false;
            if (json[index] == '"')
            {
                value = ReadQuoted(json, index + 1, out _);
                return true;
            }
            if (json.Substring(index).StartsWith("null"))
            {
                value = null;
                return true;
            }
            var end = index;
            while (end < json.Length && json[end] != ',' && json[end] != '}' && json[end] != ']')
                end++;
            value = json.Substring(index, end - index).Trim();
            return true;
        }

        public static int GetInt(string json, string key, int fallback = 0)
        {
            if (!TryGetString(json, key, out var raw) || string.IsNullOrEmpty(raw))
                return fallback;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        public static float GetFloat(string json, string key, float fallback = 0)
        {
            if (!TryGetString(json, key, out var raw) || string.IsNullOrEmpty(raw))
                return fallback;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        public static long GetLong(string json, string key, long fallback = 0)
        {
            if (!TryGetString(json, key, out var raw) || string.IsNullOrEmpty(raw))
                return fallback;
            return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        public static bool GetBool(string json, string key, bool fallback = false)
        {
            if (!TryGetString(json, key, out var raw) || string.IsNullOrEmpty(raw))
                return fallback;
            return raw == "true" || raw == "True";
        }

        public static string GetObject(string json, string key)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            var needle = "\"" + key + "\"";
            var index = IndexOfKey(json, needle);
            if (index < 0)
                return null;
            index = json.IndexOf(':', index + needle.Length);
            if (index < 0)
                return null;
            index++;
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            if (index >= json.Length)
                return null;
            if (json[index] == '{')
                return SliceBalanced(json, index, '{', '}');
            if (json[index] == '[')
                return SliceBalanced(json, index, '[', ']');
            if (json[index] == '"')
                return "\"" + ReadQuoted(json, index + 1, out _) + "\"";
            return null;
        }

        public static bool IsEmptyObject(string json)
        {
            if (string.IsNullOrEmpty(json))
                return true;
            var i = 0;
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
            if (i >= json.Length)
                return true;
            if (json.IndexOf("null", i, StringComparison.Ordinal) == i)
                return true;
            if (json[i] != '{')
                return false;
            i++;
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
            return i < json.Length && json[i] == '}';
        }

        public static bool TryGetRaw(string json, string key, out string raw)
        {
            raw = null;
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return false;
            var needle = "\"" + key + "\"";
            var index = IndexOfKey(json, needle);
            if (index < 0)
                return false;
            index = json.IndexOf(':', index + needle.Length);
            if (index < 0)
                return false;
            index++;
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            if (index >= json.Length)
                return false;
            if (json[index] == '{')
            {
                raw = SliceBalanced(json, index, '{', '}');
                return true;
            }
            if (json[index] == '[')
            {
                raw = SliceBalanced(json, index, '[', ']');
                return true;
            }
            if (json[index] == '"')
            {
                var start = index;
                ReadQuoted(json, index + 1, out var after);
                raw = json.Substring(start, after - start);
                return true;
            }
            var end = index;
            while (end < json.Length && json[end] != ',' && json[end] != '}' && json[end] != ']')
                end++;
            raw = json.Substring(index, end - index).Trim();
            return raw.Length > 0;
        }

        public static string MergeDelta(string state, string delta)
        {
            if (IsEmptyObject(delta))
                return IsEmptyObject(state) ? "{}" : state;
            if (IsEmptyObject(state))
                return delta;
            var deltaStart = FirstNonSpace(delta);
            if (deltaStart < 0)
                return state;
            if (delta[deltaStart] == '[')
                return delta;
            var stateStart = FirstNonSpace(state);
            if (stateStart < 0 || state[stateStart] != '{')
                return delta;

            var map = new Dictionary<string, string>();
            foreach (var key in ObjectKeys(state))
            {
                if (TryGetRaw(state, key, out var value))
                    map[key] = value;
            }
            foreach (var key in ObjectKeys(delta))
            {
                if (!TryGetRaw(delta, key, out var value))
                    continue;
                if (value == "\"__gp_del__\"")
                {
                    map.Remove(key);
                    continue;
                }
                if (map.TryGetValue(key, out var current) && IsObjectValue(current) && IsObjectValue(value))
                    map[key] = MergeDelta(current, value);
                else
                    map[key] = value;
            }
            var sb = new StringBuilder();
            sb.Append('{');
            var first = true;
            foreach (var pair in map)
            {
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append(Quote(pair.Key));
                sb.Append(':');
                sb.Append(pair.Value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string CalculateDelta(string prev, string next)
        {
            if (string.Equals(prev, next, StringComparison.Ordinal))
                return null;
            if (IsEmptyObject(next) && IsEmptyObject(prev))
                return null;
            if (IsEmptyObject(prev))
                return next;
            var nextIndex = FirstNonSpace(next);
            var prevIndex = FirstNonSpace(prev);
            if (nextIndex < 0)
                return null;
            if (next[nextIndex] != '{' || prevIndex < 0 || prev[prevIndex] != '{')
            {
                if (next[nextIndex] == '[' && prevIndex >= 0 && prev[prevIndex] == '[')
                    return string.Equals(prev.Trim(), next.Trim(), StringComparison.Ordinal) ? null : next;
                return next;
            }

            var nextKeys = ObjectKeys(next);
            var prevKeys = ObjectKeys(prev);
            var sb = new StringBuilder();
            sb.Append('{');
            var first = true;
            var has = false;
            for (var i = 0; i < nextKeys.Count; i++)
            {
                var key = nextKeys[i];
                TryGetRaw(next, key, out var nextValue);
                TryGetRaw(prev, key, out var prevValue);
                var field = CalculateDelta(prevValue, nextValue);
                if (field == null)
                    continue;
                if (!first)
                    sb.Append(',');
                first = false;
                has = true;
                sb.Append(Quote(key));
                sb.Append(':');
                sb.Append(field);
            }
            for (var i = 0; i < prevKeys.Count; i++)
            {
                var key = prevKeys[i];
                var found = false;
                for (var n = 0; n < nextKeys.Count; n++)
                {
                    if (nextKeys[n] != key)
                        continue;
                    found = true;
                    break;
                }
                if (found)
                    continue;
                if (!first)
                    sb.Append(',');
                first = false;
                has = true;
                sb.Append(Quote(key));
                sb.Append(":\"__gp_del__\"");
            }
            if (!has)
                return null;
            sb.Append('}');
            return sb.ToString();
        }

        static int FirstNonSpace(string json)
        {
            if (string.IsNullOrEmpty(json))
                return -1;
            var i = 0;
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
            return i < json.Length ? i : -1;
        }

        static bool IsObjectValue(string raw)
        {
            var i = FirstNonSpace(raw);
            return i >= 0 && raw[i] == '{';
        }

        public static List<string> GetObjectArray(string json, string key)
        {
            var array = GetObject(json, key);
            return SplitArray(array);
        }

        public static List<string> ObjectKeys(string json)
        {
            var keys = new List<string>();
            if (string.IsNullOrEmpty(json))
                return keys;
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start < 0 || end <= start)
                return keys;
            var i = start + 1;
            while (i < end)
            {
                while (i < end && (char.IsWhiteSpace(json[i]) || json[i] == ','))
                    i++;
                if (i >= end || json[i] != '"')
                    break;
                var key = ReadQuoted(json, i + 1, out var afterKey);
                keys.Add(key);
                i = json.IndexOf(':', afterKey);
                if (i < 0)
                    break;
                i++;
                while (i < end && char.IsWhiteSpace(json[i]))
                    i++;
                if (i >= end)
                    break;
                if (json[i] == '{')
                    i += SliceBalanced(json, i, '{', '}').Length;
                else if (json[i] == '[')
                    i += SliceBalanced(json, i, '[', ']').Length;
                else if (json[i] == '"')
                    ReadQuoted(json, i + 1, out i);
                else
                {
                    while (i < end && json[i] != ',' && json[i] != '}')
                        i++;
                }
            }
            return keys;
        }

        public static List<string> SplitArray(string arrayJson)
        {
            var items = new List<string>();
            if (string.IsNullOrEmpty(arrayJson))
                return items;
            var start = arrayJson.IndexOf('[');
            var end = arrayJson.LastIndexOf(']');
            if (start < 0 || end <= start)
                return items;
            var inner = arrayJson.Substring(start + 1, end - start - 1).Trim();
            var i = 0;
            while (i < inner.Length)
            {
                while (i < inner.Length && (char.IsWhiteSpace(inner[i]) || inner[i] == ','))
                    i++;
                if (i >= inner.Length)
                    break;
                if (inner[i] == '{')
                {
                    var obj = SliceBalanced(inner, i, '{', '}');
                    items.Add(obj);
                    i += obj.Length;
                }
                else if (inner[i] == '"')
                {
                    var text = ReadQuoted(inner, i + 1, out var next);
                    items.Add(text);
                    i = next;
                }
                else
                {
                    var next = i;
                    while (next < inner.Length && inner[next] != ',')
                        next++;
                    items.Add(inner.Substring(i, next - i).Trim());
                    i = next;
                }
            }
            return items;
        }

        public static string ReplaceKey(string json, string from, string to)
        {
            if (string.IsNullOrEmpty(json))
                return json;
            return json.Replace("\"" + from + "\"", "\"" + to + "\"");
        }

        public static string SortedStringifyObject(Dictionary<string, object> source)
        {
            if (source == null)
                return "{}";
            var keys = new List<string>(source.Keys);
            keys.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            sb.Append('{');
            for (var i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append(Quote(keys[i]));
                sb.Append(':');
                sb.Append(Stringify(source[keys[i]]));
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string Stringify(object value)
        {
            if (value == null)
                return "null";
            switch (value)
            {
                case string text:
                    return Quote(text);
                case bool flag:
                    return flag ? "true" : "false";
                case int number:
                    return number.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                case double d:
                    return d.ToString(CultureInfo.InvariantCulture);
                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);
                case GpRawJson raw:
                    return string.IsNullOrEmpty(raw.Json) ? "{}" : raw.Json;
                case Dictionary<string, object> obj:
                    return SortedStringifyObject(obj);
                case IList<string> strings:
                    {
                        var sb = new StringBuilder();
                        sb.Append('[');
                        for (var i = 0; i < strings.Count; i++)
                        {
                            if (i > 0) sb.Append(',');
                            sb.Append(Quote(strings[i]));
                        }
                        sb.Append(']');
                        return sb.ToString();
                    }
                default:
                    if (value is Array array)
                    {
                        var sb = new StringBuilder();
                        sb.Append('[');
                        for (var i = 0; i < array.Length; i++)
                        {
                            if (i > 0) sb.Append(',');
                            sb.Append(Stringify(array.GetValue(i)));
                        }
                        sb.Append(']');
                        return sb.ToString();
                    }
                    return Quote(value.ToString());
            }
        }

        static int IndexOfKey(string json, string needle)
        {
            var start = 0;
            while (start < json.Length)
            {
                var index = json.IndexOf(needle, start, StringComparison.Ordinal);
                if (index < 0)
                    return -1;
                if (index == 0 || json[index - 1] != '\\')
                    return index;
                start = index + 1;
            }
            return -1;
        }

        static string ReadQuoted(string json, int start, out int end)
        {
            var sb = new StringBuilder();
            var i = start;
            while (i < json.Length)
            {
                var c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    i++;
                    var escaped = json[i];
                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            sb.Append(escaped);
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            if (i + 4 < json.Length)
                            {
                                var hex = json.Substring(i + 1, 4);
                                if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                                        out var code))
                                    sb.Append((char)code);
                                i += 4;
                            }
                            break;
                        default:
                            sb.Append(escaped);
                            break;
                    }
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    end = i + 1;
                    return sb.ToString();
                }
                sb.Append(c);
                i++;
            }
            end = json.Length;
            return sb.ToString();
        }

        static string SliceBalanced(string json, int start, char open, char close)
        {
            var depth = 0;
            var inString = false;
            for (var i = start; i < json.Length; i++)
            {
                var c = json[i];
                if (inString)
                {
                    if (c == '\\') { i++; continue; }
                    if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') { inString = true; continue; }
                if (c == open) depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                        return json.Substring(start, i - start + 1);
                }
            }
            return json.Substring(start);
        }
    }

    public sealed class GpRawJson
    {
        public string Json;

        public GpRawJson(string json)
        {
            Json = json;
        }
    }
}
