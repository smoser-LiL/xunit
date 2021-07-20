using System;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Xunit
{
	/// <summary>
	/// Represents information about the current state of a test. This includes the current status,
	/// as well as metadata about the test during/after the run (like output and exception information).
	/// </summary>
	public class TestState
	{
		TestState()
		{ }

		/// <summary>
		/// Gets the message(s) of the exception(s). This value is only available
		/// when <see cref="Result"/> is <see cref="TestResult.Failed"/>.
		/// </summary>
		public string[]? ExceptionMessages { get; private set; }

		/// <summary>
		/// Gets the parent exception index(es) for the exception(s); a -1 indicates
		/// that the exception in question has no parent. This value is only available
		/// when <see cref="Result"/> is <see cref="TestResult.Failed"/>.
		/// </summary>
		public int[]? ExceptionParentIndices { get; private set; }

		/// <summary>
		/// Gets the stack trace(s) of the exception(s). This value is only available
		/// when <see cref="Result"/> is <see cref="TestResult.Failed"/>.
		/// </summary>
		public string?[]? ExceptionStackTraces { get; private set; }

		/// <summary>
		/// Gets the fully-qualified type name(s) of the exception(s). This value is
		/// only available when <see cref="Result"/> is <see cref="TestResult.Failed"/>.
		/// </summary>
		public string?[]? ExceptionTypes { get; private set; }

		/// <summary>
		/// Gets the amount of time the test ran, in seconds. The value may be <c>0</c> if no
		/// test code was run (for example, a statically skipped test). The final value is only
		/// available when <see cref="Status"/> is <see cref="TestStatus.Finished"/>; a partial
		/// value will be available during <see cref="TestStatus.CleaningUp"/>.
		/// </summary>
		public decimal? ExecutionTime { get; private set; }

		/// <summary>
		/// Gets a value which indicates what the cause of the test failure was. This value is only
		/// available when <see cref="Result"/> is <see cref="TestResult.Failed"/>.
		/// </summary>
		public FailureCause? FailureCause { get; private set; }

		/// <summary>
		/// Returns the output from the test that was sent to the <see cref="_ITestOutputHelper"/>.
		/// The final value is only available when <see cref="Status"/> is <see cref="TestStatus.Finished"/>;
		/// a partial value will be available during <see cref="TestStatus.CleaningUp"/>.
		/// </summary>
		public string? Output { get; private set; }

		/// <summary>
		/// Returns the result from the test run. This value is only available when <see cref="Status"/>
		/// is <see cref="TestStatus.CleaningUp"/> or <see cref="TestStatus.Finished"/>.
		/// </summary>
		public TestResult? Result { get; private set; }

		/// <summary>
		/// Returns the current state of the test.
		/// </summary>
		public TestStatus Status { get; private set; }

		/// <summary>
		/// Gets an immutable instance that indicates the test is initializing.
		/// </summary>
		static public TestState Initializing { get; } = new TestState { Status = TestStatus.Initializing };

		/// <summary>
		/// Gets an immutable instance that indicates the test is running.
		/// </summary>
		static public TestState Running { get; } = new TestState { Status = TestStatus.Running };

		/// <summary>
		/// Gets an immutable instance to indicates a test has a result.
		/// </summary>
		/// <param name="executionTime"></param>
		/// <param name="output"></param>
		/// <param name="exception"></param>
		/// <param name="status"></param>
		public static TestState FromException(
			decimal executionTime,
			string output,
			Exception? exception,
			TestStatus status)
		{
			var result = new TestState
			{
				ExecutionTime = executionTime,
				Output = Guard.ArgumentNotNull(nameof(output), output),
				Status = status,
			};

			if (exception == null)
				result.Result = TestResult.Passed;
			else
			{
				var errorMetadata = ExceptionUtility.ExtractMetadata(exception);

				result.ExceptionMessages = errorMetadata.Messages;
				result.ExceptionParentIndices = errorMetadata.ExceptionParentIndices;
				result.ExceptionStackTraces = errorMetadata.StackTraces;
				result.ExceptionTypes = errorMetadata.ExceptionTypes;
				result.FailureCause = errorMetadata.Cause;
				result.Result = TestResult.Failed;
			}

			return result;
		}

		/// <summary/>
		public static TestState FromTestResult(_TestResultMessage testResult)
		{
			var result = new TestState
			{
				ExecutionTime = testResult.ExecutionTime,
				Output = testResult.Output,
				Status = TestStatus.Finished,
			};

			if (testResult is _TestPassed)
				result.Result = TestResult.Passed;
			else if (testResult is _TestSkipped)
				result.Result = TestResult.Skipped;
			else if (testResult is _TestFailed testFailed)
			{
				result.ExceptionMessages = testFailed.Messages;
				result.ExceptionParentIndices = testFailed.ExceptionParentIndices;
				result.ExceptionStackTraces = testFailed.StackTraces;
				result.ExceptionTypes = testFailed.ExceptionTypes;
				result.FailureCause = testFailed.Cause;
				result.Result = TestResult.Failed;
			}
			else
				throw new ArgumentException($"Unknown type: '{testResult.GetType().FullName}'", nameof(testResult));

			return result;
		}
	}
}
