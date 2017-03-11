﻿using Akka.Actor;
using Akka.DI.Core;
using KeyWatcher.Dependencies;
using KeyWatcher.Messages;
using System;
using System.Collections.Generic;

namespace KeyWatcher.Actors
{
	public sealed class UserActor
		: ReceiveActor
	{
		private static readonly string[] BadWords = { "cotton", "headed", "ninny", "muggins" };
		private readonly ILogger logger;

		public UserActor(ILogger logger)
		{
			if (logger == null)
			{
				throw new ArgumentNullException(nameof(logger));
			}

			this.logger = logger;
			this.Receive<UserKeysMessage>(message => this.Handle(message));
		}

		private void Handle(UserKeysMessage message)
		{
			var keys = new string(message.Keys).ToLower();
			this.logger.LogAsync($"Received message from {message.Name}: {keys}")
				.PipeTo(this.Self);

			var foundBadWords = new List<string>();

			foreach (var word in UserActor.BadWords)
			{
				if (keys.Contains(word))
				{
					foundBadWords.Add(word);
				}
			}

			if (foundBadWords.Count > 0)
			{
				var notification = Context.ActorOf(
					Context.DI().Props<EmailActor>());
				notification.Tell(new UserBadWordsMessage(
					message.Name, foundBadWords.ToArray()));
			}
		}
	}
}
