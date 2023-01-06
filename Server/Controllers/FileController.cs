using System.Net;
using FileUploadWasm.Server.Data;
using FileUploadWasm.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileUploadWasm.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly DataContext _context;

        public FileController(IWebHostEnvironment environment, DataContext context)
        {
            _environment = environment;
            _context = context;
        }

        [HttpGet("{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var uploadResult = await _context.Uploads.FirstOrDefaultAsync(u => u.StoredFileName != null && u.StoredFileName.Equals(fileName));
            var path = Path.Combine(_environment.ContentRootPath, "uploads", fileName);
            var memory = new MemoryStream();
            await using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }

            memory.Position = 0;
            return File(memory, uploadResult.ContentType, Path.GetFileName(path));
        }
        [HttpPost]
        public async Task<ActionResult<List<UploadResult>>> UploadFile(List<IFormFile> files)
        {
            List<UploadResult> uploadResults = new();
            foreach (var file in files)
            {
                var uploadResult = new UploadResult();
                var untrustedFileName = file.FileName;
                uploadResult.FileName = untrustedFileName;
                //var trustedFileNameForDisplay = WebUtility.HtmlEncode(untrustedFileName);

                var trustedFileNameForStorage = Path.GetRandomFileName();
                var path = Path.Combine(_environment.ContentRootPath, "uploads", trustedFileNameForStorage);
                await using FileStream fs = new(path, FileMode.Create);
                await file.CopyToAsync(fs);

                uploadResult.StoredFileName = trustedFileNameForStorage;
                uploadResult.ContentType = file.ContentType;
                uploadResults.Add(uploadResult);
                _context.Uploads.Add(uploadResult);
            }

            await _context.SaveChangesAsync();
            return Ok(uploadResults);
        }
    }
}
