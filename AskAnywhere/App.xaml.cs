using System.Windows;

namespace AskAnywhere
{
    public partial class App
    {
        /// <summary>
        /// the main app instance, set up while app start running.
        /// </summary>
        private AskApp _app;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _app = new AskApp(this);
            _app.Startup();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _app.ShutDown();
            base.OnExit(e);
        }
    }
}
