﻿using Autofac.Extensions.DependencyInjection;
using KeyWatcher.Orleans.Grains;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Runtime.Configuration;
using System;
using System.Threading.Tasks;

namespace KeyWatcher.Azure
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			var webHost = Program.BuildWebHost(args);
			var siloHost = Program.BuildSiloHost();
			await siloHost.StartAsync();
			await webHost.RunAsync();
			await siloHost.StopAsync();
		}

		public static IWebHost BuildWebHost(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.ConfigureServices(services => services.AddAutofac())
				.UseStartup<Startup>()
				.Build();

		private static ISiloHost BuildSiloHost()
		{
			var configuration = ClusterConfiguration.LocalhostPrimarySilo();
			configuration.AddMemoryStorageProvider("Default");
			configuration.AddMemoryStorageProvider("PubSubStore");
			configuration.AddSimpleMessageStreamProvider("NotificationStream");

			try
			{
				return new SiloHostBuilder()
					.UseConfiguration(configuration)
					.UseServiceProviderFactory(services => services.AddAutofac().BuildServiceProvider())
					.AddApplicationPartsFromReferences(typeof(UserGrain).Assembly)
					.Build();
			}
			catch(Exception e)
			{
				Console.Out.WriteLine(e);
				return null;
			}
		}
	}
}
