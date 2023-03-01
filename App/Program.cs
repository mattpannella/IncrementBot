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
                await message.Channel.SendMessageAsync("It's some else's turn.");
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
            if(_state[guild].channel == "" && command != "init") {
                return;
            }
            switch(command) {
                case "leaderboard":
                    foreach (ulong key in _state[guild].userTotals.Keys) {
                        var user = _client.GetUserAsync(key).Result;
                        var Username = user.Username + "#" + user.Discriminator;
                        await message.Channel.SendMessageAsync(Username + ": " + _state[guild].userTotals[key]);
                    }
                    break;
                case "init":
                    _state[guild].channel = message.Channel.Name;
                    await message.Channel.SendMessageAsync("Channel set");
                    await SaveState();
                    break;
                case "help":
                    await message.Channel.SendMessageAsync("figure it out yourself");
                    break;
                default:
                    await message.Channel.SendMessageAsync("i dont know what that means");
                    break;
            }
        }

        // For better functionality & a more developer-friendly approach to handling any kind of interaction, refer to:
        // https://discordnet.dev/guides/int_framework/intro.html
        private async Task InteractionCreatedAsync(SocketInteraction interaction)
        {
            // safety-casting is the best way to prevent something being cast from being null.
            // If this check does not pass, it could not be cast to said type.
            if (interaction is SocketMessageComponent component)
            {
                // Check for the ID created in the button mentioned above.
                if (component.Data.CustomId == "unique-id")
                    await interaction.RespondAsync("Thank you for clicking my button!");

                else
                    Console.WriteLine("An ID has been received that has no handler!");
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