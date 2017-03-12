using System;

namespace OneBusAway.ViewModel.EventArgs
{
    public class ErrorHandlerEventArgs : System.EventArgs
    {
        public Exception error { get; private set; }

        public ErrorHandlerEventArgs(Exception error)
        {
            this.error = error;
        }

    }
}
