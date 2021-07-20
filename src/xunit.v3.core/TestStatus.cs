namespace Xunit
{
	/// <summary>
	/// Represents the current status of the execution of the test case.
	/// </summary>
	public enum TestStatus
	{
		/// <summary>
		/// The test is initializing and hasn't started running yet.
		/// </summary>
		Initializing = 1,

		/// <summary>
		/// The test is currently running.
		/// </summary>
		Running,

		/// <summary>
		/// The test has run, and is currently doing clean up (f.e., Dispose).
		/// </summary>
		CleaningUp,

		/// <summary>
		/// The test has finished.
		/// </summary>
		Finished,
	}
}
