using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

using Pixeez2;

namespace MonkeyBot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private readonly Tokens _pixiv;
        private readonly Random _random;
        private static ulong _lastLinkId;
        private static ulong _lastImageId;
        private static ulong _lastTagId;

        public Commands(DiscordSocketClient client, Tokens pixiv)
        {
            this._pixiv = pixiv;
            this._random = new Random(Guid.NewGuid().GetHashCode());
        }

        [Command("c")]
        public async Task CleanLast()
        {
            await Context.Channel.DeleteMessageAsync(_lastImageId);
        }

        [Command("help")]
        [Alias("h")]
        public async Task Help()
        {
            await ReplyAsync(@"```==使用說明==
~r 隨機顯示一張圖片
~r <keyword> 搜尋關鍵字然後隨機顯示
~s <id> 顯示特定id的圖片
===========```");
        }

        [Command("s")]
        public async Task ShowDetail(long illustsId)
        {
            var id = await ReplyAsync("搜尋中...");
            var artwork = await _pixiv.GetIllustDetail(illustsId);

            if (artwork == null || artwork.Illust == null)
            {
                await ReplyAsync("哭啊~找不到");
                return;
            }

            await ReplyAsync($"https://www.pixiv.net/artworks/{illustsId}");
            if (artwork.Illust.MetaPages != null && artwork.Illust.MetaPages.Count > 0)
            {
                foreach (var item in artwork.Illust.MetaPages)
                {
                    await SendImage(item.ImageUrls.Original);
                }
            }
            else
            {
                await SendImage(artwork.Illust.MetaSinglePage.OriginalImageUrl);
            }
            await Context.Channel.DeleteMessageAsync(id);
            await OutputTag(artwork.Illust.Tags.Select(t => t.Name));
        }

        [Command("r")]
        public async Task RandomGenerateImage()
        {
            var illusts = (await _pixiv.GetRankingAllAsync(mode: "monthly", perPage: 300))[0].Works;

            if (illusts == null || illusts.Count == 0)
            {
                await ReplyAsync("哭啊~找不到");
                return;
            }
            var rnIllust = illusts[_random.Next(illusts.Count)].Work;
            await ReplyAsync($"https://www.pixiv.net/artworks/{rnIllust.Id}");
            await SendImage(rnIllust.ImageUrls.Large);
            await OutputTag(rnIllust.Tags);
        }

        [Command("r")]
        public async Task RandomGenerateImageWithKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                await ReplyAsync("哭啊~找不到");
                return;
            }

            var illust = await _pixiv.SearchWorksAsync(keyword, page: _random.Next(5), perPage: 50);

            if (illust == null || illust.Count == 0)
            {
                await Context.Channel.SendMessageAsync("哭啊~找不到");
                return;
            }

            var rnIllust = illust[_random.Next(illust.Count)];
            await Context.Channel.SendMessageAsync($"https://www.pixiv.net/artworks/{rnIllust.Id}");
            await SendImage(rnIllust.ImageUrls.Large);
            await OutputTag(rnIllust.Tags);
        }

        /// <summary>
        /// 輸出Tag
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        private async Task OutputTag(IEnumerable<string> tags)
        {
            Console.WriteLine("上傳tags...");
            string result = "";
            foreach (var tag in tags)
            {
                result += $"`#{tag}` ";
            }
            await ReplyAsync(result);
            Console.WriteLine("tags上傳完成");
        }

        /// <summary>
        /// 傳送圖片
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task SendImage(string url)
        {
            try
            {
                var id = await ReplyAsync("上傳中...");
                Console.WriteLine("上傳中...");
                WebClient client = new WebClient();
                client.Headers[HttpRequestHeader.Referer] = "https://www.pixiv.net/";
                var img = client.DownloadData(url);

                using Stream stream = new MemoryStream(img);
                var pathName = Path.GetFileNameWithoutExtension(url);

                var message = await Context.Channel.SendFileAsync(stream, $"pixiv{Path.GetExtension(url)}");
                _lastImageId = message.Id;

                await Context.Channel.DeleteMessageAsync(id);
                Console.WriteLine("上傳完成");
            }
            catch (Exception ex)
            {
                await ReplyAsync($"```diff\n" +
                    $"- 哭啊~出錯了 >>> {ex.StackTrace}\n" +
                    $"```");
            }
        }
    }
}
