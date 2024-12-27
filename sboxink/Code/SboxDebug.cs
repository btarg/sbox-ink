using System.Diagnostics;
using Sandbox;
public class SboxDebug
{
	[System.Diagnostics.Conditional( "DEBUG" )]
	public static void Assert( bool condition, string message = "" )
	{

		if ( !condition )
			if ( message.Length == 0 )
			{
				throw new System.Exception( "Assert failed" );
			}
			else
			{
				throw new System.Exception( message );
			}
				
	}

	public static void WriteLine( string message )
	{
		Log.Info( message );
	}

}
