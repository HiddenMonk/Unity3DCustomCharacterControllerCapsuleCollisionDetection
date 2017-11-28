using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	public static class ExtPhysics
	{
		#region Queries

		#region Query ignore colliders

		public enum Inclusion { IncludeOnly = 0, Ignore = 1, Include = 2 } //Include = include this object regardless of layer, IncludeOnly = we are testing only against this collider so ignore the layermask, Ignore = we are ignoring it
		static IList<Component> tempComponentLayer = new List<Component>();

		static void SetTempComponentLayer(Component component)
		{
			tempComponentLayer.Clear();
			tempComponentLayer.Add(component);
		}

		static List<LayerMask> originalLayers = new List<LayerMask>();
		static void StoreOriginalLayerAndReassign(IList<Component> components)
		{
			if(components == null) return;
			originalLayers.Clear();

			for(int i = 0; i < components.Count; i++)
			{
				originalLayers.Add(components[i].gameObject.layer);
			}

			//It is important to assign all the layers to the originalLayers before setting their layer to the temp layer so that duplicate components
			//in the list dont store the originalLayer then set their new layer and then the duplicate will store that temp layer as the original...
			for(int i = 0; i < components.Count; i++)
			{
				components[i].gameObject.layer = ExtLayerMask.physicsSoloCastLayer;
			}
		}
		static void RestoreToOriginalLayer(IList<Component> components)
		{
			if(components == null) return;

			for(int i = 0; i < components.Count; i++)
			{
				components[i].gameObject.layer = originalLayers[i];
			}
		}
		
		static int GetMaskAndStoreOriginalLayerAndReassign(int layerMask, Inclusion layerContext, IList<Component> componentsLayers)
		{
			StoreOriginalLayerAndReassign(componentsLayers);

			if(layerContext == Inclusion.Ignore)
			{
				return ~ExtLayerMask.physicsSoloCastMask & layerMask;
			}
			else if(layerContext == Inclusion.IncludeOnly)
			{
				return ExtLayerMask.physicsSoloCastMask;
			}else{
				return ExtLayerMask.physicsSoloCastMask | layerMask;
			}
		}
		#endregion

		#region SphereCast
		public static RaycastHit SphereCast(Vector3 origin, float radius, Vector3 direction, Component componentLayer, float maxDistance = Mathf.Infinity, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			SetTempComponentLayer(componentLayer);
			return SphereCast(origin, radius, direction, tempComponentLayer, maxDistance, layerMask, queryTriggerInteraction, layerContext);
		}
		public static RaycastHit SphereCast(Vector3 origin, float radius, Vector3 direction, IList<Component> componentsLayers, float maxDistance = Mathf.Infinity, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			int mask = GetMaskAndStoreOriginalLayerAndReassign(layerMask, layerContext, componentsLayers);
			RaycastHit hitInfo = SphereCast(origin, radius, direction, maxDistance, mask, queryTriggerInteraction);
			RestoreToOriginalLayer(componentsLayers);
			return hitInfo;
		}
		public static RaycastHit SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			RaycastHit hit;
			Physics.SphereCast(origin, radius, direction, out hit, maxDistance, layerMask & ExtLayerMask.ignoreRaycastMask, queryTriggerInteraction);
			return hit;
		}
		#endregion

		#region CheckSphere
		public static bool CheckSphere(Vector3 origin, float radius, Component componentLayer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			SetTempComponentLayer(componentLayer);
			return CheckSphere(origin, radius, tempComponentLayer, layerMask, queryTriggerInteraction, layerContext);
		}
		public static bool CheckSphere(Vector3 origin, float radius, IList<Component> componentsLayers, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			int mask = GetMaskAndStoreOriginalLayerAndReassign(layerMask, layerContext, componentsLayers);
			bool hasHit = CheckSphere(origin, radius, mask, queryTriggerInteraction);
			RestoreToOriginalLayer(componentsLayers);
			return hasHit;
		}
		public static bool CheckSphere(Vector3 origin, float radius, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			return Physics.CheckSphere(origin, radius, layerMask & ExtLayerMask.ignoreRaycastMask, queryTriggerInteraction);
		}
		#endregion

		#region CheckCapsule
		public static bool CheckCapsule(Vector3 origin, Vector3 direction, float height, float radius, Component componentLayer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			CapsuleShape points = new CapsuleShape(origin, direction, height, radius);
			return CheckCapsule(points.bottom, points.top, radius, componentLayer, layerMask, queryTriggerInteraction);
		}
		public static bool CheckCapsule(Vector3 segment0, Vector3 segment1, float radius, Component componentLayer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			SetTempComponentLayer(componentLayer);
			return CheckCapsule(segment0, segment1, radius, tempComponentLayer, layerMask, queryTriggerInteraction, layerContext);
		}

		public static bool CheckCapsule(Vector3 origin, Vector3 direction, float height, float radius, IList<Component> componentsLayers, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			CapsuleShape points = new CapsuleShape(origin, direction, height, radius);
			return CheckCapsule(points.bottom, points.top, radius, componentsLayers, layerMask, queryTriggerInteraction);
		}
		public static bool CheckCapsule(Vector3 segment0, Vector3 segment1, float radius, IList<Component> componentsLayers, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			int mask = GetMaskAndStoreOriginalLayerAndReassign(layerMask, layerContext, componentsLayers);
			bool hasHit = CheckCapsule(segment0, segment1, radius, mask, queryTriggerInteraction);
			RestoreToOriginalLayer(componentsLayers);
			return hasHit;
		}

		public static bool CheckCapsule(Vector3 origin, Vector3 direction, float height, float radius, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			CapsuleShape points = new CapsuleShape(origin, direction, height, radius);
			return CheckCapsule(points.bottom, points.top, radius, layerMask, queryTriggerInteraction);
		}
		public static bool CheckCapsule(Vector3 segment0, Vector3 segment1, float radius, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			return Physics.CheckCapsule(segment0, segment1, radius, layerMask & ExtLayerMask.ignoreRaycastMask, queryTriggerInteraction);
		}
		#endregion

		#region OverlapSphere
		static Collider[] nonAllocateOverlapSphereResults = new Collider[100]; //Hopefully 100 is enough

		public static IList<Collider> OverlapSphere(Vector3 origin, float radius, Component componentLayer, IList<Collider> resultBuffer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			SetTempComponentLayer(componentLayer);
			return OverlapSphere(origin, radius, tempComponentLayer, resultBuffer, layerMask, queryTriggerInteraction, layerContext);
		}
		public static IList<Collider> OverlapSphere(Vector3 origin, float radius, IList<Component> componentsLayers, IList<Collider> resultBuffer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			int mask = GetMaskAndStoreOriginalLayerAndReassign(layerMask, layerContext, componentsLayers);
			OverlapSphere(origin, radius, resultBuffer, mask, queryTriggerInteraction);
			RestoreToOriginalLayer(componentsLayers);
			return resultBuffer;
		}
		public static IList<Collider> OverlapSphere(Vector3 origin, float radius, IList<Collider> resultBuffer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			resultBuffer.Clear();
			int found = Physics.OverlapSphereNonAlloc(origin, radius, nonAllocateOverlapSphereResults, layerMask & ExtLayerMask.ignoreRaycastMask, queryTriggerInteraction);
			resultBuffer.AddRange(nonAllocateOverlapSphereResults, 0, found);
			return resultBuffer;
		}
		#endregion

		#region OverlapCapsule
		static Collider[] nonAllocateOverlapCapsuleResults = new Collider[100]; //Hopefully 100 is enough

		public static IList<Collider> OverlapCapsule(Vector3 origin, Vector3 direction, float height, float radius, Component componentLayer, IList<Collider> resultBuffer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			CapsuleShape points = new CapsuleShape(origin, direction, height, radius);
			return OverlapCapsule(points.bottom, points.top, radius, componentLayer, resultBuffer, layerMask, queryTriggerInteraction);
		}
		public static IList<Collider> OverlapCapsule(Vector3 segment0, Vector3 segment1, float radius, Component componentLayer, IList<Collider> resultBuffer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			SetTempComponentLayer(componentLayer);
			return OverlapCapsule(segment0, segment1, radius, tempComponentLayer, resultBuffer, layerMask, queryTriggerInteraction, layerContext);
		}

		public static IList<Collider> OverlapCapsule(Vector3 origin, Vector3 direction, float height, float radius, IList<Component> componentsLayers, IList<Collider> resultBuffer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			CapsuleShape points = new CapsuleShape(origin, direction, height, radius);
			return OverlapCapsule(points.bottom, points.top, radius, componentsLayers, resultBuffer, layerMask, queryTriggerInteraction);
		}
		public static IList<Collider> OverlapCapsule(Vector3 segment0, Vector3 segment1, float radius, IList<Component> componentsLayers, IList<Collider> resultBuffer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Inclusion layerContext = Inclusion.Ignore)
		{
			int mask = GetMaskAndStoreOriginalLayerAndReassign(layerMask, layerContext, componentsLayers);
			OverlapCapsule(segment0, segment1, radius, resultBuffer, mask, queryTriggerInteraction);
			RestoreToOriginalLayer(componentsLayers);
			return resultBuffer;
		}

		public static IList<Collider> OverlapCapsule(Vector3 origin, Vector3 direction, float height, float radius, IList<Collider> resultBuffer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			CapsuleShape points = new CapsuleShape(origin, direction, height, radius);
			return OverlapCapsule(points.bottom, points.top, radius, resultBuffer, layerMask, queryTriggerInteraction);
		}
		public static IList<Collider> OverlapCapsule(Vector3 segment0, Vector3 segment1, float radius, IList<Collider> resultBuffer, int layerMask = Physics.AllLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
		{
			resultBuffer.Clear();
			int found = Physics.OverlapCapsuleNonAlloc(segment0, segment1, radius, nonAllocateOverlapCapsuleResults, layerMask & ExtLayerMask.ignoreRaycastMask, queryTriggerInteraction);
			resultBuffer.AddRange(nonAllocateOverlapCapsuleResults, 0, found);
			return resultBuffer;
		}
		#endregion

		#endregion
	}
}
