using System;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace ColinsALMCheckinPolicies
{
	internal abstract class BaseSerializationBinding : ISerializationBinder
	{
		public virtual string AsmName
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void BindToName(Type serializedType, out string assemblyName, out string typeName)
		{
			var assembly = serializedType.Assembly;
			if (assembly.Equals(Assembly.GetExecutingAssembly()))
			{
				assemblyName = AsmName;
			}
			else
			{
				assemblyName = assembly.FullName;
			}
			typeName = serializedType.FullName;
		}

		public Type BindToType(string assemblyName, string typeName)
		{
			// VS loads this extension outside the default assembly probing path, so
			// resolve the policy type from this assembly rather than relying on the
			// assembly name encoded in the stored $type value (Assembly.Load by simple
			// name fails for an extension-hosted assembly).
			return Assembly.GetExecutingAssembly().GetType(typeName) ?? Type.GetType(typeName);
		}
	}
}
