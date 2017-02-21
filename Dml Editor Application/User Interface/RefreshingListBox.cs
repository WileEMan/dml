using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

#if false
namespace Dml_Editor.User_Interface
{
    public class RefreshingListBox : ListBox
    {
        /** Use of RefreshItem() leads to unexpected, bad behavior.  An alternative of simply
         *  repopulating the list is being used instead. **/
#       if true
        public new void RefreshItem(int index)
        {
            base.RefreshItem(index);
        }

        public new void RefreshItems()
        {
            base.RefreshItems();
        }
#       endif
    }
}
#endif
