﻿using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;
using Newtonsoft.Json;
using System.IO;

namespace KeyWatcher.Orleans.Host.StorageProviders
{
	public sealed class FileStorageProvider
		: IStorageProvider
	{
		public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) =>
			throw new NotImplementedException();

		public Task Close() =>
			throw new NotImplementedException();

		public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
		{
			this.Name = name;
			var rootDirectory = config.Properties["RootDirectory"];

			if (string.IsNullOrWhiteSpace(rootDirectory))
			{
				throw new ArgumentException("RootDirectory property not set");
			}

			var directory = new DirectoryInfo(rootDirectory);

			if (!directory.Exists)
			{
				directory.Create();
			}

			this.RootDirectory = directory.FullName;

			return Task.CompletedTask;
		}

		public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
		{
			var fileInfo = this.GetFileInfo(grainReference, grainState);

			if (fileInfo.Exists)
			{
				using (var stream = fileInfo.OpenText())
				{
					var storedData = await stream.ReadToEndAsync();
					grainState.State = JsonConvert.DeserializeObject(
						storedData, grainState.State.GetType());
				}
			}
		}

		public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
		{
			var fileInfo = this.GetFileInfo(grainReference, grainState);

			using (var stream = new StreamWriter(
				fileInfo.Open(FileMode.Create, FileAccess.Write)))
			{
				await stream.WriteAsync(
					JsonConvert.SerializeObject(grainState.State));
			}
		}

		private FileInfo GetFileInfo(GrainReference grainReference, IGrainState grainState)
		{
			var collectionName = grainState.GetType().Name;
			var key = grainReference.ToKeyString();
			var path = Path.Combine(this.RootDirectory, $"{key}.{collectionName}");

			return new FileInfo(path);
		}

		public Logger Log { get; set; }
		public string Name { get; set; }
		public string RootDirectory { get; private set; }
	}
}
