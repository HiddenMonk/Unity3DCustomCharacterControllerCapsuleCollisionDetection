using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public class SetTargetFrameRate : MonoBehaviour
	{
		public int targetFrameRate = 400;
		int currentFrameRate = -1;

		void Update()
		{
			if(currentFrameRate != targetFrameRate)
			{
				QualitySettings.vSyncCount = 0;  // VSync must be disabled
				Application.targetFrameRate = targetFrameRate;
				currentFrameRate = targetFrameRate;
			}
		}
	}
}