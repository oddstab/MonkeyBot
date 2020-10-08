using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Pixeez2;

using PixivCS;
using PixivCS.Objects;

namespace MonkeyBot
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private IServiceProvider _services;
        private DiscordSocketClient _client;
        private CommandService _commands;
        private Tokens _pixiv;

        public async Task RunBotAsync()
        {
            using (StreamReader fs = new StreamReader("app.json"))
            {
                App app = JsonConvert.DeserializeObject<App>(fs.ReadToEnd());

                _client = new DiscordSocketClient();
                _commands = new CommandService();
                _pixiv = await Auth.AuthorizeAsync(app.UserName, app.Password);

                //依賴注入
                _services = new ServiceCollection()
                    .AddSingleton(_client)
                    .AddSingleton(_commands)
                    .AddSingleton(_pixiv)
                    .BuildServiceProvider();

                string token = app.DiscordToken;

                _client.Log += ClientLog;

                //註冊指令
                await RegisterCommandsAsync();

                //Bot登入
                await _client.LoginAsync(TokenType.Bot, token);
            }

            //Bot運作
            await _client.StartAsync();

            //阻攔程式 防止關閉
            await Task.Delay(-1);
        }

        private Task ClientLog(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            try
            {
                if (message.Author.IsBot) return;

                int argPosa = 0;
                if (message.HasStringPrefix("~", ref argPosa))
                {
                    var result = await _commands.ExecuteAsync(context, argPosa, _services);
                    if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
                    if (result.Error.Equals(CommandError.UnmetPrecondition)) await message.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync(ex.StackTrace);
            }
            return;
        }
    }
}
