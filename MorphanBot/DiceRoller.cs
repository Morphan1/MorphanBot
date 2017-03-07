using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MorphanBot
{
    public partial class MorphBot
    {
        public static async Task RollDice(CommandEventArgs e)
        {
            await e.Channel.SendIsTyping();
            string input = e.Args[0].ToLowerInvariant();
            Match match = Regex.Match(input, @"(\d+)?d(\d+)");
            if (match.Success)
            {
                while (match.Success)
                {
                    int dice = 1;
                    if (match.Groups[1].Success)
                    {
                        dice = Utilities.StringToInt(match.Groups[1].Value);
                    }
                    int sides = Utilities.StringToInt(match.Groups[2].Value);
                    StringBuilder sb = new StringBuilder();
                    if (dice > 1)
                    {
                        sb.Append("(");
                    }
                    for (int i = 0; i < dice; i++)
                    {
                        int roll = Utilities.random.Next(1, sides + 1);
                        sb.Append(roll).Append(" + ");
                    }
                    sb.Remove(sb.Length - 3, 3);
                    if (dice > 1)
                    {
                        sb.Append(")");
                    }
                    string final = sb.Length == 0 ? "0" : sb.ToString();
                    input = input.Replace(match.Index, match.Length, final);
                    match = Regex.Match(input, @"(\d+)?d(\d+)");
                }
                string err;
                List<MathOperation> calc = MonkeyMath.Parse(input, out err);
                if (err != null)
                {
                    await Reply(e, "Failed: " + err);
                    return;
                }
                if (!MonkeyMath.Verify(calc, MonkeyMath.BaseFunctions, out err))
                {
                    await Reply(e, "Failed to verify: " + err);
                    return;
                }
                await Reply(e, "You rolled: " + input, "Total roll: " + MonkeyMath.Calculate(calc, MonkeyMath.BaseFunctions));
            }
            else
            {
                await Reply(e, "You must specify at least set of dice to roll!");
            }
        }
    }
}
