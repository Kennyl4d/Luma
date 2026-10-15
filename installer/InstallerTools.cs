using System;
using System.IO;
using System.Runtime.InteropServices;

// Build-time helper: stores language transforms in the MSI. Never executes an installation.
internal static class InstallerTools
{
    [DllImport("msi.dll", CharSet = CharSet.Unicode)] static extern uint MsiOpenDatabase(string path, IntPtr mode, out uint database);
    [DllImport("msi.dll", CharSet = CharSet.Unicode)] static extern uint MsiDatabaseOpenView(uint database, string query, out uint view);
    [DllImport("msi.dll")] static extern uint MsiCreateRecord(uint fields);
    [DllImport("msi.dll", CharSet = CharSet.Unicode)] static extern uint MsiRecordSetString(uint record, uint field, string value);
    [DllImport("msi.dll", CharSet = CharSet.Unicode)] static extern uint MsiRecordSetStream(uint record, uint field, string path);
    [DllImport("msi.dll")] static extern uint MsiViewExecute(uint view, uint record);
    [DllImport("msi.dll")] static extern uint MsiDatabaseCommit(uint database);
    [DllImport("msi.dll")] static extern uint MsiCloseHandle(uint handle);
    [DllImport("msi.dll", CharSet = CharSet.Unicode)] static extern uint MsiGetSummaryInformation(uint database, string path, uint count, out uint summary);
    [DllImport("msi.dll", CharSet = CharSet.Unicode)] static extern uint MsiSummaryInfoSetProperty(uint summary, uint property, uint type, int value, IntPtr fileTime, string text);
    [DllImport("msi.dll")] static extern uint MsiSummaryInfoPersist(uint summary);

    static void Check(uint error) { if (error != 0) throw new InvalidOperationException("MSI error " + error); }
    static int Main(string[] args)
    {
        uint database = 0, summary = 0;
        try
        {
            Check(MsiOpenDatabase(args[0], new IntPtr(1), out database));
            foreach (string path in args[1] == "--summary-only" ? new string[0] : Directory.GetFiles(args[1], "*.mst"))
            {
                uint view = 0, record = MsiCreateRecord(2);
                try
                {
                    Check(MsiDatabaseOpenView(database, "INSERT INTO `_Storages` (`Name`, `Data`) VALUES (?, ?)", out view));
                    Check(MsiRecordSetString(record, 1, Path.GetFileNameWithoutExtension(path)));
                    Check(MsiRecordSetStream(record, 2, path)); Check(MsiViewExecute(view, record));
                }
                finally { if (record != 0) MsiCloseHandle(record); if (view != 0) MsiCloseHandle(view); }
            }
            Check(MsiGetSummaryInformation(database, null, 1, out summary));
            // An MSI source database has one language. Setup selects the embedded transform explicitly.
            Check(MsiSummaryInfoSetProperty(summary, 7, 30, 0, IntPtr.Zero, "x64;1033"));
            Check(MsiSummaryInfoPersist(summary)); Check(MsiDatabaseCommit(database));
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        finally { if (summary != 0) MsiCloseHandle(summary); if (database != 0) MsiCloseHandle(database); }
    }
}
