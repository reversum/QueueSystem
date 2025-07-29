using CentralAuth;

namespace JoinQueuePatch
{
    public class QueueItem
    {
		public PlayerAuthenticationManager PlayerAuthenticationManager { get; set; }
		public AuthenticationResponse AuthenticationResponse { get; set; }
	}
}
