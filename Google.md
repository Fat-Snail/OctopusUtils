# 谷歌云盘下载工具类

示例：

```C#
GoogleFileDownloader googleFileDownloader = new GoogleFileDownloader();

// 如果使用 DownloadFileAsync 才会触发此事件
googleFileDownloader.DownloadProgressChanged += ( sender, e ) => Console.WriteLine( "下载进度： " + e.BytesReceived + " " + e.TotalBytesToReceive );
//  DownloadFile 和 DownloadFileAsync 都支持此事件
googleFileDownloader.DownloadFileCompleted += ( sender, e ) => 
{
    if( e.Cancelled )
        Console.WriteLine( "Download cancelled" );
    else if( e.Error != null )
        Console.WriteLine( "Download failed: " + e.Error );
    else
        Console.WriteLine( "Download completed" );
};

var filepath = "./Data/Test.zip";

googleFileDownloader.DownloadFileAsync( $"https://drive.google.com/file/d/{google-fileId}}/view?usp=drive_link", filepath.EnsureDirectory() );


//EnsureDirectory() 是newlife.core的方法，目的是检查目录是否存在并创建

```

