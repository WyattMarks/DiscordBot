using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System;
using Genbox.WolframAlpha;
using Genbox.WolframAlpha.Enums;
using Genbox.WolframAlpha.Objects;
using Genbox.WolframAlpha.Requests;
using Genbox.WolframAlpha.Responses;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Modules {
    [Name("Math")]
    public class MathModule : ModuleBase<SocketCommandContext> {
        private readonly CommandService _service;
        private readonly IConfigurationRoot _config;

        public MathModule(CommandService service, IConfigurationRoot config) {
            _service = service;
            _config = config;
        }

        [Command("multiply")]
        [Summary("Multiply numbers")]
        public async Task Multiply(params int[] numbers) {
            int product = 1;
            foreach (int n in numbers) {
                product *= n;
            }
            await ReplyAsync($"The product of `{string.Join(" * ", numbers)}` = `{product}`.");
        }

        [Command("add"), Alias("sum")]
        [Summary("Sum numbers")]
        public async Task Sum(params int[] numbers) {
            int sum = numbers.Sum();
            await ReplyAsync($"The sum of `{string.Join(" + ", numbers)}` = `{sum}`.");
        }

        [Command("dft")]
        [Summary("Compute the DFT")]
        public async Task DFT(params double[] xn) {
            Complex[] Xk = new Complex[xn.Length];

            string reply = "`[";

            for (int k = 0; k < xn.Length; k++) {
                Xk[k] = 0;
                for (int n = 0; n < xn.Length; n++) {
                    Xk[k] += xn[n] * Complex.Exp(new Complex(0, -2 * Math.PI * k * n / xn.Length));
                }
                if (Xk[k].Imaginary < 0) {
                    reply += $"({Xk[k].Real:F2} - j{Math.Abs(Xk[k].Imaginary):F2})";
                } else {
                    reply += $"({Xk[k].Real:F2} + j{Xk[k].Imaginary:F2})";
                }
                if (k < xn.Length - 1)
                    reply += ", ";
            }

            await ReplyAsync(reply + "]`");
        }

        [Command("wolframalpha"), Alias("wolfram", "wa")]
        [Summary("Ask Wolfram")]
        public async Task Wolfram([Remainder] string text) {
            string wolframToken = _config["tokens:wolframalpha"];
            WolframAlphaClient client = new WolframAlphaClient(wolframToken);
            

            FullResultRequest request = new FullResultRequest(text);

            request.ScanTimeout = 8;
            
            FullResultResponse results = await client.FullResultAsync(request).ConfigureAwait(false);

            if (results.IsError)
                await ReplyAsync($"Error: {results.ErrorDetails.Message}");


            //Results are split into "pods" that contain information. Those pods can have SubPods.
            foreach (Pod pod in results.Pods) {
                foreach (SubPod subPod in pod.SubPods) {
                    var builder = new EmbedBuilder() {
                        Color = new Color(114, 137, 218),
                    };

                    string value = (subPod.Image.Height <= 21) ? subPod.Image.Alt : "Shown below";
                    string name = string.IsNullOrEmpty(subPod.Title) ? pod.Title : subPod.Title;

                    if (!string.IsNullOrEmpty(subPod.Image.Src.ToString()) && subPod.Image.Height > 21) 
                        builder.ImageUrl = subPod.Image.Src.ToString();


                    builder.AddField(x => {
                        x.Name = name;
                        x.IsInline = false;
                        x.Value = value;
                    });

                    await ReplyAsync("", false, builder.Build());
                }
            }
        }
    }
}