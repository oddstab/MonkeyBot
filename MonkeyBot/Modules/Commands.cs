using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Pixeez2;
using Pixeez2.Objects;

namespace MonkeyBot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private readonly Tokens _pixiv;
        private readonly Random _random;
        private static ulong _lastImageId;

        public Commands(Tokens pixiv)
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
.r 隨機顯示一張圖片
.r <keyword> 搜尋關鍵字然後隨機顯示
.s <id> 顯示特定id的圖片
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
            await Context.Channel.DeleteMessageAsync(id);

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
            await OutputTag(artwork.Illust.Tags.Select(t => t.Name));
        }

        [Command("r")]
        public async Task GenerateRandomImage()
        {
            try
            {
                string[] mode =
                {
                    RankMode.Daily,
                    RankMode.Monthly,
                    RankMode.Weekly
                };

                string rnMode = mode[_random.Next(0, mode.Length - 1)];
                string rnDate = new DateTime(_random.NextLong(633438144000000000, DateTime.Now.Ticks)).ToString("yyyy-MM-dd");
                int perPage = 40;

                //await ReplyAsync(
                //    $"```" +
                //    $"mode:{rnMode}\n" +
                //    $"date:{rnDate}\n" +
                //    $"perPage:{perPage}" +
                //    $"```");

                var illusts = (await _pixiv.GetRankingAllAsync(mode: rnMode, perPage: perPage, date: rnDate))[0].Works;

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
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(
                    $"```\n" +
                    $"哭啊~出錯了 >>> {ex.StackTrace}\n" +
                    $"```");
            }
        }

        [Command("r")]
        public async Task GenerateRandomImageWithKeyword(string keyword)
        {
            try
            {
                int[] popularSearch = { 100, 200, 500, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000, 20000, 30000, 40000, 50000 };
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    await ReplyAsync("哭啊~這是空白啊");
                    return;
                }

                var id = await ReplyAsync("搜尋中...");
                IEnumerable<Work> searchIllust = null;
                List<Work> illust = new List<Work>();

                for (int i = popularSearch.Length - 1; i >= 0; i--)
                {
                    if (illust.Count() >= 50)
                    {
                        break;
                    }

                    searchIllust = await _pixiv.SearchWorksAsync($"{keyword} {popularSearch[i]}users入り", perPage: 50);

                    if (searchIllust == null || !searchIllust.Any()) continue;
                    else
                    {
                        illust.AddRange(searchIllust);
                    }
                }

                if (illust.Count() < 50)
                {
                    searchIllust = await _pixiv.SearchWorksAsync(keyword, perPage: 50);

                    if (searchIllust != null && searchIllust.Any())
                    {
                        illust.AddRange(searchIllust);
                    }
                }

                if (illust.Count() == 0)
                {
                    await Context.Channel.DeleteMessageAsync(id);
                    await Context.Channel.SendMessageAsync("哭啊~找不到");
                    return;
                }

                await Context.Channel.DeleteMessageAsync(id);
                var rnIllust = illust.ElementAt(_random.Next(illust.Count()));
                await Context.Channel.SendMessageAsync($"https://www.pixiv.net/artworks/{rnIllust.Id}");
                await SendImage(rnIllust.ImageUrls.Large);
                await OutputTag(rnIllust.Tags);

            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(
                    $"```\n" +
                    $"哭啊~出錯了 >>> {ex.StackTrace}\n" +
                    $"```");
            }
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
            Console.WriteLine("tag上傳完成");
        }

        /// <summary>
        /// 傳送圖片
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task SendImage(string url)
        {
            int maxSize = 8388608;
            try
            {
                var id = await ReplyAsync("上傳中...");
                Console.WriteLine("上傳中...");
                //WebClient client = new WebClient();
                //client.Headers[HttpRequestHeader.Referer] = "https://www.pixiv.net/";
                //var img = client.DownloadData(url);

                //using Stream stream = new MemoryStream(img);
                //if (stream.Length > maxSize)
                //{
                //    await Context.Channel.DeleteMessageAsync(id);
                //    await ReplyAsync("上傳失敗~超過8Mb了！");
                //    return;
                //}

                //var pathName = Path.GetFileNameWithoutExtension(url);

                //var message = await Context.Channel.SendFileAsync(stream, $"pixiv{Path.GetExtension(url)}");
                //_lastImageId = message.Id;

                var eb = new EmbedBuilder();
                eb.WithImageUrl($"https://cdn.image.oddstab.cf/img?v={url}");

                await Context.Channel.SendMessageAsync("", false, eb.Build());

                //await Context.Channel.SendMessageAsync($"https://cdn.image.oddstab.cf/img?v={url}");

                await Context.Channel.DeleteMessageAsync(id);
                Console.WriteLine("上傳完成");
            }
            catch (Exception ex)
            {
                await ReplyAsync(
                    $"```\n" +
                    $"哭啊~出錯了 >>> {ex.StackTrace}\n" +
                    $"```");
            }
        }
    }
}
