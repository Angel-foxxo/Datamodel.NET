using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.IO;

namespace Datamodel.Codecs
{
    [CodecFormat("keyvalues2", 1)]
    [CodecFormat("keyvalues2", 2)]
    [CodecFormat("keyvalues2", 3)]
    [CodecFormat("keyvalues2", 4)]
    [CodecFormat("keyvalues2_noids", 1)]
    [CodecFormat("keyvalues2_noids", 2)]
    [CodecFormat("keyvalues2_noids", 3)]
    [CodecFormat("keyvalues2_noids", 4)]
    class KeyValues2 : ICodec, IDisposable
    {
        TextReader Reader;
        KV2Writer Writer;
        Datamodel DM;

        static readonly Dictionary<Type, string> TypeNames = new();
        static readonly Dictionary<int, Type[]> ValidAttributes = new();
        static KeyValues2()
        {
            TypeNames[typeof(Element)] = "element";
            TypeNames[typeof(int)] = "int";
            TypeNames[typeof(float)] = "float";
            TypeNames[typeof(bool)] = "bool";
            TypeNames[typeof(string)] = "string";
            TypeNames[typeof(byte[])] = "binary";
            TypeNames[typeof(TimeSpan)] = "time";
            TypeNames[typeof(Color)] = "color";
            TypeNames[typeof(Vector2)] = "vector2";
            TypeNames[typeof(Vector3)] = "vector3";
            TypeNames[typeof(Vector4)] = "vector4";
            TypeNames[typeof(Quaternion)] = "quaternion";
            TypeNames[typeof(Matrix4x4)] = "matrix";

            ValidAttributes[1] = ValidAttributes[2] = ValidAttributes[3] = TypeNames.Select(kv => kv.Key).ToArray();

            TypeNames[typeof(byte)] = "uint8";
            TypeNames[typeof(ulong)] = "uint64";
            TypeNames[typeof(QAngle)] = "qangle";

            ValidAttributes[4] = TypeNames.Select(kv => kv.Key).ToArray();
        }

        public void Dispose()
        {
            Reader?.Dispose();
            Writer?.Dispose();
        }

        #region Encode
        class KV2Writer : IDisposable
        {
            public int Indent
            {
                get { return indent_count; }
                set
                {
                    indent_count = value;
                    indent_string = "\n" + string.Concat(Enumerable.Repeat("    ", value));
                }
            }
            int indent_count = 0;
            string indent_string = "\n";
            readonly TextWriter Output;

            public KV2Writer(Stream output)
            {
                Output = new StreamWriter(output, Datamodel.TextEncoding);
            }

            public void Dispose()
            {
                Output.Dispose();
            }

            static string Sanitise(string value)
            {
                return value?.Replace("\"", "\\\"");
            }

            /// <summary>
            /// Writes the string straight to the output steam, with no sanitisation.
            /// </summary>
            public void Write(string value)
            {
                Output.Write(value);
            }

            public void WriteTokens(params string[] values)
            {
                Output.Write('"' + string.Join("\" \"", values.Select(s => Sanitise(s))) + '"');
            }

            public void WriteLine()
            {
                Output.Write(indent_string);
            }

            /// <summary>
            /// Writes a new line followed by the given value
            /// </summary>
            public void WriteLine(string value)
            {
                WriteLine();
                Output.Write(value);
            }

            public void WriteTokenLine(params string[] values)
            {
                Output.Write(indent_string);
                WriteTokens(values);
            }

            public void TrimEnd(int count)
            {
                if (count > 0)
                {
                    Output.Flush();
                    var stream = ((StreamWriter)Output).BaseStream;
                    stream.SetLength(stream.Length - count);
                }
            }

            public void Flush()
            {
                Output.Flush();
            }
        }

        string Encoding;
        int EncodingVersion;

        // Multi-referenced elements are written out as a separate block at the end of the file.
        // In-line only the id is written.
        Dictionary<Element, int> ReferenceCount;

        bool SupportsReferenceIds;

        void CountReferences(Element elem)
        {
            if (ReferenceCount.ContainsKey(elem))
                ReferenceCount[elem]++;
            else
            {
                ReferenceCount[elem] = 1;
                foreach (var attr in elem.GetAllAttributesForSerialization())
                {
                    if (attr.Value == null)
                        continue;

                    if (attr.Value is Element child_elem)
                    {
                        CountReferences(child_elem);
                    }
                    else if (attr.Value is IEnumerable<Element> enumerable)
                    {
                        foreach (var array_elem in enumerable.Where(c => c != null))
                            CountReferences(array_elem);
                    }
                }
            }
        }

        void WriteAttribute(string name, Type type, object value, bool in_array)
        {
            bool is_element = type == typeof(Element) || type.IsSubclassOf(typeof(Element));

            Type inner_type = null;
            if (!in_array)
            {
                // TODO: subclass check in this method like above - and in all other places with == typeof(Element)
                inner_type = Datamodel.GetArrayInnerType(type);

                if (inner_type == typeof(byte) && type == typeof(byte[]))
                    inner_type = null; // serialize as binary at all times

                /*
                if (inner_type == typeof(byte) && !ValidAttributes[EncodingVersion].Contains(typeof(byte)))
                    inner_type = null; // fall back on the "binary" type in older KV2 versions
                */
            }

            // Elements are supported by all.
            if (!is_element && !ValidAttributes[EncodingVersion].Contains(inner_type ?? type))
                throw new CodecException(type.Name + " is not valid in KeyValues2 " + EncodingVersion);

            if (inner_type != null)
            {
                is_element = inner_type == typeof(Element);

                Writer.WriteTokenLine(name, TypeNames[inner_type] + "_array");

                if (((System.Collections.IList)value).Count == 0)
                {
                    Writer.Write(" [ ]");
                    return;
                }

                if (is_element) Writer.WriteLine("[");
                else Writer.Write(" [");

                Writer.Indent++;
                foreach (var array_value in (System.Collections.IList)value)
                    WriteAttribute(null, inner_type, array_value, true);
                Writer.Indent--;
                Writer.TrimEnd(1); // remove trailing comma

                if (inner_type == typeof(Element)) Writer.WriteLine("]");
                else Writer.Write(" ]");
                return;
            }

            if (is_element)
            {
                var elem = value as Element;
                var id = elem == null ? "" : elem.ID.ToString();

                if (in_array)
                {
                    if (ShouldBeReferenced(elem))
                    {
                        Writer.WriteTokenLine("element", id);
                    }
                    else
                    {
                        Writer.WriteLine();
                        WriteElement(elem);
                    }

                    Writer.Write(",");
                }
                else
                {
                    if (ShouldBeReferenced(elem))
                    {
                        Writer.WriteTokenLine(name, "element", id);
                    }
                    else
                    {
                        Writer.WriteLine($"\"{name}\" ");
                        WriteElement(elem);
                    }
                }
            }
            else
            {
                if (type == typeof(bool))
                    value = (bool)value ? 1 : 0;
                else if (type == typeof(float))
                    value = (float)value;
                else if (type == typeof(byte[]))
                    value = BitConverter.ToString((byte[])value).Replace("-", string.Empty);
                else if (type == typeof(TimeSpan))
                    value = ((TimeSpan)value).TotalSeconds;
                else if (type == typeof(Color))
                {
                    var c = (Color)value;
                    value = string.Join(" ", new int[] { c.R, c.G, c.B, c.A });
                }
                else if (value is ulong ulong_value)
                    value = $"0x{ulong_value:X}";
                else if (type == typeof(Vector2))
                {
                    var arr = new float[2];
                    ((Vector2)value).CopyTo(arr);
                    value = string.Join(" ", arr);
                }
                else if (type == typeof(Vector3))
                {
                    var arr = new float[3];
                    ((Vector3)value).CopyTo(arr);
                    value = string.Join(" ", arr);
                }
                else if (type == typeof(Vector4))
                {
                    var arr = new float[4];
                    ((Vector4)value).CopyTo(arr);
                    value = string.Join(" ", arr);
                }
                else if (type == typeof(Quaternion))
                {
                    var q = (Quaternion)value;
                    value = FormattableString.Invariant($"{q.X} {q.Y} {q.Z} {q.W}");
                }
                else if (type == typeof(Matrix4x4))
                {
                    var m = (Matrix4x4)value;
                    value = string.Join(" ", m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44);
                }
                else if (value is QAngle qangle_value)
                {
                    value = string.Join(" ", (int)qangle_value.Pitch, (int)qangle_value.Yaw, (int)qangle_value.Roll);
                }

                if (in_array)
                    Writer.Write(String.Format(" \"{0}\",", value.ToString()));
                else
                    Writer.WriteTokenLine(name, TypeNames[type], value.ToString());
            }

        }

        private bool ShouldBeReferenced(Element elem)
        {
            return SupportsReferenceIds && (elem == null || ReferenceCount.TryGetValue(elem, out var refCount) && refCount > 1);
        }

        void WriteElement(Element element)
        {
            if (TypeNames.ContainsValue(element.ClassName))
                throw new CodecException($"Element {element.ID} uses reserved type name \"{element.ClassName}\"");
            Writer.WriteTokens(element.ClassName);
            Writer.WriteLine("{");
            Writer.Indent++;

            if (SupportsReferenceIds)
                Writer.WriteTokenLine("id", "elementid", element.ID.ToString());

            // Skip empty names right now.
            if (!string.IsNullOrEmpty(element.Name))
            {
                Writer.WriteTokenLine("name", "string", element.Name);
            }

            foreach (var attr in element.GetAllAttributesForSerialization())
            {
                if (attr.Value == null)
                    WriteAttribute(attr.Key, typeof(Element), null, false);
                else
                    WriteAttribute(attr.Key, attr.Value.GetType(), attr.Value, false);
            }

            Writer.Indent--;
            Writer.WriteLine("}");
        }

        public void Encode(Datamodel dm, string encoding, int encoding_version, Stream stream)
        {
            Writer = new KV2Writer(stream);
            Encoding = encoding;
            EncodingVersion = encoding_version;

            SupportsReferenceIds = Encoding != "keyvalues2_noids";

            Writer.Write(String.Format(CodecUtilities.HeaderPattern, encoding, encoding_version, dm.Format, dm.FormatVersion));
            Writer.WriteLine();

            ReferenceCount = new Dictionary<Element, int>();

            if (EncodingVersion >= 4 && dm.PrefixAttributes.Count > 0)
            {
                Writer.WriteTokens("$prefix_element$");
                Writer.WriteLine("{");
                Writer.Indent++;
                Writer.WriteTokenLine("id", "elementid", Guid.NewGuid().ToString());
                foreach (var attr in dm.PrefixAttributes)
                    WriteAttribute(attr.Key, attr.Value.GetType(), attr.Value, false);
                Writer.Indent--;
                Writer.WriteLine("}");
                Writer.WriteLine();
            }

            if (SupportsReferenceIds)
                CountReferences(dm.Root);

            WriteElement(dm.Root);
            Writer.WriteLine();

            if (SupportsReferenceIds)
            {
                foreach (var pair in ReferenceCount.Where(pair => pair.Value > 1))
                {
                    if (pair.Key == dm.Root)
                        continue;
                    Writer.WriteLine();
                    WriteElement(pair.Key);
                    Writer.WriteLine();
                }
            }

            Writer.Flush();
        }
        #endregion

        #region Decode
        readonly StringBuilder TokenBuilder = new();
        int Line = 0;
        string Decode_NextToken()
        {
            TokenBuilder.Clear();
            bool escaped = false;
            bool in_block = false;
            while (true)
            {
                var read = Reader.Read();
                if (read == -1) throw new EndOfStreamException();
                var c = (char)read;
                if (escaped)
                {
                    TokenBuilder.Append(c);
                    escaped = false;
                    continue;
                }
                switch (c)
                {
                    case '"':
                        if (in_block) return TokenBuilder.ToString();
                        in_block = true;
                        break;
                    case '\\':
                        escaped = true; break;
                    case '\r':
                    case '\n':
                        Line++;
                        break;
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                        if (!in_block)
                            return c.ToString();
                        else goto default;
                    default:
                        if (in_block) TokenBuilder.Append(c);
                        break;
                }
            }
        }

        Element Decode_ParseElementId()
        {
            Element elem;
            var id_s = Decode_NextToken();

            if (string.IsNullOrEmpty(id_s))
                elem = null;
            else
            {
                Guid id = new(id_s);
                elem = DM.AllElements[id];
                elem ??= new Element(DM, id);
            }
            return elem;
        }

        Element Decode_ParseElement(string class_name)
        {
            string elem_class = class_name ?? Decode_NextToken();
            string elem_name = null;
            string elem_id = null;
            Element elem = null;

            string next = Decode_NextToken();
            if (next != "{") throw new CodecException(String.Format("Expected Element opener, got '{0}'.", next));
            while (true)
            {
                next = Decode_NextToken();
                if (next == "}") break;

                var attr_name = next;
                var attr_type_s = Decode_NextToken();
                var attr_type = TypeNames.FirstOrDefault(kv => kv.Value == attr_type_s.Split('_')[0]).Key;

                if (elem == null && attr_name == "id" && attr_type_s == "elementid")
                {
                    elem_id = Decode_NextToken();
                    var id = new Guid(elem_id);
                    var local_element = DM.AllElements[id];
                    if (local_element != null)
                    {
                        elem = local_element;
                        elem.Name = elem_name;
                        elem.ClassName = elem_class;
                        elem.Stub = false;
                    }
                    else if (elem_class != "$prefix_element$")
                        elem = new Element(DM, elem_name, new Guid(elem_id), elem_class);

                    continue;
                }

                if (attr_name == "name" && attr_type == typeof(string))
                {
                    elem_name = Decode_NextToken();
                    if (elem != null)
                        elem.Name = elem_name;
                    continue;
                }

                if (elem == null)
                    continue;

                if (attr_type_s == "element")
                {
                    elem.Add(attr_name, Decode_ParseElementId());
                    continue;
                }

                object attr_value = null;

                if (attr_type == null)
                    attr_value = Decode_ParseElement(attr_type_s);
                else if (attr_type_s.EndsWith("_array"))
                {
                    var array = CodecUtilities.MakeList(attr_type, 5); // assume 5 items
                    attr_value = array;

                    next = Decode_NextToken();
                    if (next != "[") throw new CodecException(String.Format("Expected array opener, got '{0}'.", next));
                    while (true)
                    {
                        next = Decode_NextToken();
                        if (next == "]") break;

                        if (next == "element") // Element ID reference
                            array.Add(Decode_ParseElementId());
                        else if (attr_type == typeof(Element)) // inline Element
                            array.Add(Decode_ParseElement(next));
                        else // normal value
                            array.Add(Decode_ParseValue(attr_type, next));
                    }
                }
                else
                    attr_value = Decode_ParseValue(attr_type, Decode_NextToken());

                if (elem != null)
                    elem.Add(attr_name, attr_value);
                else
                    DM.PrefixAttributes[attr_name] = attr_value;
            }
            return elem;
        }

        object Decode_ParseValue(Type type, string value)
        {
            if (type == typeof(string))
                return value;

            value = value.Trim();

            if (type == typeof(Element))
                return Decode_ParseElement(value);
            if (type == typeof(int))
                return int.Parse(value);
            else if (type == typeof(float))
                return float.Parse(value);
            else if (type == typeof(bool))
                return byte.Parse(value) == 1;
            else if (type == typeof(byte[]))
            {
                byte[] result = new byte[value.Length / 2];
                for (int i = 0; i * 2 < value.Length; i++)
                {
                    result[i] = byte.Parse(value.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }
                return result;
            }
            else if (type == typeof(TimeSpan))
                return TimeSpan.FromTicks((long)(double.Parse(value) * TimeSpan.TicksPerSecond));

            var num_list = value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            if (type == typeof(Color))
            {
                var rgba = num_list.Select(i => byte.Parse(i)).ToArray();
                return Color.FromBytes(rgba);
            }

            if (type == typeof(ulong)) return ulong.Parse(value.Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
            if (type == typeof(byte)) return byte.Parse(value);

            var f_list = num_list.Select(i => float.Parse(i)).ToArray();
            if (type == typeof(Vector2)) return new Vector2(f_list[0], f_list[1]);
            else if (type == typeof(Vector3)) return new Vector3(f_list[0], f_list[1], f_list[2]);
            else if (type == typeof(Vector4)) return new Vector4(f_list[0], f_list[1], f_list[2], f_list[3]);
            else if (type == typeof(Quaternion)) return new Quaternion(f_list[0], f_list[1], f_list[2], f_list[3]);
            else if (type == typeof(Matrix4x4)) return new Matrix4x4(
                f_list[0], f_list[1], f_list[2], f_list[3],
                f_list[4], f_list[5], f_list[6], f_list[7],
                f_list[8], f_list[9], f_list[10], f_list[11],
                f_list[12], f_list[13], f_list[14], f_list[15]);
            else if (type == typeof(QAngle)) return new QAngle(f_list[0], f_list[1], f_list[2]);

            else throw new ArgumentException($"Internal error: ParseValue passed unsupported type: {type}.");
        }

        public Datamodel Decode(string encoding, int encoding_version, string format, int format_version, Stream stream, DeferredMode defer_mode)
        {
            DM = new Datamodel(format, format_version);

            if (encoding == "keyvalues2_noids")
                throw new NotImplementedException("KeyValues2_noids decoding not implemented.");

            stream.Seek(0, SeekOrigin.Begin);
            Reader = new StreamReader(stream, Datamodel.TextEncoding);
            Reader.ReadLine(); // skip DMX header
            Line = 1;
            string next;

            while (true)
            {
                try
                { next = Decode_NextToken(); }
                catch (EndOfStreamException)
                { break; }

                try
                { Decode_ParseElement(next); }
                catch (Exception err)
                { throw new CodecException($"KeyValues2 decode failed on line {Line}:\n\n{err.Message}", err); }
            }

            return DM;
        }
        #endregion
    }
}
