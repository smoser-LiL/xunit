using Xunit;

public class TestContextTests
{
	[Fact]
	public static void AmbientTestContextIsAvailableInTest()
	{
		var context = TestContext.Current;

		Assert.NotNull(context);
		var test = context.Test;
		Assert.NotNull(test);
		Assert.Equal($"{nameof(TestContextTests)}.{nameof(AmbientTestContextIsAvailableInTest)}", test.DisplayName);
		var testState = context.TestState;
		Assert.NotNull(testState);
		Assert.Null(testState.ExceptionMessages);
		Assert.Null(testState.ExceptionParentIndices);
		Assert.Null(testState.ExceptionStackTraces);
		Assert.Null(testState.ExceptionTypes);
		Assert.Null(testState.ExecutionTime);
		Assert.Null(testState.FailureCause);
		Assert.Null(testState.Output);
		Assert.Null(testState.Result);
		Assert.Equal(TestStatus.Running, testState.Status);
	}
}
