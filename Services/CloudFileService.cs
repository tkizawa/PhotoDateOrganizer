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

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // アプローチ1: cldapi.dll の CfHydratePlaceholder を試行
            try
            {
                using var handle = CreateFileW(
                    filePath,
                    GENERIC_READ | FILE_READ_ATTRIBUTES,
                    FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                    IntPtr.Zero);

                if (!handle.IsInvalid)
                {
                    int hr = CfHydratePlaceholder(handle, 0, -1, CF_HYDRATE_FLAG_NONE, IntPtr.Zero);
                    if (hr == 0) // S_OK
                    {
                        return true;
                    }
                }
            }
            catch (DllNotFoundException)
            {
                // cldapi.dll がない環境の場合はストリーム読み込みフォールバックへ
            }
            catch (EntryPointNotFoundException)
            {
                // エントリポイントがない場合もフォールバックへ
            }
            catch
            {
                // その他の例外時もフォールバックを試みる
            }

            cancellationToken.ThrowIfCancellationRequested();

            // アプローチ2: ファイルを読み込みオープンして Windows Cloud Filter に自動ダウンロードさせる
            try
            {
                const int bufferSize = 81920;
                var buffer = new byte[bufferSize];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, FileOptions.SequentialScan))
                {
                    // 先頭だけでなく全体をストリームで読み切ることで完全ダウンロードを保証
                    while (fs.Read(buffer, 0, buffer.Length) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
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

        try
        {
            // CfDehydratePlaceholder に必要な権限でハンドルを取得
            using var handle = CreateFileW(
                filePath,
                FILE_WRITE_DATA | FILE_READ_DATA | FILE_WRITE_ATTRIBUTES | FILE_READ_ATTRIBUTES,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                errorMessage = $"ファイルハンドルの取得に失敗しました (Win32Error: {error})";
                return false;
            }

            // CfDehydratePlaceholder を呼び出し、ファイル全体 (StartingOffset=0, Length=-1) を解放
            int hr = CfDehydratePlaceholder(handle, 0, -1, CF_DEHYDRATE_FLAG_NONE, IntPtr.Zero);
            if (hr != 0) // S_OK 以外
            {
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
            errorMessage = $"Dehydrate 実行中に例外が発生しました: {ex.Message}";
            return false;
        }
    }
}
