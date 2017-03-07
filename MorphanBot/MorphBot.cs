using Discord;
using Discord.Audio;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MorphanBot
{
    public partial class MorphBot
    {
        public readonly DiscordClient Client;
        
        private readonly string BotToken;

        public readonly YAMLConfiguration Configuration;

        static void Main(string[] args)
        {
            MorphBot bot = new MorphBot();
        }

        public MorphBot()
        {
            Configuration = new YAMLConfiguration(GetConfigString());
            if ((BotToken = Configuration.ReadString("discord", null)) == null)
            {
                Console.WriteLine("No 'discord' key with a token in config.yml!");
                return;
            }
            if ((WolframAlpha.AppID = Configuration.ReadString("wolfram", null)) == null)
            {
                Console.WriteLine("No 'wolfram' key with an app ID in config.yml!");
                return;
            }
            Client = new DiscordClient().UsingCommands((commandConfig) =>
            {
                commandConfig.CustomPrefixHandler = (message) =>
                {
                    if (message.RawText.StartsWith("//") || message.RawText.StartsWith("!!"))
                    {
                        return 2;
                    }
                    else if (message.RawText.StartsWith("/"))
                    {
                        return 1;
                    }
                    return -1;
                };
            });

            CommandService commands = Client.GetService<CommandService>();
            commands.CreateCommand("roll").Parameter("dice", ParameterType.Unparsed).Do(async (e) => await RollDice(e));
            commands.CreateCommand("say").Parameter("msg", ParameterType.Unparsed).Do(async (e) => await e.Channel.SendTTSMessage(e.Args[0]));
            commands.CreateCommand("search").Parameter("param", ParameterType.Unparsed).Do(async (e) =>
            {
                await e.Channel.SendIsTyping();
                WolframAlpha.QueryResult output = WolframAlpha.Query(e.Args[0]);
                string result = output.Result;
                if (output.Error || !output.Success || result == null)
                {
                    if (output.Suggestion != null)
                    {
                        await Reply(e, "Sorry, I don't know the meaning of that. Did you mean '" + output.Suggestion + "'?");
                    }
                    else
                    {
                        await Reply(e, "There was an error while parsing that statement.");
                    }
                }
                else
                {
                    if (output.SpellCheck != null)
                    {
                        await Reply(e, output.SpellCheck);
                    }
                    await Reply(e, output.Input + " = " + output.Result);
                }
            });

            Client.Log.Message += (sender, e) =>
            {
                Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");
            };
            Client.UserJoined += (sender, e) =>
            {
                if (!e.User.IsBot)
                {
                    e.Server.DefaultChannel.SendMessage("Welcome to " + e.Server.Name + ", **" + e.User.Name + "**!");
                }
            };
            Client.UserLeft += (sender, e) =>
            {
                if (!e.User.IsBot)
                {
                    e.Server.DefaultChannel.SendMessage("**" + e.User.Name + "** left the server.");
                }
            };

            Client.ExecuteAndWait(async () => await Client.Connect(BotToken, TokenType.Bot));
        }

        private static string GetConfigString()
        {
            try
            {
                return File.ReadAllText("config.yml");
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to read config.yml!");
                return "";
            }
        }

        public static async Task Reply(CommandEventArgs e, params string[] messages)
        {
            foreach (string message in messages)
            {
                await e.Channel.SendIsTyping();
                await e.Channel.SendMessage(e.User.NicknameMention + ", " + message);
            }
        }
    }
}
