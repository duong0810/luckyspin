using System;

namespace DevExpress.XtraTab.ViewInfo
{
    internal class CloseButtonEventArgs : EventArgs
    {
        public XtraTabPage TabPage { get; internal set; }
    }
}