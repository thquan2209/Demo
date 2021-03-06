﻿using Demo.Core;
using Demo.Core.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Demo.Api.Utils
{
    public class UploadUtils
    {
        private static readonly string[] Extensions = { ".jpeg", ".jpg", ".png", "mp4" };
        private const long FileSize = DemoConstants.MaxFileSize;
        private const string UploadFolder = "Uploads";
        private const string DefaultExtension = "png";

        private readonly IHostingEnvironment _hostingEnvironment;
        private static UploadUtils _itself;
        private static readonly object Lock = new object();

        private UploadUtils(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public static UploadUtils Instance(IHostingEnvironment hostingEnvironment)
        {
            if (_itself == null)
            {
                lock (Lock)
                {
                    if (_itself == null)
                    {
                        _itself = new UploadUtils(hostingEnvironment);
                    }
                }
            }

            return _itself;
        }

        public List<string> Save(List<IFormFile> files)
        {
            try
            {
                lock (Lock)
                {
                    var relativePaths = new List<string>();
                    foreach (var file in files)
                    {
                        var fileName = $"{GenerateName()}.{DefaultExtension}";

                        var uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, UploadFolder);

                        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                        var fileAbsolutePath = Path.Combine(uploadFolder, fileName);

                        using (var stream = new FileStream(fileAbsolutePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        var relativePath = $"{UploadFolder}/{fileName}";
                        relativePaths.Add(relativePath);
                    }

                    return relativePaths;
                }
            }
            catch (Exception e)
            {
                throw new DemoException(e.Message);
            }
        }

        public (string, string) Save(string base64)
        {
            try
            {
                lock (Lock)
                {
                    var name = GenerateName();

                    var fileName = $"{name}.{DefaultExtension}";

                    var uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, UploadFolder);

                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    var fileAbsolutePath = Path.Combine(uploadFolder, fileName);

                    var imageBytes = Convert.FromBase64String(base64);
                    File.WriteAllBytes(fileAbsolutePath, imageBytes);

                    var thumbName = $"{name}_thumb.{DefaultExtension}";
                    var thumbAbsolutePath = Path.Combine(uploadFolder, thumbName);

                    var image = base64.Base64ToImage();
                    image = image.Resize(100, 100);
                    image.Save(thumbAbsolutePath, ImageFormat.Png);

                    var relativePath = $"{UploadFolder}/{fileName}";
                    var thumbRelativePath = $"{UploadFolder}/{thumbName}";

                    return (relativePath, thumbRelativePath);
                }
            }
            catch (Exception e)
            {
                throw new DemoException(e.Message);
            }

        }

        public static void IsValid(IFormFile file)
        {
            if (!IsValidFileExtension(file, Extensions))
                throw new DemoException("Image is not valid");

            if (!IsValidFileSize(file, FileSize))
                throw new DemoException($"File size must be less than {FileSize} bytes");
        }

        private static string GenerateName()
        {
            var fileName = DemoUtils.Now().ToString("ddMMyyyyHHmmsstt") + "_" + Guid.NewGuid().ToString("N");

            return fileName;
        }

        private static bool IsValidFileExtension(IFormFile file, string[] extensions)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            return extensions.Any(x => x.ToLower() == extension);
        }

        private static bool IsValidFileSize(IFormFile file, long fileSize)
        {
            return file.Length > 0 && file.Length <= fileSize;
        }
    }
}
