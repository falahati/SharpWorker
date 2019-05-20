using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using SharpWorker.Log;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.DataBackup
{
    // ReSharper disable once HollowTypeName
    [WebApiController("backup")]
    public class DataBackupController : WebApiController
    {
        private readonly DataBackupWorker _backupWorker;
        private readonly Logger _logger;

        public DataBackupController(DataBackupWorker backupWorker, Logger logger)
        {
            _backupWorker = backupWorker;
            _logger = logger;
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(byte[]))]
        [WebApiRequest(WebApiRequestMethod.Get, "Backups/{backupFile}", "download")]
        public WebApiResponse DownloadBackup(string backupFile)
        {
            try
            {
                var backup = _backupWorker.GetBackups().FirstOrDefault(s =>
                    s.FileName.Equals(backupFile, StringComparison.InvariantCultureIgnoreCase));

                var file = backup?.GetFile();

                if (file?.Exists != true)
                {
                    return NotFound();
                }

                if (backup.State == DataBackupState.Ongoing)
                {
                    return BadRequest();
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                };

                response.Content.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = file.Name
                    };

                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                return ResponseMessage(response);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataBackupController), nameof(DownloadBackup), LogType.Error, e);

                return InternalServerError(e);
            }
        }


        [WebApiResponse(HttpStatusCode.OK, typeof(DataBackupStatus[]))]
        [WebApiRequest(WebApiRequestMethod.Get, "", "read")]
        public WebApiResponse GetBackups()
        {
            try
            {
                return Ok(_backupWorker.GetBackups());
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataBackupController), nameof(GetBackups), LogType.Error, e);

                return InternalServerError(e);
            }
        }


        [WebApiResponse(HttpStatusCode.OK, typeof(bool))]
        [WebApiRequest(WebApiRequestMethod.Get, "IsBackingUp", "read")]
        public WebApiResponse IsBackingUp()
        {
            try
            {
                return Ok(_backupWorker.IsBackingUp);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataBackupController), nameof(IsBackingUp), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK)]
        [WebApiRequest(WebApiRequestMethod.Post, "StartBackup", "write")]
        public WebApiResponse StartBackup()
        {
            try
            {
                if (_backupWorker.IsBackingUp)
                {
                    return BadRequest();
                }

                var _ = _backupWorker.BackupDataStoreCallback(CancellationToken.None);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataBackupController), nameof(StartBackup), LogType.Error, e);

                return InternalServerError(e);
            }
        }
    }
}