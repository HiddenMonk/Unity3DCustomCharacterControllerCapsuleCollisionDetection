using UnityEditor;

namespace CapsuleCharacterCollisionDetection
{
	[InitializeOnLoad]
	public class StopPlayingOnRecompile
	{
		static StopPlayingOnRecompile()
		{
			//Since InitializeOnLoad is called when unity starts AND every time you hit play, we will unsubscribe and resubscribe to avoid duplicates.
			//Might not be needed to do since EditorApplication.update might be cleared on every InitializeOnLoad call?
			EditorApplication.update -= StopPlayingIfRecompiling;
			EditorApplication.update += StopPlayingIfRecompiling;
		}

		static void StopPlayingIfRecompiling()
		{
			if(EditorApplication.isCompiling && EditorApplication.isPlaying)
			{
				EditorApplication.isPlaying = false;
			}
		}
	}
}