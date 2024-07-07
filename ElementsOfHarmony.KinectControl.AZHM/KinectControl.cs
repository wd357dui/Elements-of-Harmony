using static ElementsOfHarmony.KinectControl.KinectControl;

namespace ElementsOfHarmony.KinectControl.AZHM
{
	public static class KinectControl
	{
		public struct ButtonStatus
		{
			public bool Left, Up, Right, Down,
				A, B, X, Y, View, Menu, LB, RB;
			public float LeftStick;
		}
		public static ButtonStatus? PreviousPlayer1Status, PreviousPlayer2Status,
			CurrentPlayer1Status, CurrentPlayer2Status;
		public static void EnsureFrameUpdate() // copied & modified from AMBA
		{
			PreviousPlayer1Status = CurrentPlayer1Status;
			PreviousPlayer2Status = CurrentPlayer2Status;

			static ButtonStatus? GetStatus(PlayerStatus? Player)
			{
				if (Player == null) return null;
				lock (Player)
				{
					return new ButtonStatus()
					{
						Left = Player.Left == true,
						Up = Player.Up == true,
						Right = Player.Right == true,
						Down = Player.Down == true,
						A = Player.South == true,
						B = Player.East == true,
						X = Player.West == true,
						Y = Player.North == true,
						View = Player.SouthWest == true,
						Menu = Player.SouthEast == true,
						LB = Player.NorthWest == true,
						RB = Player.NorthEast == true,
					};
				}
			}

			CurrentPlayer1Status = GetStatus(Player1);
			CurrentPlayer2Status = GetStatus(Player2);
		}
	}
}
