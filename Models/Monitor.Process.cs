using _Process = System.Diagnostics.Process;

namespace Brandmauer;

public partial class Monitor
{
    public string Process_Name { get; set; } = string.Empty;

    bool Check_Process()
    {
        if (Process_Name == string.Empty)
            return true;

        if (_Process.GetProcessesByName(Process_Name).Length == 0)
            return false;

        return true;
    }
}
