using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorphanBot
{
    public partial class MorphBot
    {
        private const string MemeFolder = "data/meme/";

        public static async Task GenerateMeme(CommandEventArgs e)
        {
            try
            {
                string imageName = MemeFolder + e.Args[0].ToLower() + ".jpg";
                bool exists = false;
                StringBuilder sb = new StringBuilder();
                foreach (string file in Directory.EnumerateFiles(MemeFolder))
                {
                    string lower = file.ToLower();
                    if (lower == imageName)
                    {
                        exists = true;
                        imageName = file;
                        break;
                    }
                    sb.Append(", ").Append(lower.Substring(MemeFolder.Length).Replace(".jpg", ""));
                }
                if (!exists)
                {
                    await Reply(e, "Invalid meme image! I currently have: " + sb.Remove(0, 2).ToString());
                    return;
                }
                Bitmap bitmap = new Bitmap(imageName);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    GraphicsPath path = new GraphicsPath();
                    StringFormat sf = new StringFormat();
                    sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.FitBlackBox | StringFormatFlags.NoWrap;
                    Font font = new Font(new FontFamily("Impact"), 72F, FontStyle.Bold, GraphicsUnit.Pixel);
                    List<string> wrapped = WrapText(graphics, e.Args[1], bitmap.Width, font, sf);
                    while (font.Size > 0 && wrapped.Count > 2)
                    {
                        font = new Font(new FontFamily("Impact"), font.Size - 12F, FontStyle.Bold, GraphicsUnit.Pixel);
                        wrapped = WrapText(graphics, e.Args[1], bitmap.Width, font, sf);
                    }
                    if (font.Size <= 0)
                    {
                        await Reply(e, "Failed to write text correctly! Try shorter text!");
                        return;
                    }
                    int y = 60;
                    foreach (string s in wrapped)
                    {
                        path.AddString(s, font.FontFamily, (int)font.Style, font.Size, new Point((int)(bitmap.Width / 2F) - (int)(graphics.MeasureString(s, font).Width / 2F), y), sf);
                        y += (int)graphics.MeasureString(s, font).Height + 5;
                    }
                    graphics.FillPath(new SolidBrush(Color.White), path);
                    // TODO: figure out outlining on Linux - Mono borks this right up!
                    graphics.DrawPath(new Pen(Brushes.Black, 5) { LineJoin = LineJoin.Round }, path);
                }
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Jpeg);
                    stream.Seek(0, SeekOrigin.Begin);
                    await e.Channel.SendFile("jpg", stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static List<string> WrapText(Graphics graphics, string text, double pixels, Font font, StringFormat sf)
        {
            string[] originalLines = text.Split(new string[] { " " }, StringSplitOptions.None);

            List<string> wrappedLines = new List<string>();

            StringBuilder actualLine = new StringBuilder();
            double actualWidth = 0;

            foreach (string item in originalLines)
            {
                string word = item + " ";
                actualWidth += graphics.MeasureString(word, font, new PointF(0, 0), sf).Width;

                if (actualWidth > pixels)
                {
                    wrappedLines.Add(actualLine.ToString().TrimEnd());
                    actualLine.Clear();
                    actualWidth = 0;
                }
                actualLine.Append(word);
            }

            if (actualLine.Length > 0)
            {
                wrappedLines.Add(actualLine.ToString().TrimEnd());
            }

            return wrappedLines;
        }
    }
}
