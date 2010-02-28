﻿/*  MapleLib - A general-purpose MapleStory library
 * Copyright (C) 2009, 2010 Snow and haha01haha01
   
 * This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.*/

using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib
{
	/// <summary>
	/// An interface for wz img properties
	/// </summary>
	public abstract class IWzImageProperty : IWzNonDirectoryProperty
	{
		public abstract WzPropertyType PropertyType { get; }

		public abstract WzImage ParentImage { get; internal set; }

		public override WzObjectType ObjectType { get { return WzObjectType.Property; } }

		public abstract void WriteValue(WzBinaryWriter writer);

		public virtual void ExportXml(StreamWriter writer, int level)
		{
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag(this.PropertyType.ToString(), this.Name, true));
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag(this.PropertyType.ToString()));
		}

		internal static void WritePropertyList(WzBinaryWriter writer, IWzImageProperty[] properties)
		{
			writer.Write((ushort)0);
			writer.WriteCompressedInt(properties.Length);
			for (int i = 0; i < properties.Length; i++)
			{
				writer.WriteStringValue(properties[i].Name, 0x00, 0x01);
				properties[i].WriteValue(writer);
			}
		}

		internal static void DumpPropertyList(StreamWriter writer, int level, IWzImageProperty[] properties)
		{
			foreach (IWzImageProperty prop in properties)
			{
				prop.ExportXml(writer, level + 1);
			}
		}

		internal static IWzImageProperty[] ParsePropertyList(uint offset, WzBinaryReader reader, IWzObject parent, WzImage parentImg)
		{
			List<IWzImageProperty> properties = new List<IWzImageProperty>();
			int entryCount = reader.ReadCompressedInt();
			for (int i = 0; i < entryCount; i++)
			{
				string name = reader.ReadStringBlock(offset);
				switch (reader.ReadByte())
				{
					case 0:
						properties.Add(new WzNullProperty(name) { Parent = parent, ParentImage = parentImg });
						break;
					case 0x0B:
					case 2:
						properties.Add(new WzUnsignedShortProperty(name, reader.ReadUInt16()) { Parent = parent, ParentImage = parentImg });
						break;
					case 3:
						properties.Add(new WzCompressedIntProperty(name, reader.ReadCompressedInt()) { Parent = parent, ParentImage = parentImg });
						break;
					case 4:
						byte type = reader.ReadByte();
						if (type == 0x80)
							properties.Add(new WzByteFloatProperty(name, reader.ReadSingle()) { Parent = parent, ParentImage = parentImg });
						else if (type == 0)
							properties.Add(new WzByteFloatProperty(name, 0f) { Parent = parent, ParentImage = parentImg });
						break;
					case 5:
						properties.Add(new WzDoubleProperty(name, reader.ReadDouble()) { Parent = parent, ParentImage = parentImg });
						break;
					case 8:
						properties.Add(new WzStringProperty(name, reader.ReadStringBlock(offset)) { Parent = parent });
						break;
					case 9:
						int eob = (int)(reader.ReadUInt32() + reader.BaseStream.Position);
						WzExtendedProperty exProp = new WzExtendedProperty(offset, eob, name);
						exProp.Parent = parent;
						exProp.ParentImage = parentImg;
						exProp.ParseExtendedProperty(reader);
						properties.Add(exProp);
						if (reader.BaseStream.Position != eob) reader.BaseStream.Position = eob;
						break;
				}
			}
			return properties.ToArray();
		}

		#region Cast Values

		public float ToFloat()
		{
			return ToFloat(0);
		}
		public WzPngProperty ToPngProperty()
		{
			return ToPngProperty(null);
		}
		public int ToInt()
		{
			return ToInt(0);
		}
		public double ToDouble()
		{
			return ToDouble(0);
		}
		public Bitmap ToBitmap()
		{
			return ToBitmap(null);
		}
		public byte[] ToSoundBytes()
		{
			return ToSoundBytes(null);
		}
		public string ToStringValue()
		{
			return ToStringValue(null);
		}
		public ushort ToUnsignedShort()
		{
			return ToUnsignedShort(0);
		}
		public IWzImageProperty ToUOLLink()
		{
			return ToUOLLink(null);
		}
		public Point ToVector()
		{
			return ToVector(Point.Empty);
		}

		public float ToFloat(float def)
		{
			if (this is WzByteFloatProperty) return (float)WzValue;
			else return def;
		}
		public WzPngProperty ToPngProperty(WzPngProperty def)
		{
			if (this is WzCanvasProperty) return (WzPngProperty)WzValue;
			else if (this is WzUOLProperty) return ToUOLLink().ToPngProperty(def);
			else return def;
		}
		public int ToInt(int def)
		{
			if (this is WzCompressedIntProperty) return (int)WzValue;
			else if (this is WzStringProperty) return Int32.Parse((string)WzValue);
			else return def;
		}
		public double ToDouble(double def)
		{
			if (this is WzDoubleProperty) return (double)WzValue;
			else if (this is WzStringProperty) return Double.Parse((string)WzValue);
			else return def;
		}
		public Bitmap ToBitmap(Bitmap def)
		{
			if (this is WzPngProperty) return (Bitmap)WzValue;
			else if (this is WzCanvasProperty) return (Bitmap)((WzCanvasProperty)this).PngProperty.WzValue;
			else if (this is WzUOLProperty) return ToUOLLink().ToBitmap(def);
			else return def;
		}
		public byte[] ToSoundBytes(byte[] def)
		{
			if (this is WzSoundProperty) return (byte[])WzValue;
			else return def;
		}
		public string ToStringValue(string def)
		{
			if (this is WzStringProperty) return (string)WzValue;
			else return def;
		}
		public ushort ToUnsignedShort(ushort def)
		{
			if (this is WzUnsignedShortProperty) return (ushort)WzValue;
			else if (this is WzStringProperty) return UInt16.Parse((string)WzValue);
			else return def;
		}
		public IWzImageProperty ToUOLLink(IWzImageProperty def)
		{
			if (this is WzUOLProperty) return (IWzImageProperty)WzValue;
			else return def;
		}
		public Point ToVector(Point def)
		{
			if (this is WzVectorProperty) return (Point)WzValue;
			else return def;
		}

		#endregion
	}
}