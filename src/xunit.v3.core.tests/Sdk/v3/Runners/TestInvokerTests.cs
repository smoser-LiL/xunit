using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

public class TestInvokerTests
{
	public class Messages
	{
		[Fact]
		public static async void Messages_StaticTestMethod()
		{
			var messageBus = new SpyMessageBus();
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("StaticPassing", messageBus);

			await invoker.RunAsync();

			Assert.Empty(messageBus.Messages);
			Assert.True(invoker.BeforeTestMethodInvoked_Called);
			Assert.True(invoker.AfterTestMethodInvoked_Called);
		}

		[Fact]
		public static async void Messages_NonStaticTestMethod_NoDispose()
		{
			var messageBus = new SpyMessageBus();
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("Passing", messageBus, "Display Name");

			await invoker.RunAsync();

			Assert.Collection(
				messageBus.Messages,
				msg => Assert.IsType<_TestClassConstructionStarting>(msg),
				msg => Assert.IsType<_TestClassConstructionFinished>(msg)
			);
		}

		[Fact]
		public static async void Messages_NonStaticTestMethod_WithDispose()
		{
			var messageBus = new SpyMessageBus();
			var invoker = TestableTestInvoker.Create<DisposableClass>("Passing", messageBus, "Display Name");

			await invoker.RunAsync();

			Assert.Collection(
				messageBus.Messages,
				msg => Assert.IsType<_TestClassConstructionStarting>(msg),
				msg => Assert.IsType<_TestClassConstructionFinished>(msg),
				msg => Assert.IsType<_TestClassDisposeStarting>(msg),
				msg => Assert.IsType<_TestClassDisposeFinished>(msg)
			);
		}

		[Fact]
		public static async void Messages_NonStaticTestMethod_WithDisposeAsync()
		{
			var messageBus = new SpyMessageBus();
			var invoker = TestableTestInvoker.Create<AsyncDisposableClass>("Passing", messageBus, "Display Name");

			await invoker.RunAsync();

			Assert.Collection(
				messageBus.Messages,
				msg => Assert.IsType<_TestClassConstructionStarting>(msg),
				msg => Assert.IsType<_TestClassConstructionFinished>(msg),
				msg => Assert.IsType<_TestClassDisposeStarting>(msg),
				msg => Assert.IsType<_TestClassDisposeFinished>(msg)
			);
		}

		[Fact]
		public static async void Messages_NonStaticTestMethod_WithDisposeAndDisposeAsync()
		{
			var messageBus = new SpyMessageBus();
			var invoker = TestableTestInvoker.Create<BothDisposableClass>("Passing", messageBus, "Display Name");

			await invoker.RunAsync();

			Assert.Collection(
				messageBus.Messages,
				msg => Assert.IsType<_TestClassConstructionStarting>(msg),
				msg => Assert.IsType<_TestClassConstructionFinished>(msg),
				msg => Assert.IsType<_TestClassDisposeStarting>(msg),
				msg => Assert.IsType<_TestClassDisposeFinished>(msg)
			);
		}
	}

	public class Execution
	{
		[Fact]
		public static async void Passing()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("Passing");

			var result = await invoker.RunAsync();

			Assert.NotEqual(0m, result);
			Assert.Null(invoker.Aggregator.ToException());
		}

		[Fact]
		public static async void Failing()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("Failing");

			var result = await invoker.RunAsync();

			Assert.NotEqual(0m, result);
			Assert.IsType<TrueException>(invoker.Aggregator.ToException());
		}

		[Fact]
		public static async void TooManyParameterValues()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("Passing", testMethodArguments: new object[] { 42 });

			await invoker.RunAsync();

			var ex = Assert.IsType<InvalidOperationException>(invoker.Aggregator.ToException());
			Assert.Equal("The test method expected 0 parameter values, but 1 parameter value was provided.", ex.Message);
		}

		[Fact]
		public static async void NotEnoughParameterValues()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("FactWithParameter");

			await invoker.RunAsync();

			var ex = Assert.IsType<InvalidOperationException>(invoker.Aggregator.ToException());
			Assert.Equal("The test method expected 1 parameter value, but 0 parameter values were provided.", ex.Message);
		}
	}

	public class Cancellation
	{
		[Fact]
		public static async void CancellationRequested_DoesNotInvokeTestMethod()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("Failing");
			invoker.TokenSource.Cancel();

			var result = await invoker.RunAsync();

			Assert.Equal(0m, result);
			Assert.Null(invoker.Aggregator.ToException());
			Assert.False(invoker.BeforeTestMethodInvoked_Called);
			Assert.False(invoker.AfterTestMethodInvoked_Called);
		}

		[Fact]
		public static async void CancellationRequested_DisposeCalledIfClassConstructed()
		{
			var classConstructed = false;

			bool cancelThunk(_MessageSinkMessage msg)
			{
				if (msg is _TestClassConstructionFinished)
					classConstructed = true;
				return !classConstructed;
			}

			var messageBus = new SpyMessageBus(cancelThunk);
			var invoker = TestableTestInvoker.Create<DisposableClass>("Passing", messageBus, "Display Name");

			await invoker.RunAsync();

			Assert.Collection(
				messageBus.Messages,
				msg => Assert.IsType<_TestClassConstructionStarting>(msg),
				msg => Assert.IsType<_TestClassConstructionFinished>(msg),
				msg => Assert.IsType<_TestClassDisposeStarting>(msg),
				msg => Assert.IsType<_TestClassDisposeFinished>(msg)
			);
		}
	}

	public class TestContextVisibility
	{
		[Fact]
		public async void Before_SeesInitializing()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("Passing", displayName: "Test display name");

			await invoker.RunAsync();

			var context = invoker.BeforeTestMethodInvoked_Context;
			Assert.NotNull(context);
			var test = context.Test;
			Assert.NotNull(test);
			Assert.Equal("Test display name", test.DisplayName);
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
			Assert.Equal(TestStatus.Initializing, testState.Status);
		}

		[Fact]
		public async void Executing_SeesRunning()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("Passing", displayName: "Test display name");

			await invoker.RunAsync();

			var context = invoker.InvokeTestMethodAsync_Context;
			Assert.NotNull(context);
			var test = context.Test;
			Assert.NotNull(test);
			Assert.Equal("Test display name", test.DisplayName);
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

		[Fact]
		public async void After_Passing_SeesCleaningUp()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("Passing", displayName: "Test display name");

			await invoker.RunAsync();

			var context = invoker.AfterTestMethodInvoked_Context;
			Assert.NotNull(context);
			var test = context.Test;
			Assert.NotNull(test);
			Assert.Equal("Test display name", test.DisplayName);
			var testState = context.TestState;
			Assert.NotNull(testState);
			Assert.Null(testState.ExceptionMessages);
			Assert.Null(testState.ExceptionParentIndices);
			Assert.Null(testState.ExceptionStackTraces);
			Assert.Null(testState.ExceptionTypes);
			Assert.Equal(21.12m, testState.ExecutionTime);
			Assert.Null(testState.FailureCause);
			Assert.Equal("This is the output", testState.Output);
			Assert.Equal(TestResult.Passed, testState.Result);
			Assert.Equal(TestStatus.CleaningUp, testState.Status);
		}

		[Fact]
		public async void After_Failing_SeesCleaningUp()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("Failing", displayName: "Test display name");

			await invoker.RunAsync();

			var context = invoker.AfterTestMethodInvoked_Context;
			Assert.NotNull(context);
			var test = context.Test;
			Assert.NotNull(test);
			Assert.Equal("Test display name", test.DisplayName);
			var testState = context.TestState;
			Assert.NotNull(testState);
			Assert.Equal(
				"Assert.True() Failure" + Environment.NewLine +
				"Expected: True" + Environment.NewLine +
				"Actual:   False",
				Assert.Single(testState.ExceptionMessages!)
			);
			Assert.Equal(-1, Assert.Single(testState.ExceptionParentIndices!));
			Assert.Single(testState.ExceptionStackTraces!);
			Assert.Equal(typeof(TrueException).FullName, Assert.Single(testState.ExceptionTypes!));
			Assert.Equal(21.12m, testState.ExecutionTime);
			Assert.Equal(FailureCause.Assertion, testState.FailureCause);
			Assert.Equal("This is the output", testState.Output);
			Assert.Equal(TestResult.Failed, testState.Result);
			Assert.Equal(TestStatus.CleaningUp, testState.Status);
		}

		[Fact]
		public async void After_Exception_SeesCleaningUp()
		{
			var invoker = TestableTestInvoker.Create<NonDisposableClass>("FactWithParameter", displayName: "Test display name");

			await invoker.RunAsync();

			var context = invoker.AfterTestMethodInvoked_Context;
			Assert.NotNull(context);
			var test = context.Test;
			Assert.NotNull(test);
			Assert.Equal("Test display name", test.DisplayName);
			var testState = context.TestState;
			Assert.NotNull(testState);
			Assert.Equal("The test method expected 1 parameter value, but 0 parameter values were provided.", Assert.Single(testState.ExceptionMessages!));
			Assert.Equal(-1, Assert.Single(testState.ExceptionParentIndices!));
			Assert.Single(testState.ExceptionStackTraces!);
			Assert.Equal(typeof(InvalidOperationException).FullName, Assert.Single(testState.ExceptionTypes!));
			Assert.Equal(21.12m, testState.ExecutionTime);
			Assert.Equal(FailureCause.Exception, testState.FailureCause);
			Assert.Equal("This is the output", testState.Output);
			Assert.Equal(TestResult.Failed, testState.Result);
			Assert.Equal(TestStatus.CleaningUp, testState.Status);
		}
	}

	class NonDisposableClass
	{
		[Fact]
		public static void StaticPassing() { }

		[Fact]
		public void Passing() { }

		[Fact]
		public void Failing()
		{
			Assert.True(false);
		}

		[Fact]
		public void FactWithParameter(int x) { }
	}

	class DisposableClass : IDisposable
	{
		public void Dispose() { }

		[Fact]
		public void Passing() { }
	}

	class AsyncDisposableClass : IAsyncDisposable
	{
		public ValueTask DisposeAsync() => default;

		[Fact]
		public void Passing() { }
	}

	class BothDisposableClass : IAsyncDisposable, IDisposable
	{
		public void Dispose() { }

		public ValueTask DisposeAsync() => default;

		[Fact]
		public void Passing() { }
	}

	class TestableTestInvoker : TestInvoker<_ITestCase>
	{
		public readonly new ExceptionAggregator Aggregator;
		public bool AfterTestMethodInvoked_Called;
		public TestContext? AfterTestMethodInvoked_Context;
		public bool BeforeTestMethodInvoked_Called;
		public TestContext? BeforeTestMethodInvoked_Context;
		public TestContext? InvokeTestMethodAsync_Context;
		public readonly new _ITestCase TestCase;
		public readonly CancellationTokenSource TokenSource;

		TestableTestInvoker(
			_ITest test,
			IMessageBus messageBus,
			Type testClass,
			MethodInfo testMethod,
			object?[]? testMethodArguments,
			ExceptionAggregator aggregator,
			CancellationTokenSource cancellationTokenSource) :
				base(test, messageBus, testClass, new object[0], testMethod, testMethodArguments, aggregator, cancellationTokenSource)
		{
			TestCase = test.TestCase;
			Aggregator = aggregator;
			TokenSource = cancellationTokenSource;
		}

		public static TestableTestInvoker Create<TClassUnderTest>(
			string methodName,
			IMessageBus? messageBus = null,
			string displayName = "MockDisplayName",
			object?[]? testMethodArguments = null)
		{
			var testCase = Mocks.TestCase<TClassUnderTest>(methodName);
			var test = Mocks.Test(testCase, displayName, "test-id");

			return new TestableTestInvoker(
				test,
				messageBus ?? new SpyMessageBus(),
				typeof(TClassUnderTest),
				typeof(TClassUnderTest).GetMethod(methodName) ?? throw new ArgumentException($"Could not find method '{methodName}' in '{typeof(TClassUnderTest).FullName}'"),
				testMethodArguments,
				new ExceptionAggregator(),
				new CancellationTokenSource()
			);
		}

		protected override Task AfterTestMethodInvokedAsync()
		{
			AfterTestMethodInvoked_Called = true;
			AfterTestMethodInvoked_Context = TestContext.Current;
			return Task.CompletedTask;
		}

		protected override Task BeforeTestMethodInvokedAsync()
		{
			BeforeTestMethodInvoked_Called = true;
			BeforeTestMethodInvoked_Context = TestContext.Current;
			return Task.CompletedTask;
		}

		protected override Task InvokeTestMethodAsync(object? testClassInstance)
		{
			InvokeTestMethodAsync_Context = TestContext.Current;

			return base.InvokeTestMethodAsync(testClassInstance);
		}

		protected override TestState CreatePostRunTestState() =>
			TestState.FromException(
				21.12m,
				"This is the output",
				Aggregator.ToException(),
				TestStatus.CleaningUp
			);
	}
}
