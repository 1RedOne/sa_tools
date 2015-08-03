﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SonicRetro.SAModel.Direct3D;
using SonicRetro.SAModel.SAEditorCommon.SETEditing;
using SonicRetro.SAModel.SAEditorCommon.UI;

namespace SonicRetro.SAModel.SAEditorCommon.DataTypes
{
	[Serializable]
	public class SETItem : Item, ICustomTypeDescriptor
	{
		private BoundingSphere bounds;
		public override BoundingSphere Bounds
		{
			get
			{
				return bounds;
			}
		}

		private ObjectDefinition objdef;

		public SETItem(EditorItemSelection selectionManager)
			: base(selectionManager)
		{
			position = new Vertex();
			Rotation = new Rotation();
			Scale = new Vertex();
			AssignObjectDefinition();
			bounds = objdef.GetBounds(this);
		}

		public SETItem(byte[] file, int address, EditorItemSelection selectionManager)
			: base(selectionManager)
		{
			ushort _id = ByteConverter.ToUInt16(file, address);
			ID = _id;
			ClipLevel = (byte)(_id >> 12);
			ushort xrot = BitConverter.ToUInt16(file, address + 2);
			ushort yrot = BitConverter.ToUInt16(file, address + 4);
			ushort zrot = BitConverter.ToUInt16(file, address + 6);
			Rotation = new Rotation(xrot, yrot, zrot);
			position = new Vertex(file, address + 8);
			Scale = new Vertex(file, address + 0x14);
			isLoaded = true;
			bounds = objdef.GetBounds(this);
		}

		private void AssignObjectDefinition()
		{
			objdef = (id < LevelData.ObjDefs.Count) ? LevelData.ObjDefs[id] : LevelData.ObjDefs[0];
		}

		[ParenthesizePropertyName(true)]
		public string Name { get { return objdef.Name; } }
		[ParenthesizePropertyName(true)]
		public string InternalName { get { return objdef.InternalName; } }
		protected bool isLoaded = false;

		private ushort id;
		[Editor(typeof(IDEditor), typeof(UITypeEditor))]
		public ushort ID
		{
			get { return id; }
			set { id = (ushort)(value & 0xFFF); AssignObjectDefinition(); }
		}

		private ushort cliplevel;
		[Browsable(false)]
		public ushort ClipLevel
		{
			get { return cliplevel; }
			set { cliplevel = (byte)(value & 0xF); }
		}

		[DisplayName("Clip Level")]
		public ClipSetting ClipSetting
		{
			get { return (ClipSetting)ClipLevel; }
			set { ClipLevel = (ushort)value; }
		}

		private Vertex position;

		public override Vertex Position
		{
			get { return position; }
			set
			{
				position = value;
				if (objdef != null) bounds = objdef.GetBounds(this);
			}
		}

		public override Rotation Rotation { get; set; }

		public Vertex Scale { get; set; }

		public override void Paste()
		{
			LevelData.SETItems[LevelData.Character].Add(this);
		}

		public override void Delete()
		{
			LevelData.SETItems[LevelData.Character].Remove(this);
		}

		public override HitResult CheckHit(Vector3 Near, Vector3 Far, Viewport Viewport, Matrix Projection, Matrix View)
		{
			return objdef.CheckHit(this, Near, Far, Viewport, Projection, View, new MatrixStack());
		}

		public override List<RenderInfo> Render(Device dev, EditorCamera camera, MatrixStack transform)
		{
			if (!camera.SphereInFrustum(Bounds))
				return EmptyRenderInfo;

			return objdef.Render(this, dev, camera, transform);
		}

		public byte[] GetBytes()
		{
			List<byte> bytes = new List<byte>(0x20);
			bytes.AddRange(BitConverter.GetBytes((ushort)(id | (cliplevel << 12))));
			unchecked
			{
				bytes.AddRange(BitConverter.GetBytes((ushort)Rotation.X));
				bytes.AddRange(BitConverter.GetBytes((ushort)Rotation.Y));
				bytes.AddRange(BitConverter.GetBytes((ushort)Rotation.Z));
			}
			bytes.AddRange(Position.GetBytes());
			bytes.AddRange(Scale.GetBytes());
			return bytes.ToArray();
		}

		AttributeCollection ICustomTypeDescriptor.GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this, true);
		}

		string ICustomTypeDescriptor.GetClassName()
		{
			return TypeDescriptor.GetClassName(this, true);
		}

		string ICustomTypeDescriptor.GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}

		TypeConverter ICustomTypeDescriptor.GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}

		EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}

		PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}

		object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}

		EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}

		EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}

		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
		{
			return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
		}

		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
		{
			PropertyDescriptorCollection result = TypeDescriptor.GetProperties(this, attributes, true);

			AssignObjectDefinition();
			if (objdef.CustomProperties == null || objdef.CustomProperties.Length == 0) return result;
			List<PropertyDescriptor> props = new List<PropertyDescriptor>(result.Count);
			foreach (PropertyDescriptor item in result)
				props.Add(item);

			foreach (PropertySpec property in objdef.CustomProperties)
			{
				List<Attribute> attrs = new List<Attribute>();

				// Additionally, append the custom attributes associated with the
				// PropertySpec, if any.
				if (property.Attributes != null)
					attrs.AddRange(property.Attributes);

				// Create a new property descriptor for the property item, and add
				// it to the list.
				PropertySpecDescriptor pd = new PropertySpecDescriptor(property,
					property.Name, attrs.ToArray());
				props.Add(pd);
			}

			return new PropertyDescriptorCollection(props.ToArray(), true);
		}

		object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}
	}

	public enum ClipSetting : ushort
	{
		All,
		HighOnly,
		MediumAndHigh
	}
}
