using System;

namespace DevExpress.XtraTab.ViewInfo
{
    internal class CloseButtonEventHandler
    {
        private Action<object, CloseButtonEventArgs> tabVoucher_CloseButtonClick;

        public CloseButtonEventHandler(Action<object, CloseButtonEventArgs> tabVoucher_CloseButtonClick)
        {
            this.tabVoucher_CloseButtonClick = tabVoucher_CloseButtonClick;
        }
    }
}