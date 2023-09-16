using System.Diagnostics;
using System.Security.Principal;

class Program
{
    static void Main(string[] args)
    {

        

        Debug.WriteLine("This process has access to the entire public desktop API surface");
        Debug.WriteLine("Running as admin: " + IsAppRunningAsAdministrator());

        // Wait for 10 seconds
        AutoResetEvent areWeDone = new AutoResetEvent(false);
        areWeDone.WaitOne(10000);
    }


    public static bool IsAppRunningAsAdministrator()
    {
        var identity = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return identity.IsInRole(WindowsBuiltInRole.Administrator);
    }
}