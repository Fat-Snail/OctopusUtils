namespace Octopus.Tools;

public class GoogleFileDownloader : IDisposable
{
    private const String GOOGLE_DRIVE_DOMAIN = "drive.google.com";
    private const String GOOGLE_DRIVE_DOMAIN2 = "https://drive.google.com";

    // In the worst case, it is necessary to send 3 download requests to the Drive address
    //   1. an NID cookie is returned instead of a download_warning cookie
    //   2. download_warning cookie returned
    //   3. the actual file is downloaded
    private const Int32 GOOGLE_DRIVE_MAX_DOWNLOAD_ATTEMPT = 3;

    public delegate void DownloadProgressChangedEventHandler(Object sender, DownloadProgress progress);

    // Custom download progress reporting (needed for Google Drive)
    public class DownloadProgress
    {
        public Int64 BytesReceived, TotalBytesToReceive;
        public Object UserState;

        public Int32 ProgressPercentage
        {
            get
            {
                if ( TotalBytesToReceive > 0L )
                    return ( Int32 )((( Double )BytesReceived / TotalBytesToReceive) * 100);

                return 0;
            }
        }
    }

    // Web client that preserves cookies (needed for Google Drive)
    private class CookieAwareWebClient : WebClient
    {
        private class CookieContainer
        {
            private readonly Dictionary<String, String> cookies = new Dictionary<String, String>();

            public String this[Uri address]
            {
                get
                {
                    String cookie;
                    if ( cookies.TryGetValue(address.Host, out cookie) )
                        return cookie;

                    return null;
                }
                set { cookies[address.Host] = value; }
            }
        }

        private readonly CookieContainer cookies = new CookieContainer();
        public DownloadProgress ContentRangeTarget;

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if ( request is HttpWebRequest )
            {
                var cookie = cookies[address];
                if ( cookie != null )
                    (( HttpWebRequest )request).Headers.Set("cookie", cookie);

                if ( ContentRangeTarget != null )
                    (( HttpWebRequest )request).AddRange(0);
            }

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            return ProcessResponse(base.GetWebResponse(request, result));
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            return ProcessResponse(base.GetWebResponse(request));
        }

        private WebResponse ProcessResponse(WebResponse response)
        {
            var cookies = response.Headers.GetValues("Set-Cookie");
            if ( cookies != null && cookies.Length > 0 )
            {
                var length = 0;
                for ( var i = 0; i < cookies.Length; i++ )
                    length += cookies[i].Length;

                var cookie = new StringBuilder(length);
                for ( var i = 0; i < cookies.Length; i++ )
                    cookie.Append(cookies[i]);

                this.cookies[response.ResponseUri] = cookie.ToString();
            }

            if ( ContentRangeTarget != null )
            {
                var rangeLengthHeader = response.Headers.GetValues("Content-Range");
                if ( rangeLengthHeader != null && rangeLengthHeader.Length > 0 )
                {
                    var splitIndex = rangeLengthHeader[0].LastIndexOf('/');
                    if ( splitIndex >= 0 && splitIndex < rangeLengthHeader[0].Length - 1 )
                    {
                        Int64 length;
                        if ( Int64.TryParse(rangeLengthHeader[0].Substring(splitIndex + 1), out length) )
                            ContentRangeTarget.TotalBytesToReceive = length;
                    }
                }
            }

            return response;
        }
    }

    private readonly CookieAwareWebClient webClient;
    private readonly DownloadProgress downloadProgress;

    private Uri downloadAddress;
    private String downloadPath;

    private Boolean asyncDownload;
    private Object userToken;

    private Boolean downloadingDriveFile;
    private Int32 driveDownloadAttempt;

    public event DownloadProgressChangedEventHandler DownloadProgressChanged;
    public event AsyncCompletedEventHandler DownloadFileCompleted;

    public GoogleFileDownloader()
    {
        webClient = new CookieAwareWebClient();
        webClient.DownloadProgressChanged += DownloadProgressChangedCallback;
        webClient.DownloadFileCompleted += DownloadFileCompletedCallback;

        downloadProgress = new DownloadProgress();
    }

    public void DownloadFile(String address, String fileName)
    {
        DownloadFile(address, fileName, false, null);
    }

    public void DownloadFileAsync(String address, String fileName, Object userToken = null)
    {
        DownloadFile(address, fileName, true, userToken);
    }

    private void DownloadFile(String address, String fileName, Boolean asyncDownload, Object userToken)
    {
        downloadingDriveFile = address.StartsWith(GOOGLE_DRIVE_DOMAIN) || address.StartsWith(GOOGLE_DRIVE_DOMAIN2);
        if ( downloadingDriveFile )
        {
            address = GetGoogleDriveDownloadAddress(address);
            driveDownloadAttempt = 1;

            webClient.ContentRangeTarget = downloadProgress;
        }
        else
            webClient.ContentRangeTarget = null;

        downloadAddress = new Uri(address);
        downloadPath = fileName;

        downloadProgress.TotalBytesToReceive = -1L;
        downloadProgress.UserState = userToken;

        this.asyncDownload = asyncDownload;
        this.userToken = userToken;

        DownloadFileInternal();
    }

    private void DownloadFileInternal()
    {
        if ( !asyncDownload )
        {
            webClient.DownloadFile(downloadAddress, downloadPath);

            // This callback isn't triggered for synchronous downloads, manually trigger it
            DownloadFileCompletedCallback(webClient, new AsyncCompletedEventArgs(null, false, null));
        }
        else if ( userToken == null )
            webClient.DownloadFileAsync(downloadAddress, downloadPath);
        else
            webClient.DownloadFileAsync(downloadAddress, downloadPath, userToken);
    }

    private void DownloadProgressChangedCallback(Object sender, DownloadProgressChangedEventArgs e)
    {
        if ( DownloadProgressChanged != null )
        {
            downloadProgress.BytesReceived = e.BytesReceived;
            if ( e.TotalBytesToReceive > 0L )
                downloadProgress.TotalBytesToReceive = e.TotalBytesToReceive;

            DownloadProgressChanged(this, downloadProgress);
        }
    }

    private void DownloadFileCompletedCallback(Object sender, AsyncCompletedEventArgs e)
    {
        if ( !downloadingDriveFile )
        {
            if ( DownloadFileCompleted != null )
                DownloadFileCompleted(this, e);
        }
        else
        {
            if ( driveDownloadAttempt < GOOGLE_DRIVE_MAX_DOWNLOAD_ATTEMPT && !ProcessDriveDownload() )
            {
                // Try downloading the Drive file again
                driveDownloadAttempt++;
                DownloadFileInternal();
            }
            else if ( DownloadFileCompleted != null )
                DownloadFileCompleted(this, e);
        }
    }

    // Downloading large files from Google Drive prompts a warning screen and requires manual confirmation
    // Consider that case and try to confirm the download automatically if warning prompt occurs
    // Returns true, if no more download requests are necessary
    private Boolean ProcessDriveDownload()
    {
        var downloadedFile = new FileInfo(downloadPath);
        if ( downloadedFile == null )
            return true;

        // Confirmation page is around 50KB, shouldn't be larger than 60KB
        if ( downloadedFile.Length > 60000L )
            return true;

        // Downloaded file might be the confirmation page, check it
        String content;
        using ( var reader = downloadedFile.OpenText() )
        {
            // Confirmation page starts with <!DOCTYPE html>, which can be preceeded by a newline
            var header = new Char[20];
            var readCount = reader.ReadBlock(header, 0, 20);
            if ( readCount < 20 || !(new String(header).Contains("<!DOCTYPE html>")) )
                return true;

            content = reader.ReadToEnd();
        }

        var linkIndex = content.LastIndexOf("href=\"/uc?");
        if ( linkIndex >= 0 )
        {
            linkIndex += 6;
            var linkEnd = content.IndexOf('"', linkIndex);
            if ( linkEnd >= 0 )
            {
                downloadAddress = new Uri("https://drive.google.com" +
                                          content.Substring(linkIndex, linkEnd - linkIndex).Replace("&amp;", "&"));
                return false;
            }
        }

        var formIndex = content.LastIndexOf("<form id=\"download-form\"");
        if ( formIndex >= 0 )
        {
            var formEndIndex = content.IndexOf("</form>", formIndex + 10);
            var inputIndex = formIndex;
            var sb = new StringBuilder().Append("https://drive.usercontent.google.com/download");
            var isFirstArgument = true;
            while ( (inputIndex = content.IndexOf("<input type=\"hidden\"", inputIndex + 10)) >= 0 &&
                   inputIndex < formEndIndex )
            {
                linkIndex = content.IndexOf("name=", inputIndex + 10) + 6;
                sb.Append(isFirstArgument ? '?' : '&')
                    .Append(content, linkIndex, content.IndexOf('"', linkIndex) - linkIndex).Append('=');

                linkIndex = content.IndexOf("value=", linkIndex) + 7;
                sb.Append(content, linkIndex, content.IndexOf('"', linkIndex) - linkIndex);

                isFirstArgument = false;
            }

            downloadAddress = new Uri(sb.ToString());
            return false;
        }

        return true;
    }

    // Handles the following formats (links can be preceeded by https://):
    // - drive.google.com/open?id=FILEID&resourcekey=RESOURCEKEY
    // - drive.google.com/file/d/FILEID/view?usp=sharing&resourcekey=RESOURCEKEY
    // - drive.google.com/uc?id=FILEID&export=download&resourcekey=RESOURCEKEY
    private String GetGoogleDriveDownloadAddress(String address)
    {
        var index = address.IndexOf("id=");
        Int32 closingIndex;
        if ( index > 0 )
        {
            index += 3;
            closingIndex = address.IndexOf('&', index);
            if ( closingIndex < 0 )
                closingIndex = address.Length;
        }
        else
        {
            index = address.IndexOf("file/d/");
            if ( index < 0 ) // address is not in any of the supported forms
                return String.Empty;

            index += 7;

            closingIndex = address.IndexOf('/', index);
            if ( closingIndex < 0 )
            {
                closingIndex = address.IndexOf('?', index);
                if ( closingIndex < 0 )
                    closingIndex = address.Length;
            }
        }

        var fileID = address.Substring(index, closingIndex - index);

        index = address.IndexOf("resourcekey=");
        if ( index > 0 )
        {
            index += 12;
            closingIndex = address.IndexOf('&', index);
            if ( closingIndex < 0 )
                closingIndex = address.Length;

            var resourceKey = address.Substring(index, closingIndex - index);
            return String.Concat("https://drive.google.com/uc?id=", fileID, "&export=download&resourcekey=",
                resourceKey, "&confirm=t");
        }
        else
            return String.Concat("https://drive.google.com/uc?id=", fileID, "&export=download&confirm=t");
    }

    public void Dispose()
    {
        webClient.Dispose();
    }
}
