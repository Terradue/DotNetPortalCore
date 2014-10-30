using System;
using Terradue.Portal;

namespace Terradue.Portal {

	public class Agent {
		
		private static ActionLoop loop;
	
		/**************************************************************************************************/
		public static void Main(string[] args) {
			loop = new ActionLoop(true);
			Console.TreatControlCAsInput = false;
			Console.CancelKeyPress += new ConsoleCancelEventHandler(OnCancel);
			loop.Process();
		}
		
		/**************************************************************************************************/
		protected static void OnCancel(object sender, ConsoleCancelEventArgs args) {
			loop.End("Execution terminated by external request");
		}
	}
}

