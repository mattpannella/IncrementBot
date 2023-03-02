using Discord;
using Discord.WebSocket;
using System.Text.Json;

namespace IncrementBot
{
    // This is a minimal, bare-bones example of using Discord.Net.
    //
    // If writing a bot with commands/interactions, we recommend using the Discord.Net.Commands/Discord.Net.Interactions
    // framework, rather than handling them yourself, like we do in this sample.
    //
    // You can find samples of using the command framework:
    // - Here, under the TextCommandFramework sample
    // - At the guides: https://discordnet.dev/guides/text_commands/intro.html
    //
    // You can find samples of using the interaction framework:
    // - Here, under the InteractionFramework sample
    // - At the guides: https://discordnet.dev/guides/int_framework/intro.html
    class Program
    {
        // Non-static readonly fields can only be assigned in a constructor.
        // If you want to assign it elsewhere, consider removing the readonly keyword.
        private readonly DiscordSocketClient _client;
        private Dictionary<ulong, IncremementState> _state;

        // Discord.Net heavily utilizes TAP for async, so we create
        // an asynchronous context from the beginning.
        static void Main(string[] args)
            => new Program()
                .MainAsync()
                .GetAwaiter()
                .GetResult();

        public Program()
        {
            string file = Environment.GetEnvironmentVariable("STATE");
            if(File.Exists(file)) {
                string json = File.ReadAllText(file);
                _state = JsonSerializer.Deserialize<Dictionary<ulong, IncremementState>>(json);
            } else {
                _state = new Dictionary<ulong, IncremementState>();
            }

            // Config used by DiscordSocketClient
            // Define intents for the client
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            // It is recommended to Dispose of a client when you are finished
            // using it, at the end of your app's lifetime.
            _client = new DiscordSocketClient(config);

            // Subscribing to client events, so that we may receive them whenever they're invoked.
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.InteractionCreated += InteractionCreatedAsync;
        }

        public async Task MainAsync()
        {
            // Tokens should be considered secret data, and never hard-coded.
            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TOKEN"));
            // Different approaches to making your token a secret is by putting them in local .json, .yaml, .xml or .txt files, then reading them on startup.

            await _client.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        // This is not the recommended way to write a bot - consider
        // reading over the Commands Framework sample.
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            var chnl = message.Channel as SocketGuildChannel;
            var guild = chnl.Guild.Id;
            _state.TryAdd(guild, new IncremementState());

            if (message.Author.Id == _client.CurrentUser.Id) {
                return;
            }

            //check for leaderboard command. return afterwards
            if (message.Content.StartsWith("i!")) {
                await ParseCommand(message);
                return;
            }
            //allow commands without trying to count
            //react to missed numbers, and same users. message to take turns
            //top 10 only, sorted. single message
            //i!help
            //only one channel. admin needs to initialize count in channel to become counting channel
            //save count and channel from admin command to external file and initialize

            //stretch goal: multi server

            // The bot should never respond to itself. dont respond if same user twice in a row
            if (message.Channel.Name != _state[guild].channel) {
                return;
            }

            //number will contain the integer from the chat, if its a int
            int number;
            bool check = int.TryParse(message.Content, out number);
            
            //if it was an int and its previous+1
            if(check != false) {
                await ParseNumber(message, number);
            }
        }

        private async Task ParseNumber(SocketMessage message, int number)
        {
            var chnl = message.Channel as SocketGuildChannel;
            var guild = chnl.Guild.Id;
            if (message.Author.Id == _state[guild].mostRecentUser) {
                await message.AddReactionAsync(new Emoji("❌"));
                await message.Channel.SendMessageAsync("It's someone else's turn.");
            } 
            else if(number == (_state[guild].count+1)) {
                _state[guild].count++;
                await message.AddReactionAsync(new Emoji("✅"));
                ulong userid = message.Author.Id;
                _state[guild].mostRecentUser = message.Author.Id;
                if(!_state[guild].userTotals.ContainsKey(userid)){
                    _state[guild].userTotals.Add(userid, 1);
                } else {
                    _state[guild].userTotals[userid]++;
                }

                //auto leaderboard display on 10 and multiples of 100
                if(_state[guild].count == 10 || _state[guild].count % 100 == 0)
                {
                    await message.Channel.SendMessageAsync("An excellent milestone, let's see the leaderboard!");
                    string leaderboard = await GetLeaderBoard(guild);
                    await message.Channel.SendMessageAsync(leaderboard);
                }

                await SaveState();
            } else {
                //otherwise send the fail  message
                await message.Channel.SendMessageAsync(GetRandomIncorrectResponse(_state[guild].count));
                await message.AddReactionAsync(new Emoji("❌"));
            }
        }

        private async Task ParseCommand(SocketMessage message)
        {
            var chnl = message.Channel as SocketGuildChannel;
            var guild = chnl.Guild.Id;
            string command = message.Content.Split("i!")[1];

            //PIL - change "init" to "increment"
            if(_state[guild].channel == "" && command != "increment") {
                return;
            }
            switch(command) {
                case "leaderboard":
                        string leaderboard = await GetLeaderBoard(guild);
                        await message.Channel.SendMessageAsync(leaderboard);
                    break;
                case "increment":
                    var u = message.Author as SocketGuildUser;
                    if(HasManageServerPermission(u)) {
                        _state[guild].channel = message.Channel.Name;
                        await message.Channel.SendMessageAsync("Channel set");
                        await SaveState();
                    } else {
                        await message.Channel.SendMessageAsync("i dont think so");
                    }
                    break;
                case "help":
                    await message.Channel.SendMessageAsync("Increase together forever! Players take turns typing the next number in sequence.\r\nCommands:\r\ni!help - Read this text.\r\ni!increment - An admin must type this in the desired channel- that will become the Incrementing channel!\r\ni!leaderboard - See the top ten contributors to the increasing on this server.\r\ni!global - See the global total of increasing.\r\n\r\nIf you like this Discord game, check out the VR version, \"Increment\"!");
                    break;
                case "global":
                    await message.Channel.SendMessageAsync("There are increasers everywhere! They have increased globally by" + "???");
                    break;
                default:
                    await message.Channel.SendMessageAsync("Invalid command.");
                    break;
            }
        }

        private async Task SortLeaderboard(ulong guild)
        {
            var topTenSorted = _state[guild].userTotals.OrderByDescending(u => u.Value).Take(10)
                     .ToDictionary(u => u.Key, u => u.Value);
            _state[guild].userTotals = topTenSorted;
        }

        private async Task<string> GetLeaderBoard(ulong guild)
        {
            await SortLeaderboard(guild);
            string output = "Top Ten Incrementalists\r\n";
            int count = 1;
            foreach (var user in _state[guild].userTotals)
            {
                var discordUser = await _client.GetUserAsync(user.Key);
                var username = discordUser.Username + "#" + discordUser.Discriminator;
                output += $"#{count} {username}: {user.Value}\r\n";
                count++;
            }

            return output;
        }

        // For better functionality & a more developer-friendly approach to handling any kind of interaction, refer to:
        // https://discordnet.dev/guides/int_framework/intro.html
        private async Task InteractionCreatedAsync(SocketInteraction interaction)
        {
            // safety-casting is the best way to prevent something being cast from being null.
            // If this check does not pass, it could not be cast to said type.
            /*
            if (interaction is SocketMessageComponent component)
            {
                // Check for the ID created in the button mentioned above.
                if (component.Data.CustomId == "unique-id")
                    await interaction.RespondAsync("Thank you for clicking my button!");

                else
                    Console.WriteLine("An ID has been received that has no handler!");
            }*/
        }

        private string GetRandomIncorrectResponse(int count)
        {
            string[] incorrectResponses = new string[] {
                $"Nope, the next number is {count + 1}",
                $"Sorry, that's a good number, but the previous number was {count}",
                $"That's a good number, but I'm looking for {count + 1}",
                $"One of my favorite numbers, but the next number is {count + 1}",
                $"Not quite, the previous number was {count}",
                $"Almost there, but not quite {count + 1}",
                $"Try again! {count + 1} is the next number",
                $"Better luck next time! {count + 1} is the correct number",
                $"I wish, but the next number is {count + 1}"
            };
            return incorrectResponses[new Random().Next(incorrectResponses.Length)];
        }

        private bool HasManageServerPermission(SocketGuildUser user)
        {
            return user.GuildPermissions.ManageGuild;
        }

        private async Task SaveState()
        {
            string file = Environment.GetEnvironmentVariable("STATE");
            if (file == null) {
                return;
            }
            string json = JsonSerializer.Serialize(_state);
            File.WriteAllText(file, json);
        }
    }
}