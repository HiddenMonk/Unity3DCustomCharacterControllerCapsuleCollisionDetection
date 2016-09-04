using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	[Serializable]
	public class SubStepUpdater
	{
		public float maxDeltaTime = .2f; //This is the max deltaTime of the overall deltaTime, similar to the unity physics max allowed timesteps
		public float maxSubStepDeltaTime = .01f;  //If our framerate is low, we run more times for more accuracy. lower = more movement accuracy, too low might cause problems (such as too low values causing float point precision issues).. This is the target update rate, so if we are 1 FPS then with .01 we update 100 times
		public int maxSubSteps = 10; //Higher = more movement accuracy. 10 is good for framerates 10+ with maxSubStepDeltaTime being .01 (1 / (10 * .01)) = 10. Any value over 1 / maxSubStepDeltaTime will be ignored.
		public Action<float> subStepMethod;

		public SubStepUpdater(){} //Required so we can do = new VariableFixedUpdater() next to declared variable so unity can properly set default values in inspector.
		public SubStepUpdater(Action<float> subStepMethod, int maxSubSteps, float maxSubStepDeltaTime, float maxDeltaTime = .2f)
		{
			this.subStepMethod = subStepMethod;
			this.maxSubSteps = maxSubSteps;
			this.maxSubStepDeltaTime = maxSubStepDeltaTime;
			this.maxDeltaTime = maxDeltaTime;
		}

		public void Update()
		{
			float deltaTime = Mathf.Min(Time.deltaTime, maxDeltaTime);
			int maxSteps = Mathf.CeilToInt(deltaTime / maxSubStepDeltaTime);
			int steps = Mathf.Clamp(maxSteps, 1, maxSubSteps);
			
			deltaTime = deltaTime / (float)steps;
			if(deltaTime > 0f)
			{
				for(int i = 0; i < steps; i++)
				{
					subStepMethod(deltaTime);
				}
			}
		}
	}
}