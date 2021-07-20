using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Xunit.Internal;
using Xunit.v3;

namespace Xunit
{
	/// <summary>
	/// Represents information about the current state of the test engine. It may be available at
	/// various points during the execution pipeline, so consumers must always take care to ensure
	/// that they check for <c>null</c> values from the various properties.
	/// </summary>
	public class TestContext
	{
		static readonly AsyncLocal<TestContext> local = new();

		TestContext()
		{ }

		/// <summary/>
		public static TestContext? Current => local.Value;

		/// <summary>
		/// Gets the current test, if the engine is currently in the process of running a test;
		/// will return <c>null</c> outside of the context of a test.
		/// </summary>
		// TODO: This comes from Xunit.v3; is that an appropriate dependency?
		public _ITest? Test { get; set; }

		///// <summary>
		///// Gets the output helper, which can be used to add output to the test. Will only be
		///// available when <see cref="Test"/> is not <c>null</c>.
		///// </summary>
		//[NotNullIfNotNull(nameof(Test))]
		//public _ITestOutputHelper? TestOutputHelper { get; set; }

		/// <summary>
		/// Gets the current state of the test. Will only be available when <see cref="Test"/>
		/// is not <c>null</c>.
		/// </summary>
		[NotNullIfNotNull(nameof(Test))]
		public TestState? TestState { get; set; }

		internal static TestContext SetForTest(
			_ITest test,
			TestState testState) =>
				local.Value = new TestContext
				{
					Test = Guard.ArgumentNotNull(nameof(test), test),
					TestState = Guard.ArgumentNotNull(nameof(testState), testState),
				};
	}
}
