using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public static class ExtLayerMask
	{
		public static int ignoreRaycastMask = ~LayerMask.GetMask("Ignore Raycast");
		public static LayerMask physicsSoloCastLayer = LayerMask.NameToLayer("PhysicsSoloCast");
		public static int physicsSoloCastMask = LayerMask.GetMask("PhysicsSoloCast");

		//Kinda a hackish way to warn the user in the inspector that a layer isnt set =)
		static bool layersAreSet = LareLayersAreSet();
		static bool LareLayersAreSet()
		{
			if(physicsSoloCastMask == 0)
			{
				Debug.LogError("Layer PhysicsSoloCast is not defined. ExtLayerMask.physicsSoloCastLayer and ExtLayerMask.physicsSoloCastMask will return the wrong value");
				return false;
			}

			if(layersAreSet){} //Im just putting this here so that the editor stops complaining that the variable was never used...

			return true;
		}
	}
}