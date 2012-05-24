using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using BlueBlocksLib.SetUtils;
using System.Runtime.InteropServices;
using System.Collections;

namespace BlueBlocksLib.Reports {

	[AttributeUsage(AttributeTargets.Field)]
	public class HeaderNameAttribute : Attribute {
		public string Name { get; private set; }
		public HeaderNameAttribute(string name) {
			Name = name;
		}
	}

    [AttributeUsage(AttributeTargets.Field)]
    public class ColumnSizeAttribute : Attribute
    {
        public int Size { get; private set; }
        public ColumnSizeAttribute(int size)
        {
            Size = size;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class MultiColumn : Attribute
    {
        public int Size { get; private set; }
        public MultiColumn(int size)
        {
            Size = size;
        }
    }

	public class HTMLTable<TData> {

		TData[] m_data;

		public HTMLTable(TData[] data) {
			m_data = data;
		}

		public string ClassName = null;

        public string Render(Func<string, string> translationDelegate)
        {
            string[] content = Array.ConvertAll(m_data, x => RenderData(x));

			string[] rows = ArrayUtils.ConvertAll(content, x => "<tr>" + x + "</tr>");

			return "<table " + (ClassName == null ? "" : ("class=\"" + ClassName + "\"")) + ">" +
				"<thead><tr>" +
				RenderHeader(translationDelegate) +
				"</tr></thead>" +
				string.Join("", rows) +
				"</table>";
        }

		public string Render() {
            return Render(x => x);
		}

		FieldInfo[] m_fieldsInOrder = null;

		string[] ConvertFields(Func<string, FieldInfo> converter) {

			if (m_fieldsInOrder == null) {
				m_fieldsInOrder = typeof(TData).GetFields();
				m_fieldsInOrder = ArrayUtils.FindAll(m_fieldsInOrder, x =>
					x.GetCustomAttributes(typeof(HeaderNameAttribute), false).Length != 0);

				Array.Sort(m_fieldsInOrder, (x, y) =>
					Marshal.OffsetOf(typeof(TData), x.Name).ToInt64().CompareTo(
					Marshal.OffsetOf(typeof(TData), y.Name).ToInt64()));
			}

			return ArrayUtils.ConvertAll(m_fieldsInOrder, x => converter(x));
		}

		string RenderHeader(Func<string,string> translationDelegate) {
			string[] headerNames = ConvertFields(x => {
				HeaderNameAttribute attr = (HeaderNameAttribute)
					x.GetCustomAttributes(typeof(HeaderNameAttribute), false)[0];

                MultiColumn[] multicolumn = (MultiColumn[])
                    x.GetCustomAttributes(typeof(MultiColumn), false);

                if (multicolumn.Length > 0)
                {
                    string header = "";
                    for (int i = 0; i < multicolumn[0].Size; i++)
                    {
                        header += MakeColumnHeader(x, attr, translationDelegate);
                    }
                    return header;
                }

                return MakeColumnHeader(x, attr, translationDelegate);
			});

            return string.Join("\r\n", headerNames);
		}

        private static string MakeColumnHeader(FieldInfo x, HeaderNameAttribute attr, Func<string,string> translationDelegate)
        {

            ColumnSizeAttribute[] colsize = (ColumnSizeAttribute[])
                x.GetCustomAttributes(typeof(ColumnSizeAttribute), false);

            if (colsize.Length != 0)
            {
                return "<th width=\"" + colsize[0].Size + "px\">" + translationDelegate(attr.Name) + "</th>";
            }

            return "<th>" + translationDelegate(attr.Name) + "</th>";
        }


		string RenderData(TData row) {
			return string.Join("", ConvertFields(x => {
                if (x.FieldType.IsArray)
                {
                    return string.Join("", ArrayUtils.ConvertAll((object[])x.GetValue(row),
                        item => "<td>" + item.ToString() + "</td>"));
                }
                return "<td>" + x.GetValue(row) + "</td>";
            }
                ));
		}
	}
}
