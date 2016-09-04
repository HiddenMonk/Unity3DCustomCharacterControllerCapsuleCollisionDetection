using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public struct Rect3D
	{
		public Vector3 localBottomLeft {get; private set;}
		public Vector3 bottomLeft {get {return localBottomLeft + center;}}
		public Vector3 localBottomRight {get; private set;}
		public Vector3 bottomRight {get {return localBottomRight + center;}}
		public Vector3 localTopLeft {get; private set;}
		public Vector3 topLeft {get {return localTopLeft + center;}}
		public Vector3 localTopRight {get; private set;}
		public Vector3 topRight {get {return localTopRight + center;}}

		public Vector3 center {get; private set;}

		public float width {get; private set;}
		public float height {get; private set;}

		public Rect3D(Vector3 center, Vector3 right, Vector3 up, float width, float height)
		{
			Vector3 halfUp = up * (height * .5f);
			Vector3 halfSide = right * (width * .5f);

			this.localBottomLeft = -halfUp - halfSide;
			this.localBottomRight = -halfUp + halfSide;
			this.localTopLeft = halfUp - halfSide;
			this.localTopRight = halfUp + halfSide;

			this.center = center;

			this.width = width;
			this.height = height;
		}

		public Vector3 this[int index]
		{
			get
			{
				switch (index)
				{
					case 0:
						return this.localBottomLeft;
					case 1:
						return this.localBottomRight;
					case 2:
						return this.localTopLeft;
					case 3:
						return this.localTopRight;
					case 4:
						return this.bottomLeft;
					case 5:
						return this.bottomRight;
					case 6:
						return this.topLeft;
					case 7:
						return this.topRight;
					default:
						throw new IndexOutOfRangeException("Invalid Rect3D index");
				}
			}
		}

		public Vector3 Extents()
		{
			return new Vector3(width, height, 0f);
		}
	}
}