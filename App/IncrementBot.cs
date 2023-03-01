/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;


namespace IncrementBot
{

    public class IncrementChannelData
    {
        public ulong ChannelId { get; set; }
        public ulong LastUserId { get; set; } = 0;
        public int CurrentCount { get; set; } = 0;
        public Dictionary<ulong, int> UserCounts { get; set; } = new Dictionary<ulong, int>();
    }

    public class IncrementBot
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private readonly Dictionary<ulong, IncrementChannelData> _incrementChannels = new Dictionary<ulong, IncrementChannelData>();

        public async Task Start(string token)
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.MessageReceived += HandleMessageAsync;
            _client.ReactionAdded += HandleReactionAsync;
            _client.Ready += OnReadyAsync;

            await Task.Delay(-1);
        }

        private Task OnReadyAsync()
        {
            Console.WriteLine($"IncrementBot is connected!");
            return Task.CompletedTask;
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            // Ignore system messages, other bots, and DMs
            if (!(message is SocketUserMessage msg) || msg.Author.IsBot || msg.Channel is IDMChannel) return;

            // Check if this is an "i!begin" command
            if (msg.Content.Equals("i!begin") && msg.Author is SocketGuildUser user && user.GuildPermissions.Administrator)
            {
                // Check if the channel is already an incrementing channel
                if (_incrementChannels.ContainsKey(msg.Channel.Id))
                {
                    await msg.Channel.SendMessageAsync($"This channel is already an incrementing channel!");
                    return;
                }

                _incrementChannels.Add(msg.Channel.Id, new IncrementChannelData());
                await msg.Channel.SendMessageAsync($"This channel is now an incrementing channel!");
            }
            else if (_incrementChannels.ContainsKey(msg.Channel.Id) && int.TryParse(msg.Content, out var count) && count == _incrementChannels[msg.Channel.Id].Count + 1)
            {
                var data = _incrementChannels[msg.Channel.Id];
                var userCount = data.UserCounts.GetValueOrDefault(msg.Author.Id);
                if (userCount >= 1 && data.LastUserId == msg.Author.Id)
                {
                    await msg.Channel.SendMessageAsync($"Sorry {msg.Author.Mention}, you can't go twice in a row!");
                }
                else
                {
                    if (userCount == 0) data.LastUserId = msg.Author.Id;
                    data.UserCounts[msg.Author.Id] = userCount + 1;
                    data.Count = count;
                    await msg.Channel.SendMessageAsync($"<:check:123456789012345678> {count}");
                    if (count % 100 == 0)
                    {
                        var topUsers = GetTopUsers(data.UserCounts);
                        await msg.Channel.SendMessageAsync($"Whoa! That's a big number, check out who contributed the most!\n{string.Join("\n", topUsers)}");
                    }
                    else if (count % 10 == 0)
                    {
                        await msg.Channel.SendMessageAsync($"Nice job {msg.Author.Mention}! Keep it up!");
                    }
                }
            }
            else if (_incrementChannels.ContainsKey(msg.Channel.Id) && int.TryParse(msg.Content, out var incorrectCount))
            {
                var data = _incrementChannels[msg.Channel.Id];
                await msg.Channel.SendMessageAsync(GetRandomIncorrectResponse(data.Count));
                await msg.AddReactionAsync(new Emoji("❌"));
            }
        }

        private string GetRandomIncorrectResponse(int expectedCount)
        {
            string[] incorrectResponses = new string[] {
        $"Nope, the next number is {expectedCount + 1}",
        $"Sorry, it's not {expectedCount}, try {expectedCount + 1}",
        $"That's a good number, but we're looking for {expectedCount + 1}",
        $"Incorrect! The next number is {expectedCount + 1}",
        $"Not quite, next number is {expectedCount + 1}",
        $"Almost there, but not quite {expectedCount + 1}",
        $"Try again! {expectedCount + 1} is the next number",
        $"Better luck next time! {expectedCount + 1} is the correct number",
        $"Nice try, but the next number is {expectedCount + 1}"
            };
            return incorrectResponses[new Random().Next(incorrectResponses.Length)];
        }

        private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> messageCacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            // Check if the reaction is from the bot itself, and if so, ignore it
            if (reaction.UserId == _client.CurrentUser.Id)
                return;

            // Retrieve the message that the reaction was added to from the cache
            var message = await messageCacheable.GetOrDownloadAsync();

            // Check if the message is a valid counting message
            if (message.Author.IsBot || !_countingChannels.ContainsKey(channel.Id) || !int.TryParse(message.Content, out int count) || count != _currentCount + 1)
                return;

            // Check if the user is trying to count twice in a row
            if (message.Author.Id == _lastUserToCount)
            {
                await message.AddReactionAsync(new Emoji("❌"));
                return;
            }

            // Update the current count and the user's score
            _currentCount = count;
            _lastUserToCount = message.Author.Id;
            if (!_userScores.ContainsKey(message.Author.Id))
                _userScores[message.Author.Id] = 1;
            else
                _userScores[message.Author.Id]++;

            // Add a checkmark reaction to the message
            await message.AddReactionAsync(new Emoji("✅"));

            // Check if the current count is a multiple of 10 or 100, and display the leaderboard if so
            if (_currentCount == 10 || _currentCount % 100 == 0)
            {
                await DisplayLeaderboard(channel);
            }
        }

        private async Task DisplayLeaderboard(ISocketMessageChannel channel)
        {
            // Retrieve the top ten users with the highest scores
            var topUsers = _userScores.OrderByDescending(pair => pair.Value).Take(10);

            // Construct the leaderboard message
            var leaderboardMessage = new StringBuilder();
            leaderboardMessage.AppendLine($"Whoa! That's a big number, check out who contributed the most!");
            leaderboardMessage.AppendLine("```");
            leaderboardMessage.AppendLine($"{"User",-30} Score");
            foreach (var userScore in topUsers)
            {
                var user = _client.GetUser(userScore.Key);
                if (user != null)
                {
                    leaderboardMessage.AppendLine($"{user.Username,-30} {userScore.Value}");
                }
            }
            leaderboardMessage.AppendLine("```");

            // Send the leaderboard message to the channel
            await channel.SendMessageAsync(leaderboardMessage.ToString());
        }

    }
}
*/