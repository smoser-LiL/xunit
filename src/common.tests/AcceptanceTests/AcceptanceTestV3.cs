using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

public class AcceptanceTestV3
{
	public Task<List<_MessageSinkMessage>> RunAsync(params Type[] types)
	{
		var tcs = new TaskCompletionSource<List<_MessageSinkMessage>>();

		ThreadPool.QueueUserWorkItem(async _ =>
		{
			try
			{
				var diagnosticMessageSink = _NullMessageSink.Instance;
				await using var testFramework = new XunitTestFramework(diagnosticMessageSink, configFileName: null);

				using var discoverySink = SpyMessageSink<_DiscoveryComplete>.Create();
				var assemblyInfo = Reflector.Wrap(Assembly.GetEntryAssembly()!);
				var discoverer = testFramework.GetDiscoverer(assemblyInfo);
				var discoveryOptions = _TestFrameworkOptions.ForDiscovery();
				foreach (var type in types)
				{
					discoverer.Find(type.FullName!, discoverySink, discoveryOptions);
					discoverySink.Finished.WaitOne();
					discoverySink.Finished.Reset();
				}

				var testCases = discoverySink.Messages.OfType<_TestCaseDiscovered>().Select(msg => msg.Serialization).ToArray();

				using var runSink = SpyMessageSink<_TestAssemblyFinished>.Create();
				var executor = testFramework.GetExecutor(assemblyInfo);
				var executionOptions = _TestFrameworkOptions.ForExecution();
				executor.RunTests(testCases, runSink, executionOptions);
				runSink.Finished.WaitOne();

				tcs.TrySetResult(runSink.Messages.ToList());
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		});

		return tcs.Task;
	}

	public async Task<List<TMessageType>> RunAsync<TMessageType>(params Type[] types)
		where TMessageType : _MessageSinkMessage
	{
		var results = await RunAsync(types);
		return results.OfType<TMessageType>().ToList();
	}
}
