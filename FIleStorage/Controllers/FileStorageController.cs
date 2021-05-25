using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileStorage.Controllers
{
    [ApiController]
    [Route("[controller]/path/to")]
    public class FileStorageController : ControllerBase
    {
        private Dictionary<StatusCodes, StatusCodeResult> _codeResults;

        public FileStorageController()
        {
            InitializeResultsCodes();
        }

        [HttpGet("{*input}")]
        public ActionResult Get(string input)
        {
            var path = ProcessingFile.RootDirectory + @"\" + input;

            if (Directory.Exists(path))
            {
                if (Directory.GetFiles(path).Length == 0 &&
                    Directory.GetDirectories(path).Length == 0)
                {
                    return NoContent();
                }

                try
                {
                    return new JsonResult(ProcessingFile.GetAllFiles(path));
                }
                catch
                {
                    return StatusCode(500);
                }
            }

            if (System.IO.File.Exists(path))
            {
                try
                {
                    var fileStream = new FileStream(path, FileMode.Open);
                    return File(fileStream, "application/unknown", input);
                }
                catch
                {
                    return StatusCode(500);
                }
            }

            return NotFound();
        }

        [HttpPut("{*input}")]
        public ActionResult Put(IFormFile file, string input)
        {
            const string customCopyHeader = "X-Copy-From";
            var pathCopyFrom = Request.Headers.FirstOrDefault
                (h => h.Key == customCopyHeader).Value;


            if (string.IsNullOrEmpty(pathCopyFrom))
            {
                var statusCode = ProcessingFile.InsertFile(file, input);

                return _codeResults[statusCode];
            }

            if (file == null)
            {
                var statusCode = ProcessingFile.CopyFile(input, pathCopyFrom);

                return _codeResults[statusCode];
            }

            return BadRequest();
        }

        [HttpHead("{*filename}")]
        public ActionResult Head(string filename)
        {
            try
            {
                var fullFileName = ProcessingFile.RootDirectory + @"\" + filename;
                var fileInfo = new FileInfo(fullFileName);

                if (!fileInfo.Exists)
                    return NotFound();

                Response.Headers.Add("Filename", fileInfo.Name);
                Response.Headers.Add("Created-at", fileInfo.CreationTime.ToString(CultureInfo.InvariantCulture));
                Response.Headers.Add("Modified-at", fileInfo.LastWriteTime.ToString(CultureInfo.InvariantCulture));
                Response.Headers.Add("Size", ProcessingFile.SizeSuffix(fileInfo.Length));

                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpDelete("{*input}")]
        public ActionResult Delete(string input)
        {
            try
            {
                var path = ProcessingFile.RootDirectory + @"\" + input;

                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    return Ok();
                }

                if (!Directory.Exists(path))
                    return NotFound();

                if (string.IsNullOrEmpty(input))
                    return BadRequest();

                Directory.Delete(path, true);
                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        private void InitializeResultsCodes()
        {
            _codeResults = new Dictionary<StatusCodes, StatusCodeResult>
            {
                {StatusCodes.Ok, Ok()},
                {StatusCodes.Created, StatusCode(201)},
                {StatusCodes.BadRequest, BadRequest()},
                {StatusCodes.InternalServerError, StatusCode(500)}
            };

        }
    }
}
