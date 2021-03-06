﻿using KeyWatcher.Dependencies;
using KeyWatcher.Messages;
using KeyWatcher.Orleans.Contracts;
using Orleans;
using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeyWatcher.Orleans.Grains
{
	[StorageProvider(ProviderName = ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME)]
	public class UserGrain
		: Grain<UserGrainState>, IUserGrain
	{
		private static readonly string[] BadWords = { "cotton", "headed", "ninny", "muggins" };
		private readonly ILogger logger;
		private readonly Lazy<INotification> notification;

		public UserGrain(ILogger logger, Lazy<INotification> notification)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.notification = notification ?? throw new ArgumentNullException(nameof(notification));
		}

		public override async Task OnActivateAsync()
		{
			this.Id = this.GetPrimaryKeyString();
			await this.logger.LogAsync($"{nameof(OnActivateAsync)} - user ID is {this.Id}");
			await base.OnActivateAsync();
		}

		public override async Task OnDeactivateAsync()
		{
			await this.logger.LogAsync($"{nameof(OnDeactivateAsync)} - user ID is {this.Id}");
			await base.OnDeactivateAsync();
		}

		public string Id { get; private set; }

		public async Task ProcessAsync(UserKeysMessage message)
		{
			var keys = new string(message.Keys.ToArray()).ToLower();

			await this.logger.LogAsync($"Received message from {message.Name}: {keys}");

			var foundBadWords = new List<string>();

			foreach (var word in UserGrain.BadWords)
			{
				if (keys.Contains(word))
				{
					foundBadWords.Add(word);
				}
			}

			await this.logger.LogAsync($"Bad word count for {message.Name}: {this.State.BadWords.Count}");

			if (foundBadWords.Count > 0)
			{
				this.State.BadWords.AddRange(foundBadWords);
				await this.WriteStateAsync();

				var badWords = string.Join(", ", foundBadWords);
				await this.notification.Value.SendAsync("ITWatchers@YourCompany.com", "BAD WORDS SAID",
					$"The user {message.Name} typed the following bad words: {badWords}");
			}
		}
	}
}