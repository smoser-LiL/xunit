using System.Collections;
using System.Collections.Generic;
using Xunit.Internal;

namespace Xunit.Runner.Common
{
	/// <summary>
	/// Represents a project which contains zero or more test assemblies, as well as global
	/// (cross-assembly) configuration settings.
	/// </summary>
	public class XunitProject : IEnumerable<XunitProjectAssembly>
	{
		readonly List<XunitProjectAssembly> assemblies;

		/// <summary/>
		public XunitProject()
		{
			assemblies = new List<XunitProjectAssembly>();
		}

		/// <summary/>
		public ICollection<XunitProjectAssembly> Assemblies => assemblies;

		/// <summary>
		/// Gets the configuration values for the test project.
		/// </summary>
		public TestProjectConfiguration Configuration { get; } = new();

		/// <summary/>
		public void Add(XunitProjectAssembly assembly)
		{
			Guard.ArgumentNotNull("assembly", assembly);

			assemblies.Add(assembly);
		}

		/// <summary/>
		public IEnumerator<XunitProjectAssembly> GetEnumerator() =>
			assemblies.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() =>
			GetEnumerator();
	}
}
