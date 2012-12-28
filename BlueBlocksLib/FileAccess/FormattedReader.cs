using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using BlueBlocksLib.Endianness;
using BlueBlocksLib.TypeUtils;

namespace BlueBlocksLib.FileAccess
{
    public class FormattedReader : DisposableStream {
		TypeTools t = new TypeTools();

        public BinaryReader BaseStream
        {
            get { return (BinaryReader)m_stream; }
        }

        public FormattedReader(Stream stream)
        {
            m_stream = new BinaryReader(stream);
        }

        public readonly string Filename;

        public FormattedReader(string filename)
        {
            byte[] bytes = File.ReadAllBytes(filename);

            m_stream = new BinaryReader(new MemoryStream(bytes));

            Filename = filename;
        }

        public T Read<T>() where T : new()
        {
            T o = new T();
            object obj = (object)o;
            Read(ref obj, true);
            return (T)obj;
        }

        public bool EndOfStream
        {
            get
            {
                return ((BinaryReader)m_stream).BaseStream.Position == ((BinaryReader)m_stream).BaseStream.Length;
            }
        }

        void Read(ref object o, bool isLittleEndian)
        {
            BinaryReader m_stream = (BinaryReader)this.m_stream;
            Type t = o.GetType();
            StructLayoutAttribute structlayout = t.StructLayoutAttribute;

            EndianBitConverter b;
            if (!isLittleEndian)
            {
                b = new BigEndianBitConverter();
            }
            else
            {
                b = new LittleEndianBitConverter();
            }

			if (t.IsEnum) {
				t = Enum.GetUnderlyingType(t);
			}

            if (t == typeof(int) ) {
                o = b.ToInt32(m_stream.ReadBytes(4), 0);

            }
            else if (t == typeof(uint))
            {
                o = b.ToUInt32(m_stream.ReadBytes(4), 0);

            }
            else if (t == typeof(short))
            {
                o = b.ToInt16(m_stream.ReadBytes(2), 0);

            }
            else if (t == typeof(ushort))
            {
                o = b.ToUInt16(m_stream.ReadBytes(2), 0);

            }
            else if (t == typeof(byte))
            {
                o = m_stream.ReadBytes(1)[0];

            }
            else if (t == typeof(long))
            {
                o = b.ToInt64(m_stream.ReadBytes(8), 0);

            }
            else if (t == typeof(ulong))
            {
                o = b.ToUInt64(m_stream.ReadBytes(8), 0);

            }
            else if (t == typeof(float))
            {
                o = b.ToSingle(m_stream.ReadBytes(4), 0);

            }
            else if (t == typeof(double))
            {
                o = b.ToDouble(m_stream.ReadBytes(8), 0);
            }
			else if (t == typeof(string))
			{
				List<byte> bytes = new List<byte>();
				byte lastbyte = 0xff;
				while (lastbyte != 0) {
					lastbyte = m_stream.ReadByte();
					bytes.Add(lastbyte);
				}
				o = Encoding.UTF8.GetString(bytes.ToArray()).Trim('\0');
			} 
			else if (t.IsArray)
			{
				if (t.GetElementType() == typeof(byte)) {
					byte[] arr = (byte[])o;
					m_stream.Read(arr, 0, arr.Length);
				} else {
					ReadIntoArray(o, isLittleEndian);
				}

			} else if (structlayout != null) {
				ReadIntoStruct(o, m_stream);

			} else {
				throw new Exception("I don't know how to write this object:" + o.GetType().Name + " Please include the [StructLayout] attribute");
			}

        }

        private void ReadIntoArray(object o, bool isLittleEndian)
        {
            Array arr = (Array)o;


			for (int i = 0; i < arr.Length; i++) {
				object element = Activator.CreateInstance(o.GetType().GetElementType());
                Read(ref element, isLittleEndian);
                arr.SetValue(element, i);
            }
        }

        private void ReadIntoStruct(object o, BinaryReader m_stream)
        {

            Dictionary<FieldInfo, OffsetAttribute> fieldsWithSpecifiedOffsets = new Dictionary<FieldInfo, OffsetAttribute>();
            Type objecttype = o.GetType();
			FieldInfo[] fis = t.GetFieldsOfTypeInOrderOfDeclaration(objecttype);

            // Sort the fields into the order they were specified
            SortFieldInfoArrayByOrderSpecified(objecttype, fis);

            List<object> towrite = new List<object>();

            // Read the laid out fields
            foreach (FieldInfo fi in fis) {
				OffsetAttribute[] offset = (OffsetAttribute[])fi.GetCustomAttributes(typeof(OffsetAttribute), false);

                InternalUseAttribute[] internalUse = (InternalUseAttribute[])fi.GetCustomAttributes(typeof(InternalUseAttribute), false);

                if (internalUse.Length > 0)
                {
                    continue;
                }

                if (offset.Length > 0)
                {
                    // Defer reading this field until we are done with all the laid-out ones
                    fieldsWithSpecifiedOffsets[fi] = offset[0];
                    continue;
                }

				ReadIntoField(o, fi);
            }

            // Save our position
            long endofstruct = m_stream.BaseStream.Position;

            // Read the field which have specific offsets specified
            foreach (KeyValuePair<FieldInfo, OffsetAttribute> kvp in fieldsWithSpecifiedOffsets)
            {

                OffsetAttribute offattr = kvp.Value;
				offattr.SpecificOffset = GetOffsetAttrContent(o, o.GetType(), offattr);

                // Offset is specified by a field
                m_stream.BaseStream.Seek(offattr.SpecificOffset, SeekOrigin.Begin);
                ReadIntoField(o, kvp.Key);
            }

            // Restore our position
            m_stream.BaseStream.Position = endofstruct;
        }

        private static void SortFieldInfoArrayByOrderSpecified(Type objecttype, FieldInfo[] fis)
        {
            Dictionary<string, IntPtr> fieldorder = new Dictionary<string, IntPtr>();


            object cache = objecttype.GetType().GetProperty("Cache", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic).GetValue(objecttype, null);

            cache.GetType().GetField("m_fieldInfoCache", BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cache, null);

            //// We make use of Marshal.OffsetOf to get the fields in order
            //// they were specified. This is why we want the structs used to
            //// be marked StructLayout.Sequential, since there is no other
            //// way to do this in .NET's reflection services
            //foreach (FieldInfo fi in fis) {

            // fieldorder[fi.Name] = Marshal.OffsetOf(objecttype, fi.Name);
            //}

            //// By sorting the array, we can process each item in order.
            //// This makes it easy to declaratively define file formats
            //Array.Sort(fis, (x, y) => fieldorder[x.Name].ToInt32() - fieldorder[y.Name].ToInt32());

            FieldInfo[] fiSorted = objecttype.GetFields();
            for (int i = 0; i < fis.Length; i++)
            {
                fis[i] = fiSorted[i];
            }
        }

		private void ReadIntoField(object o, FieldInfo fi) {

            object[] lil = fi.GetCustomAttributes(typeof(LittleEndianAttribute), false);
            object[] big = fi.GetCustomAttributes(typeof(BigEndianAttribute), false);
            object[] array = fi.GetCustomAttributes(typeof(ArraySizeAttribute), false);

			ReadTypeAttribute[] readtypes = (ReadTypeAttribute[])fi.GetCustomAttributes(typeof(ReadTypeAttribute), false);

			Type t = fi.FieldType;
			if (readtypes.Length != 0) {
				foreach (var readtype in readtypes) {
					uint val = GetTypeValue(o, o.GetType(), readtype);
					if (readtype.value == val) {
						t = readtype.t;
						break;
					}
				}
			}

            bool littleendian = big.Length == 0;

            Type objecttype = o.GetType();
            object field = fi.GetValue(o);

			if (t.IsArray)
            {
                ArraySizeAttribute arr = (ArraySizeAttribute)array[0];

                int size = GetArraySize(o, objecttype, arr);

                field = Array.CreateInstance(t.GetElementType(), size);

                Read(ref field, littleendian);

            }
            else
            {
                if (t == typeof(string))
                {
                    field = "";
                }
                else if (field == null)
                {
                    field = Activator.CreateInstance(t);
                }

                Read(ref field, littleendian);
            }
            fi.SetValue(o, field);
        }

		static long InferMember(long value, string spec, object o) {
			if (string.IsNullOrEmpty(spec)) {
				return value;
			}
			Type objecttype = o.GetType();
			if (spec.Contains("()")) {
				// its a method
				var meth = objecttype.GetMethod(spec.Replace("()", ""));
				value = long.Parse(meth.Invoke(o, null).ToString());
			} else {
				// its a field or property
				var fld = objecttype.GetField(spec);
				var prop = objecttype.GetProperty(spec);

				if (fld != null) {
					value = long.Parse(fld.GetValue(o).ToString());
				} else if (prop != null) {
					value = long.Parse(prop.GetValue(o, null).ToString());
				} else {
					throw new Exception("Field or property called " + spec + " not found!");
				}
			}
			return value;
		}

        internal static int GetArraySize(object o, Type objecttype, ArraySizeAttribute arr)
        {
			return (int)InferMember(arr.size, arr.getSize, o);
        }

		internal static long GetOffsetAttrContent(object o, Type objecttype, OffsetAttribute arr) {
			return (long)InferMember(arr.SpecificOffset, arr.getOffset, o);
		}

		internal static uint GetTypeValue(object o, Type objecttype, ReadTypeAttribute arr) {
			return (uint)InferMember(arr.value, arr.field, o);
		}
    }


}
