using System;
using System.Collections.Generic;
using System.Text;
using BlueBlocksLib.TypeUtils;
using BlueBlocksLib.BaseClasses;
using System.Reflection;

namespace BlueBlocksLib.PluginLoader {

	public class PluginDll<TInterface> : IImplements<TInterface>, IDisposable {

		AppDomain domain;
		RemoteLoader<TInterface> loader;
		public PluginDll(string libpath, string name) {

			SetupStuff(name);

			Interface = (TInterface)loader.Load(libpath, typeof(TInterface));
			LibraryPath = libpath;
			Name = name;
		}

		public PluginDll(Handler handler, string name) {

			SetupStuff(name);

			Interface = (TInterface)loader.Load(handler);
			LibraryPath = null;
			Name = name;
		}

		private void SetupStuff(string name) {
			// This setup is to force the release of DLL file handles
			// when the domain is unloaded
			AppDomainSetup setup = new AppDomainSetup();
			setup.LoaderOptimization = LoaderOptimization.MultiDomainHost;
			domain = AppDomain.CreateDomain(name, null, setup);

			loader = (RemoteLoader<TInterface>)domain.CreateInstanceFromAndUnwrap(
			Assembly.GetExecutingAssembly().CodeBase,
			typeof(RemoteLoader<TInterface>).FullName);
		}

		public TInterface Interface { get; private set; }

		public string LibraryPath { get; private set; }

		public string Name { get; private set; }

		#region IDisposable Members

		public void Dispose() {
			AppDomain.Unload(domain);
		}

		#endregion
	}

	class RemoteLoader<TInterface> : MarshalByRefObject {

		public object Load(string libpath, Type interfaceType) {
			AssemblyName name = AssemblyName.GetAssemblyName(libpath);
			Assembly pluginAassembly = AppDomain.CurrentDomain.Load(name);
			//AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler((o, e) => {
			// return Assembly.LoadFile(libpath);
			//});


			var requiredType = Array.Find(pluginAassembly.GetTypes()
			, type => Array.Exists(type.GetInterfaces(),
				intf => intf == interfaceType)
			);

			object result = Activator.CreateInstance(requiredType);
			return result;
		}

		InterfaceImplementor<TInterface> implementor = null;
		public object Load(Handler handler) {

			implementor = new InterfaceImplementor<TInterface>(handler);

			return implementor.Interface;
		}

		public override object InitializeLifetimeService() {
			// This prevents the object from being destroyed after a few minutes
			return null;
		}

		~RemoteLoader() {
			if (implementor != null) {
				implementor.Dispose();
			}
			Console.WriteLine("Remote Loader Finalized");
		}

	}
}
