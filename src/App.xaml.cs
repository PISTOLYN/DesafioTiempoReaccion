using System.IO;
using System.Windows;
namespace DesafioTiempoReaccion;

public partial class App : Application
{
    public App()
    {
        // Si algo falla al abrir, se muestra el error y se guarda en error.log (junto al .exe)
        DispatcherUnhandledException += (_, e) =>
        {
            Mostrar(e.Exception);
            e.Handled = true;
            Shutdown();
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Mostrar(e.ExceptionObject as Exception);
    }

    private static void Mostrar(Exception? ex)
    {
        string txt = ex?.ToString() ?? "Error desconocido";
        try { File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "error.log"), txt); } catch { }
        MessageBox.Show(txt, "Error al iniciar", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
