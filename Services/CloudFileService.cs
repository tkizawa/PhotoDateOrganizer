using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace PhotoDateOrganizer.Services;

/// <summary>
/// OneDrive / SharePoint 等の Windows クラウドファイル（Cloud Filter API）を操作するサービスインターフェース
/// </summary>
public interface ICloudFileService
{
    /// <summary>
    /// 指定されたファイルがオンライン専用（ローカルに実体がないプレースホルダー）かどうかを判定します。
    /// </summary>
    bool IsCloudOnlyFile(string filePath);

    /// <summary>
    /// オンライン専用ファイルをローカルにダウンロード（Hydrate）します。
    /// </summary>
    Task<bool> HydrateFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// ローカルにダウンロードされたファイルをクラウド専用（Dehydrate / 空き容量を増やす）に戻します。
    /// </summary>
    bool DehydrateFile(string filePath, out string? errorMessage);

    /// <summary>
    /// ローカルにダウンロードされたファイルを非同期でクラウド専用（Dehydrate / 空き容量を増やす）に戻します。
    /// </summary>
    Task<(bool success, string? errorMessage)> DehydrateFileAsync(string filePath, CancellationToken cancellationToken = default);
}


/// <summary>
/// Windows Cloud Filter API (cldapi.dll) を利用したクラウドファイル操作サービスの実装
/// </summary>
public class CloudFileService : ICloudFileService
{
    // Windows Cloud Filter API 定数
    private const uint CF_HYDRATE_FLAG_NONE = 0x00000000;
    private const uint CF_DEHYDRATE_FLAG_NONE = 0x00000000;

    // Win32 CreateFile 定数
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_READ_DATA = 0x0001;
    private const uint FILE_WRITE_DATA = 0x0002;
    private const uint FILE_READ_ATTRIBUTES = 0x0080;
    private const uint FILE_WRITE_ATTRIBUTES = 0x0100;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FILE_SHARE_DELETE = 0x00000004;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;

    // クラウドファイル属性
    private const FileAttributes AttributeRecallOnOpen = (FileAttributes)0x00040000;
    private const FileAttributes AttributeRecallOnDataAccess = (FileAttributes)0x00400000;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("cldapi.dll", ExactSpelling = true, SetLastError = true)]
    private static extern int CfHydratePlaceholder(
        SafeFileHandle fileHandle,
        long startingOffset,
        long length,
        uint hydrateFlags,
        IntPtr overlapped);

    [DllImport("cldapi.dll", ExactSpelling = true, SetLastError = true)]
    private static extern int CfDehydratePlaceholder(
        SafeFileHandle fileHandle,
        long startingOffset,
        long length,
        uint dehydrateFlags,
        IntPtr overlapped);

    /// <summary>
    /// 指定されたファイルがオンライン専用（ローカルに実体がないプレースホルダー）かどうかを判定します。
    /// </summary>
    public bool IsCloudOnlyFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var attributes = File.GetAttributes(filePath);

            // Offline または RecallOnDataAccess または RecallOnOpen 属性がある場合はローカルに実体がない
            return (attributes & FileAttributes.Offline) != 0 ||
                   (attributes & AttributeRecallOnOpen) != 0 ||
                   (attributes & AttributeRecallOnDataAccess) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// オンライン専用ファイルをローカルにダウンロード（Hydrate）します。
    /// </summary>
    public async Task<bool> HydrateFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        // 既にローカルにある場合は即時成功
        if (!IsCloudOnlyFile(filePath))
        {
            return true;
        }

        return await Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Windows Cloud Filter は通常 FileStream で読み込みアクセスを行うことで
            // カーネルドライバが同期的にクラウドから完全ダウンロードを実行します。
            try
            {
                const int bufferSize = 128 * 1024; // 128KB
                var buffer = new byte[bufferSize];

                using (var fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize,
                    FileOptions.SequentialScan | FileOptions.Asynchronous))
                {
                    while (await fs.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                // ダウンロード完了後にオンライン専用フラグが解除されているか確認（最大2秒ポーリング）
                for (int i = 0; i < 20; i++)
                {
                    if (!IsCloudOnlyFile(filePath))
                    {
                        return true;
                    }
                    await Task.Delay(100, cancellationToken);
                }

                return !IsCloudOnlyFile(filePath);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// ローカルにダウンロードされたファイルをクラウド専用（Dehydrate / 空き容量を増やす）に戻します。
    /// </summary>
    public bool DehydrateFile(string filePath, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            errorMessage = "ファイルが存在しません。";
            return false;
        }

        // 既にクラウド専用の場合は何もしない
        if (IsCloudOnlyFile(filePath))
        {
            return true;
        }

        // 直前のファイルアクセス（コピー等）のハンドル解放待ちのため、最大3回リトライ
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                // CfDehydratePlaceholder に必要な権限でハンドルを取得
                // 注: ダウンロード済みファイルはリパースポイントではないため FILE_FLAG_OPEN_REPARSE_POINT は指定しない
                using var handle = CreateFileW(
                    filePath,
                    FILE_WRITE_DATA | FILE_READ_DATA | FILE_WRITE_ATTRIBUTES | FILE_READ_ATTRIBUTES,
                    FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_FLAG_BACKUP_SEMANTICS,
                    IntPtr.Zero);

                if (handle.IsInvalid)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (attempt < 3)
                    {
                        Thread.Sleep(150);
                        continue;
                    }
                    errorMessage = $"ファイルハンドルの取得に失敗しました (Win32Error: {error})";
                    return false;
                }

                // CfDehydratePlaceholder を呼び出し、ファイル全体 (StartingOffset=0, Length=-1) を解放
                int hr = CfDehydratePlaceholder(handle, 0, -1, CF_DEHYDRATE_FLAG_NONE, IntPtr.Zero);
                if (hr != 0) // S_OK 以外
                {
                    if (attempt < 3)
                    {
                        Thread.Sleep(150);
                        continue;
                    }
                    errorMessage = $"CfDehydratePlaceholder が失敗しました (HRESULT: 0x{hr:X8})";
                    return false;
                }

                return true;
            }
            catch (DllNotFoundException)
            {
                errorMessage = "cldapi.dll が見つかりません。このバージョンの Windows では Dehydrate をサポートしていません。";
                return false;
            }
            catch (Exception ex)
            {
                if (attempt < 3)
                {
                    Thread.Sleep(150);
                    continue;
                }
                errorMessage = $"Dehydrate 実行中に例外が発生しました: {ex.Message}";
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// ローカルにダウンロードされたファイルを非同期でクラウド専用（Dehydrate / 空き容量を増やす）に戻します（最大5秒タイムアウト保護）。
    /// </summary>
    public async Task<(bool success, string? errorMessage)> DehydrateFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var dehydrateTask = Task.Run(() =>
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                bool success = DehydrateFile(filePath, out var errorMessage);
                return (success, errorMessage);
            }, linkedCts.Token);

            var completedTask = await Task.WhenAny(dehydrateTask, Task.Delay(5000, linkedCts.Token));
            if (completedTask == dehydrateTask)
            {
                return await dehydrateTask;
            }
            else
            {
                return (false, "クラウド専用化（Dehydrate）処理がタイムアウトしました。");
            }
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            return (false, "クラウド専用化（Dehydrate）処理がタイムアウトしました。");
        }
        catch (Exception ex)
        {
            return (false, $"Dehydrate中に例外が発生しました: {ex.Message}");
        }
    }
}



