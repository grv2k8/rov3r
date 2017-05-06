using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace rov3r_console
{
    /// <summary>
    /// Console version of rov3r - wallpaper changer.
    /// Fetches a random wallpaper image (provided by https://www.desktoppr.co/api) and applies it as a stretched wallpaper
    /// author: GRJoshi (ever3stmomo@gmail.com)
    /// </summary>
    class rov3r_console
    {
        #region Win32_API_Stuff
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;
        #endregion

        static HttpClient wallpaperApiClient = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine("Rov3r v1 - Console mode");
            Console.WriteLine("Created by: GRJoshi [ever3stmomo@gmail.com]");
            FetchAndApply().Wait();
        }

        static async Task FetchAndApply()
        {
            //main fnx to fetch wallpaper image and apply to desktop
            wallpaperApiClient.BaseAddress = new Uri("https://www.desktoppr.co/api");
            wallpaperApiClient.DefaultRequestHeaders.Accept.Clear();
            wallpaperApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                //get URL for new wallpaper
                var randomImageUrl = await GrabWallpaperUrl("https://api.desktoppr.co/1/wallpapers/random");

                Console.WriteLine("Fetching wallpaper image( " + randomImageUrl + " )");
                Console.WriteLine("Applying background image on device...");

                SetWallpaper(randomImageUrl);               //Apply wallpaper image

                Console.WriteLine("Done. Exiting...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static async Task<string> GrabWallpaperUrl(string path)
        {
            var imageUrl = "";
            HttpResponseMessage response = await wallpaperApiClient.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var imageResource = JObject.Parse(await response.Content.ReadAsStringAsync());
                imageUrl = (string)imageResource["response"]["image"]["url"];
            }
            return imageUrl;
        }

        private static void SetWallpaper(string imageUri)
        {
            Stream imgStream = new System.Net.WebClient().OpenRead(imageUri);
            Image wallImage = Image.FromStream(imgStream);

            string tempPath = Path.Combine(Path.GetTempPath(), "wallpaper.bmp");
            wallImage.Save(tempPath, System.Drawing.Imaging.ImageFormat.Bmp);

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            
            //stretched by default
            key.SetValue(@"WallpaperStyle", 2.ToString());
            key.SetValue(@"TileWallpaper", 0.ToString());

            SystemParametersInfo(SPI_SETDESKWALLPAPER,
                0,
                tempPath,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}
