using Microsoft.Extensions.Configuration;
using OxfordOnline.Repositories.Interfaces;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace OxfordOnline.Services
{
    public class FtpService : IFtpService
    {
        private readonly string _ftpHost;
        private readonly string _ftpUser;
        private readonly string _ftpPassword;

        public FtpService(IConfiguration configuration)
        {
            _ftpHost = configuration["Ftp:Host"] ?? throw new ArgumentNullException("Ftp:Host");
            _ftpUser = configuration["Ftp:User"] ?? throw new ArgumentNullException("Ftp:User");
            _ftpPassword = configuration["Ftp:Password"] ?? throw new ArgumentNullException("Ftp:Password");
        }

        public async Task UploadAsync(string remotePath, Stream content)
        {
            var uri = new Uri($"ftp://{_ftpHost}/{remotePath}");
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;

            using var requestStream = await request.GetRequestStreamAsync();
            await content.CopyToAsync(requestStream);
            content.Position = 0; // Opcional, caso queira resetar posição após upload

            using var response = (FtpWebResponse)await request.GetResponseAsync();
            // Opcional: você pode validar response.StatusDescription
        }

        public async Task DeleteAsync(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
                throw new ArgumentException("Caminho remoto inválido.", nameof(remotePath));

            try
            {
                // Criação segura da URI
                var uri = new Uri($"ftp://{_ftpHost}/{remotePath}");

                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
                request.UsePassive = true;
                request.KeepAlive = false;

                using var response = (FtpWebResponse)await request.GetResponseAsync();

                if (response.StatusCode != FtpStatusCode.FileActionOK &&
                    response.StatusCode != FtpStatusCode.CommandOK)
                {
                    throw new Exception($"Falha ao excluir o arquivo: {response.StatusDescription}");
                }
            }
            catch (WebException ex) when (ex.Response is FtpWebResponse ftpResponse)
            {
                // erros do FTP
                var status = ftpResponse.StatusDescription;
                throw new Exception($"Erro FTP ao excluir arquivo '{remotePath}': {status}", ex);
            }
            catch (Exception ex)
            {
                // erros gerais
                throw new Exception($"Erro inesperado ao excluir arquivo '{remotePath}': {ex.Message}", ex);
            }
        }

        public async Task<Stream> DownloadAsync(string remotePath)
        {
            var uri = new Uri($"ftp://{_ftpHost}/{remotePath}");
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(_ftpUser, _ftpPassword);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;

            var response = (FtpWebResponse)await request.GetResponseAsync();
            return response.GetResponseStream();
        }
    }
}
