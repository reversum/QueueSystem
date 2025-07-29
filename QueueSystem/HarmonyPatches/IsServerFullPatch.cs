using HarmonyLib;

namespace JoinQueuePatch.HarmonyPatches
{

	[HarmonyPatch(typeof(CustomLiteNetLib4MirrorTransport), "IsServerFull")]
	public static class IsServerFullPatch
	{
		public static bool Prefix(ref bool __result)
		{
			__result = false; 
			return false;   
		}
	}
}
