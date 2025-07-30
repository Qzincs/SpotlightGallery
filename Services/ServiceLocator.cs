using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotlightGallery.Services
{
    public static class ServiceLocator
    {
        public static IWallpaperService WallpaperService { get; } = new WallpaperService();
    }

}
