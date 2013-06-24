using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBlocksLib.FileAccess {

	// Attribute for saying this is stored as little endian
	[AttributeUsage(AttributeTargets.Field)]
	public class LittleEndianAttribute : Attribute { }

	// Attribute for saying this is stored as big endian
	[AttributeUsage(AttributeTargets.Field)]
	public class BigEndianAttribute : Attribute { }

	// Attribute for marking how big an array should be
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class ArraySizeAttribute : Attribute {

		internal ArrayInfo info;

		// When writing: if the array is null then assume all zero
		internal int size;

		public ArraySizeAttribute(int size) {
			this.size = size;
			this.info = ArrayInfo.FixedSize;
		}

		// This is for property, field or method extraction of size
		internal string getSize = null;
		public ArraySizeAttribute(string sizeFunction) {
			this.getSize = sizeFunction;
			this.info = ArrayInfo.AnotherMemberGivesSize;
		}

		// This is for saying the array length is determined somehow
		public ArraySizeAttribute(ArrayProperty prop) {
			this.info = (ArrayInfo)Enum.Parse(typeof(ArrayInfo), prop.ToString());
		}

		// For internal reference
		public enum ArrayInfo {
			FixedSize,
			AnotherMemberGivesSize,
			DefaultTerminated,
			TerminatedAtEndOfFile,
		}
	}

	// Publicly set. These MUST have corresponders in the ArrayInfo class.
	public enum ArrayProperty {
		DefaultTerminated,
		TerminatedAtEndOfFile
	}


	// Attribute for saying where the start offset of the item should be
	[AttributeUsage(AttributeTargets.Field)]
	public class OffsetAttribute : Attribute {
		internal long SpecificOffset;

		public OffsetAttribute(long offset) {
			SpecificOffset = offset;
		}

		// This is for property, field or method extraction of the offset
		public OffsetAttribute(string offsetFunction) {
			getOffset = offsetFunction;
		}

		internal string getOffset = null;
	}

	// Adds bytes to behind this struct to pad it to a certain boundary
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
	public class AlignmentAttribute : Attribute {
		internal int m_boundarySize;

		public AlignmentAttribute(int boundarySize) {
			m_boundarySize = boundarySize;
		}
	}

	// Populates this field with the offset at the point this field is reached
	// will not be written to the file when Write and will not advance ptr when Read
	[AttributeUsage(AttributeTargets.Field)]
	public class PopulateWithCurrentOffsetAttribute : Attribute { }


	// Populates the attributed interface with a the type selected given
	// the versionGetter that provides the version number and the version
	// that says what type is to be used for the version
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class VersionSelectAttribute : Attribute {

		public string versionGetter;
		public int version;
		public Type t;

		public VersionSelectAttribute(string versionGetter, int version, Type t) {
			this.versionGetter = versionGetter;
			this.version = version;
			this.t = t;
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class ConstructWithInformationAttribute : Attribute {

		public readonly string parameterList;

		public ConstructWithInformationAttribute(string parameterList) {
			this.parameterList = parameterList;
		}
	}
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class ReadTypeAttribute : Attribute {
		public readonly Type t;
		public readonly string field;
		public readonly uint value;
		public ReadTypeAttribute(Type type, string field, uint value) {
			this.t = type;
			this.field = field;
			this.value = value;
		}
	}

    [AttributeUsage(AttributeTargets.Field)]
    public class InternalUseAttribute : Attribute
    {
    }
}
