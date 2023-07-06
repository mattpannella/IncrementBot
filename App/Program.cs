using Discord;
using Discord.WebSocket;
using System.Text;
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

        private const string STATE_FILE = "_data/state.json";

        private static Color INC_COLOR = new Color(1, 255, 253);
        private static string INC_LOGO = "https://images.squarespace-cdn.com/content/v1/623a01f4bb3fd3071ad90e32/7e2eb108-aec7-4356-a9ce-15d608c5e4f6/webLogo.jpg?format=500w";
        private static string STEAM_PAGE = "https://store.steampowered.com/app/1899820/Increment/";

        private static string VERSION = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        // Discord.Net heavily utilizes TAP for async, so we create
        // an asynchronous context from the beginning.
        static void Main(string[] args)
            => new Program()
                .MainAsync()
                .GetAwaiter()
                .GetResult();

        public Program()
        {
            Console.WriteLine("Increment Bot Version " + VERSION);
            if(File.Exists(STATE_FILE)) {
                string json = File.ReadAllText(STATE_FILE);
                _state = JsonSerializer.Deserialize<Dictionary<ulong, IncremementState>>(json);
                if(_state == null) {
                    Console.WriteLine("Unable to parse state file");
                    _state = new Dictionary<ulong, IncremementState>();
                } else {
                    Console.WriteLine("Loaded state");
                }
            } else {
                Console.WriteLine("State file not found");
                _state = new Dictionary<ulong, IncremementState>();
            }

            // Config used by DiscordSocketClient
            // Define intents for the client
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
                AlwaysDownloadUsers = true
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
            if (message.Content.StartsWith("i!") || message.Content == "!help") {
                await ParseCommand(message);
                return;
            }

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
                if (number.ToString().Contains("69")) {
                    await message.AddReactionAsync(new Emoji("N"));
                    await message.AddReactionAsync(new Emoji("I"));
                    await message.AddReactionAsync(new Emoji("C"));
                    await message.AddReactionAsync(new Emoji("E"));
                }
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
                    Embed leaderboard = await GetLeaderBoard(guild);
                    await message.Channel.SendMessageAsync("", false, leaderboard);
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
            string command;
            if (message.Content == "!help") {
                command = "help";
            } else {
                command = message.Content.Split("i!")[1];
            }

            //PIL - change "init" to "increment"
            if(_state[guild].channel == "" && (command != "increment" && command != "help")) {
                return;
            }
            switch(command) {
                case "leaderboard":
                        Embed leaderboard = await GetLeaderBoard(guild);
                        await message.Channel.SendMessageAsync("", false, leaderboard);
                    break;
                case "increment":
                    var u = message.Author as SocketGuildUser;
                    if(HasManageServerPermission(u)) {
                        _state[guild].channel = message.Channel.Name;
                        await message.Channel.SendMessageAsync("", false, await BuildMessage("Channel set"));
                        await SaveState();
                    } else {
                        await message.Channel.SendMessageAsync("i dont think so");
                    }
                    break;
                case "help":
                    await message.Channel.SendMessageAsync("", false, await BuildMessage($"Increase together forever! Players take turns typing the next number in sequence.\r\nCommands:\r\ni!help - Read this text.\r\ni!increment - An admin must type this in the desired channel- that will become the Incrementing channel!\r\ni!leaderboard - See the top ten contributors to the increasing on this server.\r\ni!global - See the global total of increasing.\r\n\r\nIf you like this Discord game, check out the VR version, [Increment]({STEAM_PAGE})!", "Help"));
                    break;
                case "global":
                    int count = await GetGlobalCount();
                    await message.Channel.SendMessageAsync("", false, await BuildMessage($"There are increasers everywhere! They have increased globally by {count}", "Global Total"));
                    break;
              //  case "globalboard":
                //        Embed globalboard = await GetGlobalLeaderBoard();
                  //      await message.Channel.SendMessageAsync("", false, globalboard);
                    //break;
                default:
                    await message.Channel.SendMessageAsync("", false, await BuildMessage("Invalid command."));
                    break;
            }
        }

        private async Task<Embed> GetLeaderBoard(ulong guild)
        {
            await SortLeaderboard(guild);
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Top Ten Incrementalists");
            StringBuilder list = new StringBuilder();
            int count = 1;
            foreach (var user in _state[guild].userTotals)
            {
                var discordUser = await _client.GetUserAsync(user.Key);
                var username = discordUser.Username;
                var fullUser = _client.GetGuild(guild).GetUser(user.Key);
                if (fullUser != null) {
                    username = fullUser.DisplayName;
                }
                list.AppendLine($"**{count}**. {Format.Sanitize(username)}, {user.Value}");
                count++;
            }
            list.AppendLine("");
            list.AppendLine($"If you like this Discord game, check out the VR version, [Increment]({STEAM_PAGE})!");
            builder.Description = list.ToString();
            builder.WithColor(INC_COLOR);
            return builder.Build();
        }

        private async Task<Embed> BuildMessage(string message, string? title = null)
        {
            return await BuildMessage(new string[] { message }, title);
        }

        private async Task<Embed> BuildMessage(string[] messages, string? title = null)
        {
            EmbedBuilder builder = new EmbedBuilder();
            if(title != null) {
                builder.WithTitle(title);
            } 
            if(title == null && messages.Length == 1) {
                builder.WithTitle(messages[0]);
            } else {
                StringBuilder list = new StringBuilder();
                foreach (string m in messages) {
                    list.AppendLine(m);
                }
                builder.Description = list.ToString();
            }

            //builder.WithThumbnailUrl(INC_LOGO);
            builder.WithColor(INC_COLOR);

            return builder.Build();
        }

        private async Task<int> GetGlobalCount()
        {
            int total = 0;
            foreach(IncremementState i in _state.Values) {
                total += i.count;
            }

            return total;
        }

        private async Task SortLeaderboard(ulong guild)
        {
            var topTenSorted = _state[guild].userTotals.OrderByDescending(u => u.Value).Take(10)
                     .ToDictionary(u => u.Key, u => u.Value);
            _state[guild].userTotals = topTenSorted;
        }

        private async Task<Embed> GetGlobalLeaderBoard()
        {
            await SortGlobalLeaderboard();
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Top Incremental Servers");
            StringBuilder list = new StringBuilder();
            int count = 1;
            foreach (var s in _state)
            {
                var guild = _client.GetGuild(s.Key);
                list.AppendLine($"{guild.Name}, {s.Value.count}");
                count++;
            }
            list.AppendLine("");
            list.AppendLine($"If you like this Discord game, check out the VR version, [Increment]({STEAM_PAGE})!");
            builder.Description = list.ToString();
            builder.WithColor(INC_COLOR);
            return builder.Build();
        }

        private async Task SortGlobalLeaderboard()
        {
            
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
            if (STATE_FILE == null) {
                return;
            }
            string json = JsonSerializer.Serialize(_state);
            File.WriteAllText(STATE_FILE, json);
        }
    }
}