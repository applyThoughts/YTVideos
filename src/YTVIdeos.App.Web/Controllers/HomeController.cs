using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using YTVIdeos.App.Web.Models;
using YTVidoes.Core;

namespace YTVideos.App.Web.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _appSettings;
        private readonly YoutubeService _youtubeService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(ILogger<HomeController> logger,AppSettings appSettings,YoutubeService youtubeService, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _appSettings = appSettings;
            _youtubeService = youtubeService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            
            if (string.IsNullOrEmpty(_appSettings.YouTubeAPIKey))
            {
                ModelState.AddModelError("Error", "Please add your youtube api key in the app settings file.");
            }

            if (ModelState.IsValid && !string.IsNullOrEmpty(_appSettings.DefaultChannelUrl))
            {
                var result = await _youtubeService.GetYoutubeVideosInAChannel(_appSettings.DefaultChannelUrl);
                if (result.IsValid)
                {
                    return View(result.Result);
                }
                else
                {
                    ModelState.AddModelError("Error",result.GetAllErrors());
                }

            }
            return View();
        }
        public async Task<JsonResult> GetChannelVideos(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                ModelState.AddModelError("Error", "Please enter a valid channel url");
            }

            var channel = new YoutubeChannel();
            if (ModelState.IsValid)
            {
                var result = await _youtubeService.GetYoutubeVideosInAChannel(url);
                if (result.IsValid)
                {
                    channel = result.Result;
                }
                else
                {
                    ModelState.AddModelError("Error", result.GetAllErrors());
                }
            }
            return Json(new
            {
                result = "success",
                htmlData = RenderPartialViewToStringAsync("_ListVideos", channel)
            });

        }
        public async Task<IActionResult> DownloadChannelFile(string url)
        {
            if (string.IsNullOrEmpty(_appSettings.ExportPath)) {
                _appSettings.ExportPath =
                    Path.Combine(this._webHostEnvironment.WebRootPath, "files", "youtube-channels");
            }
            var result = await _youtubeService.GetYoutubeVideosInAChannel(url,exportToCSV:true);
            byte[] bytes = await System.IO.File.ReadAllBytesAsync(result.Result.DownloadPath);
            return File(bytes, "application/octet-stream", $"{result.Result.Id}.csv");
        }


    }
}