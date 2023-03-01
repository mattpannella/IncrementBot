using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BasicBot
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
        private string token = "MTA4MDE5MDUxODIwMzUzNTM4MA.Gm9_oq.ccyhrme_6hLd0XHZ7W74iMn7SNAzZ2-WTd2XN8";
        private Dictionary<ulong, int> userTotals;
        private ulong mostRecentUser;

        private int count = 0;

        // Discord.Net heavily utilizes TAP for async, so we create
        // an asynchronous context from the beginning.
        static void Main(string[] args)
            => new Program()
                .MainAsync()
                .GetAwaiter()
                .GetResult();

        public Program()
        {
            userTotals = new Dictionary<ulong, int>();

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
            await _client.LoginAsync(TokenType.Bot, token);
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
            // The bot should never respond to itself. dont respond if same user twice in a row
            if (message.Author.Id == _client.CurrentUser.Id || message.Author.Id == mostRecentUser) {
                return;
            }

            //check for leaderboard command. return afterwards
            if (message.Content == "i!leaderboard") {
                foreach (ulong key in userTotals.Keys) {
                    var user = _client.GetUserAsync(key).Result;
                    var Username = user.Username + "#" + user.Discriminator;
                    await message.Channel.SendMessageAsync("User " + Username + ": " + userTotals[key]);
                    return;
                }
            }

            //number will contain the integer from the chat, if its a int
            int number;
            bool check = int.TryParse(message.Content, out number);
            
            //if it was an int and its previous+1
            if(check != false && number == (count+1)) {
                count++;
                await message.AddReactionAsync(new Emoji("✅"));
                ulong userid = message.Author.Id;
                mostRecentUser = message.Author.Id;
                if(!userTotals.ContainsKey(userid)){
                    userTotals.Add(userid, 1);
                } else {
                    userTotals[userid]++;
                }
            } else {
                //otherwise send the fail  message
                await message.Channel.SendMessageAsync(GetRandomIncorrectResponse(count));
                await message.AddReactionAsync(new Emoji("❌"));
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
    }
}