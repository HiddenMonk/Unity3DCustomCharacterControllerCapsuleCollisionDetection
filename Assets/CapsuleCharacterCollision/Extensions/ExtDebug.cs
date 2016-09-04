using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public static class ExtDebug
	{
		public static string DetailedString(this Vector3 vector)
		{
			return vector.x + " " + vector.y + " " + vector.z;
		}

		public static void DrawMarker(Vector3 position, float size, Color color, float duration, bool depthTest = true)
		{
			Vector3 line1PosA = position + Vector3.up * size * 0.5f;
			Vector3 line1PosB = position - Vector3.up * size * 0.5f;

			Vector3 line2PosA = position + Vector3.right * size * 0.5f;
			Vector3 line2PosB = position - Vector3.right * size * 0.5f;

			Vector3 line3PosA = position + Vector3.forward * size * 0.5f;
			Vector3 line3PosB = position - Vector3.forward * size * 0.5f;

			Debug.DrawLine(line1PosA, line1PosB, color, duration, depthTest);
			Debug.DrawLine(line2PosA, line2PosB, color, duration, depthTest);
			Debug.DrawLine(line3PosA, line3PosB, color, duration, depthTest);
		}

		// Courtesy of robertbu
		public static void DrawPlane(Vector3 position, Vector3 normal, float size, Color color, float duration = .001f, bool depthTest = true) 
		{
			Vector3 v3;
			
			if(normal.normalized != Vector3.forward && normal.normalized != -Vector3.forward)
			{
				v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
			}else{
				v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude;
			}
 
			Vector3 corner0 = position + v3 * size;
			Vector3 corner2 = position - v3 * size;
 
			Quaternion q = Quaternion.AngleAxis(90.0f, normal);
			v3 = q * v3;
			Vector3 corner1 = position + v3 * size;
			Vector3 corner3 = position - v3 * size;

			Debug.DrawLine(corner0, corner2, color, duration, depthTest);
			Debug.DrawLine(corner1, corner3, color, duration, depthTest);
			Debug.DrawLine(corner0, corner1, color, duration, depthTest);
			Debug.DrawLine(corner1, corner2, color, duration, depthTest);
			Debug.DrawLine(corner2, corner3, color, duration, depthTest);
			Debug.DrawLine(corner3, corner0, color, duration, depthTest);
			Debug.DrawRay(position, normal * size, color, duration, depthTest);
		}

		public static void DrawPlane(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, float duration = .001f)
		{
			Debug.DrawLine(a, b, color, duration);
			Debug.DrawLine(b, c, color, duration);
			Debug.DrawLine(c, a, color, duration);
			Debug.DrawLine(c, d, color, duration);
			Debug.DrawLine(d, a, color, duration);
		}
		
		public static void DrawRect3D(Rect3D rect, Color color, float duration = .001f)
		{
			DrawPlane(rect.bottomLeft, rect.topLeft, rect.topRight, rect.bottomRight, color, duration);
		}

		public static void DrawVector(Vector3 position, Vector3 direction, float raySize, float markerSize, Color color, float duration, bool depthTest = true)
		{
			Debug.DrawRay(position, direction * raySize, color, 0, false);
			ExtDebug.DrawMarker(position + direction * raySize, markerSize, color, 0, false);
		}

		public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
		{
			Debug.DrawLine(a, b, color);
			Debug.DrawLine(b, c, color);
			Debug.DrawLine(c, a, color);
		}

		public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color, Transform t)
		{
			a = t.TransformPoint(a);
			b = t.TransformPoint(b);
			c = t.TransformPoint(c);

			Debug.DrawLine(a, b, color);
			Debug.DrawLine(b, c, color);
			Debug.DrawLine(c, a, color);
		}

		public static void DrawMesh(Mesh mesh, Color color, Transform t)
		{
			for (int i = 0; i < mesh.triangles.Length; i += 3)
			{
				DrawTriangle(mesh.vertices[mesh.triangles[i]], mesh.vertices[mesh.triangles[i + 1]], mesh.vertices[mesh.triangles[i + 2]], color, t);
			}
		}

		public static Color RandomColor()
		{
			return new Color(Random.value, Random.value, Random.value);
		}

		//Below taken from free asset on unity asset store - https://www.assetstore.unity3d.com/en/#!/content/11396

		public static void DrawCapsule(Vector3 origin, Vector3 direction, float height, float radius, Color color, float duration = .001f, bool depthTest = true)
		{
			CapsuleShape points = new CapsuleShape(origin, direction, height, radius);
			DrawCapsule(points.top, points.bottom, radius, color, duration, depthTest);
		}

		public static void DrawCapsule(Vector3 start, Vector3 end, float radius, Color color, float duration = .001f, bool depthTest = true)
		{
			Vector3 up = (end-start).normalized * radius;
			if(up == Vector3.zero) up = Vector3.up; //This can happen when the capsule is actually a sphere, so the start and end are right on eachother
			Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
			Vector3 right = Vector3.Cross(up, forward).normalized * radius;
		
			//Radial circles
			DrawCircle(start, up, color, radius, duration, depthTest);	
			DrawCircle(end, -up, color, radius, duration, depthTest);
		
			//Side lines
			Debug.DrawLine(start+right, end+right, color, duration, depthTest);
			Debug.DrawLine(start-right, end-right, color, duration, depthTest);
		
			Debug.DrawLine(start+forward, end+forward, color, duration, depthTest);
			Debug.DrawLine(start-forward, end-forward, color, duration, depthTest);
		
			for(int i = 1; i < 26; i++){
			
				//Start endcap
				Debug.DrawLine(Vector3.Slerp(right, -up, i/25.0f)+start, Vector3.Slerp(right, -up, (i-1)/25.0f)+start, color, duration, depthTest);
				Debug.DrawLine(Vector3.Slerp(-right, -up, i/25.0f)+start, Vector3.Slerp(-right, -up, (i-1)/25.0f)+start, color, duration, depthTest);
				Debug.DrawLine(Vector3.Slerp(forward, -up, i/25.0f)+start, Vector3.Slerp(forward, -up, (i-1)/25.0f)+start, color, duration, depthTest);
				Debug.DrawLine(Vector3.Slerp(-forward, -up, i/25.0f)+start, Vector3.Slerp(-forward, -up, (i-1)/25.0f)+start, color, duration, depthTest);
			
				//End endcap
				Debug.DrawLine(Vector3.Slerp(right, up, i/25.0f)+end, Vector3.Slerp(right, up, (i-1)/25.0f)+end, color, duration, depthTest);
				Debug.DrawLine(Vector3.Slerp(-right, up, i/25.0f)+end, Vector3.Slerp(-right, up, (i-1)/25.0f)+end, color, duration, depthTest);
				Debug.DrawLine(Vector3.Slerp(forward, up, i/25.0f)+end, Vector3.Slerp(forward, up, (i-1)/25.0f)+end, color, duration, depthTest);
				Debug.DrawLine(Vector3.Slerp(-forward, up, i/25.0f)+end, Vector3.Slerp(-forward, up, (i-1)/25.0f)+end, color, duration, depthTest);
			}
		}

		public static void DrawWireSphere(Vector3 position, float radius, Color color, float duration = .001f, bool depthTest = true)
		{
			float angle = 10.0f;
		
			Vector3 x = new Vector3(position.x, position.y + radius * Mathf.Sin(0), position.z + radius * Mathf.Cos(0));
			Vector3 y = new Vector3(position.x + radius * Mathf.Cos(0), position.y, position.z + radius * Mathf.Sin(0));
			Vector3 z = new Vector3(position.x + radius * Mathf.Cos(0), position.y + radius * Mathf.Sin(0), position.z);
		
			Vector3 new_x;
			Vector3 new_y;
			Vector3 new_z;
		
			for(int i = 1; i < 37; i++){
			
				new_x = new Vector3(position.x, position.y + radius * Mathf.Sin(angle*i*Mathf.Deg2Rad), position.z + radius * Mathf.Cos(angle*i*Mathf.Deg2Rad));
				new_y = new Vector3(position.x + radius * Mathf.Cos(angle*i*Mathf.Deg2Rad), position.y, position.z + radius * Mathf.Sin(angle*i*Mathf.Deg2Rad));
				new_z = new Vector3(position.x + radius * Mathf.Cos(angle*i*Mathf.Deg2Rad), position.y + radius * Mathf.Sin(angle*i*Mathf.Deg2Rad), position.z);
			
				Debug.DrawLine(x, new_x, color, duration, depthTest);
				Debug.DrawLine(y, new_y, color, duration, depthTest);
				Debug.DrawLine(z, new_z, color, duration, depthTest);
		
				x = new_x;
				y = new_y;
				z = new_z;
			}
		}

		public static void DrawCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f, float duration = .001f, bool depthTest = true)
		{
			Vector3 _up = up.normalized * radius;
			Vector3 _forward = Vector3.Slerp(_up, -_up, 0.5f);
			Vector3 _right = Vector3.Cross(_up, _forward).normalized*radius;
		
			Matrix4x4 matrix = new Matrix4x4();
		
			matrix[0] = _right.x;
			matrix[1] = _right.y;
			matrix[2] = _right.z;
		
			matrix[4] = _up.x;
			matrix[5] = _up.y;
			matrix[6] = _up.z;
		
			matrix[8] = _forward.x;
			matrix[9] = _forward.y;
			matrix[10] = _forward.z;
		
			Vector3 _lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
			Vector3 _nextPoint = Vector3.zero;
		
			color = (color == default(Color)) ? Color.white : color;
		
			for(var i = 0; i < 91; i++){
				_nextPoint.x = Mathf.Cos((i*4)*Mathf.Deg2Rad);
				_nextPoint.z = Mathf.Sin((i*4)*Mathf.Deg2Rad);
				_nextPoint.y = 0;
			
				_nextPoint = position + matrix.MultiplyPoint3x4(_nextPoint);
			
				Debug.DrawLine(_lastPoint, _nextPoint, color, duration, depthTest);
				_lastPoint = _nextPoint;
			}
		}

		public static void DrawTransformedBox(Vector3 localOrigin, Vector3 localExtents, Matrix4x4 space, Color color, float duration = .001f, bool depthTest = true)
		{	
			Vector3 lbb = space.MultiplyPoint3x4(localOrigin+((-localExtents)*0.5f));
			Vector3 rbb = space.MultiplyPoint3x4(localOrigin+(new Vector3(localExtents.x, -localExtents.y, -localExtents.z)*0.5f));
		
			Vector3 lbf = space.MultiplyPoint3x4(localOrigin+(new Vector3(localExtents.x, -localExtents.y, localExtents.z)*0.5f));
			Vector3 rbf = space.MultiplyPoint3x4(localOrigin+(new Vector3(-localExtents.x, -localExtents.y, localExtents.z)*0.5f));
		
			Vector3 lub = space.MultiplyPoint3x4(localOrigin+(new Vector3(-localExtents.x, localExtents.y, -localExtents.z)*0.5f));
			Vector3 rub = space.MultiplyPoint3x4(localOrigin+(new Vector3(localExtents.x, localExtents.y, -localExtents.z)*0.5f));
		
			Vector3 luf = space.MultiplyPoint3x4(localOrigin+((localExtents)*0.5f));
			Vector3 ruf = space.MultiplyPoint3x4(localOrigin+(new Vector3(-localExtents.x, localExtents.y, localExtents.z)*0.5f));
		
			Debug.DrawLine(lbb, rbb, color, duration, depthTest);
			Debug.DrawLine(rbb, lbf, color, duration, depthTest);
			Debug.DrawLine(lbf, rbf, color, duration, depthTest);
			Debug.DrawLine(rbf, lbb, color, duration, depthTest);
		
			Debug.DrawLine(lub, rub, color, duration, depthTest);
			Debug.DrawLine(rub, luf, color, duration, depthTest);
			Debug.DrawLine(luf, ruf, color, duration, depthTest);
			Debug.DrawLine(ruf, lub, color, duration, depthTest);
		
			Debug.DrawLine(lbb, lub, color, duration, depthTest);
			Debug.DrawLine(rbb, rub, color, duration, depthTest);
			Debug.DrawLine(lbf, luf, color, duration, depthTest);
			Debug.DrawLine(rbf, ruf, color, duration, depthTest);
		}
	}
}