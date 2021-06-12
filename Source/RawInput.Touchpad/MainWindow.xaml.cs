using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RawInput.Touchpad
{
	public partial class MainWindow : Window
	{
		public string Status
		{
			get { return (string)GetValue(StatusProperty); }
			set { SetValue(StatusProperty, value); }
		}
		public static readonly DependencyProperty StatusProperty =
			DependencyProperty.Register("Status", typeof(string), typeof(MainWindow), new PropertyMetadata(null));

		public MainWindow()
		{
			InitializeComponent();
		}

		private HwndSource _targetSource;

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			_targetSource = PresentationSource.FromVisual(this) as HwndSource;
			_targetSource?.AddHook(WndProc);

			if (TouchpadHelper.Exists())
				TouchpadHelper.RegisterInput(_targetSource.Handle);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case TouchpadHelper.WM_INPUT:
					var contacts = TouchpadHelper.ParseInput(lParam);

					Status = string.Join(Environment.NewLine, contacts.Select(x => x.ToString()));
					break;
			}
			return IntPtr.Zero;
		}
	}
}